//#define DEBUG_LOGGING

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using IrcDotNet;
using Parkitect.UI;
using System.IO;
using MiniJSON;
using System.Text.RegularExpressions;

namespace TwitchIntegration {
	/// <summary>
	/// Allows your Twitch viewers to spawn a guest in the game and follow their actions, and post notifications.
	/// Uses IRC.Net, backported to work with .Net 2.0.
	/// </summary>
	public class TwitchIntegration : MonoBehaviour {
		private const string twitchClientID = "ogtnoqm4m86chl2oflq5myfznk8u5oq";
		private const string twitchRedirectUri = "http://www.themeparkitect.com/twitch_auth.html";
		private const string twitchIrcServer = "irc.twitch.tv";
		private const int twitchIrcPort = 6667;

		private Dictionary<string, string> twitchApiHeaders = new Dictionary<string, string>();

		private List<string> ircLog = new List<string>();
		private StringBuilder ircLogStringBuilder = new StringBuilder();
		private string ircLogString;
		// we have to interact with the game from the main thread, so we queue all irc messages and evaluate them in Update()
		private Queue<IrcMessageEventArgs> messageQueue = new Queue<IrcMessageEventArgs>();

		private Dictionary<string, Guest> twitchUserGuestAssoc = new Dictionary<string, Guest>();

		private TwitchIrcClient ircClient;
		private IrcChannel ircChannel;

		private enum Page {
			STATUS, SETTINGS
		}
		private Page page = Page.STATUS;
		private Rect uiRect = new Rect(4, 30, 300, 300);
		private Vector2 scrollPosition;
		private bool drawGUI = true;

		private Settings settings = new Settings();
		private string settingsFileName = "settings_TwitchIntegration.txt";
		private string settingsFilePath;

		#region MonoBehaviour lifecycle events
		void Start() {
			settingsFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/" + settingsFileName;

			loadSettings();

			twitchApiHeaders.Add("Accept", "application/vnd.twitchtv.v3+json");
			twitchApiHeaders.Add("Authorization", "OAuth " + settings.twitchOAuthToken);
			twitchApiHeaders.Add("Client-ID", twitchClientID);

			ircClient = new TwitchIrcClient();
			// Twitch doesn't allow sending more than 20 messages within 30 seconds
			ircClient.FloodPreventer = new IrcStandardFloodPreventer(1, (long)((30f / 20) * 1000));

			ircClient.Connected += onIrcConnected;
			ircClient.Disconnected += onIrcDisconnected;
			ircClient.Registered += onIrcRegistered;
			ircClient.Error += onIrcError;
			ircClient.ErrorMessageReceived += onIrcErrorMessageReceived;
			ircClient.RawMessageReceived += onIrcRawMessageReceived;
		}
		
		void Update() {
			if (Input.GetKeyDown(KeyCode.T)) {
				drawGUI = !drawGUI;
			}
			
			if (messageQueue.Count > 0) {
				parseMessage(messageQueue.Dequeue());
			}
		}
		
		public void renderSettingsUI() {
			//if (!drawGUI) {
				//return;
			//}

			//GUILayout.BeginArea(uiRect);
			//GUI.Box(new Rect(0, 0, uiRect.width, uiRect.height), "");
			//scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Status")) {
				page = Page.STATUS;
			}

			if (GUILayout.Button("Settings")) {
				page = Page.SETTINGS;
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(20);

			if (page == Page.STATUS) {
				if (string.IsNullOrEmpty(settings.twitchUsername)) {
					GUILayout.Label("You need to enter a twitch username on the settings tab");
				}
				else if (string.IsNullOrEmpty(settings.twitchChannelName)) {
					GUILayout.Label("You need to enter a twitch channel on the settings tab");
				}
				else if (string.IsNullOrEmpty(settings.twitchOAuthToken)) {
					GUILayout.Label("You need to enter a twitch auth token on the settings tab");
				}
				else {
					if (!ircClient.IsConnected && GUILayout.Button("Connect")) {
						IrcUserRegistrationInfo botUser = new IrcUserRegistrationInfo();
						botUser.NickName = settings.twitchUsername.ToLower();
						botUser.UserName = botUser.NickName;
						botUser.Password = "oauth:" + settings.twitchOAuthToken;
					
						ircClient.Connect(twitchIrcServer, twitchIrcPort, false, botUser);
					} else if (ircClient.IsConnected && GUILayout.Button("Disconnect")) {
						ircClient.Disconnect();
					}

					foreach (Guest guest in twitchUserGuestAssoc.Values) {
						if (GUILayout.Button(guest.getName())) {
							GameController.Instance.cameraController.lockOnto(guest.gameObject);
						}
					}

					GUILayout.Label(ircLogString);
				}
			}
			else if (page == Page.SETTINGS) {
				GUILayout.Label("!alert: Sending notifications");
				drawAuthToggleGroup(ref settings.authPostAlerts);
				GUILayout.Space(20);

				GUILayout.Label("!spawn: Spawning a guest");
				drawAuthToggleGroup(ref settings.authSpawnGuests);
				GUILayout.Space(20);

				GUILayout.Label("Twitch user name");
				settings.twitchUsername = GUILayout.TextField(settings.twitchUsername);
				GUILayout.Space(20);

				GUILayout.Label("Twitch channel name");
				settings.twitchChannelName = GUILayout.TextField(settings.twitchChannelName);
				GUILayout.Space(20);

				GUILayout.Label("Twitch auth token");
				settings.twitchOAuthToken = GUILayout.PasswordField(settings.twitchOAuthToken, '*');

				if (GUILayout.Button("Request an auth token")) {
					Application.OpenURL(string.Format("https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id={0}&redirect_uri={1}&scope=chat_login+channel_check_subscription",
					                                  twitchClientID, twitchRedirectUri));
				}
			}
			
			//GUILayout.EndScrollView();
			//GUILayout.EndArea();
		}

		private void drawAuthToggleGroup(ref Settings.AuthorizationLevel authLevel) {
			authLevel = GUILayout.Toggle(authLevel == Settings.AuthorizationLevel.NOBODY, "Nobody") ? Settings.AuthorizationLevel.NOBODY : authLevel;
			authLevel = GUILayout.Toggle(authLevel == Settings.AuthorizationLevel.SUBSCRIBERS, "Subscribers") ? Settings.AuthorizationLevel.SUBSCRIBERS : authLevel;
			authLevel = GUILayout.Toggle(authLevel == Settings.AuthorizationLevel.ANYONE, "Anyone") ? Settings.AuthorizationLevel.ANYONE : authLevel;
		}
		
		void OnDestroy() {
			saveSettings();

			ircClient.Disconnect();
		}

		private void loadSettings() {
			try {
				if (File.Exists(settingsFilePath)) {
					using (StreamReader reader = new StreamReader(settingsFilePath)) {
						string jsonString;
						
						SerializationContext context = new SerializationContext(SerializationContext.Context.Savegame);
						while((jsonString = reader.ReadLine()) != null) {
							Dictionary<string,object> values = (Dictionary<string,object>)Json.Deserialize(jsonString);
							Serializer.deserialize(context, settings, values);
						}
						
						reader.Close();
					}
				}
			}
			catch (System.Exception e) {
				Debug.LogError("Couldn't properly load settings file! " + e.ToString());
			}
		}

		private void saveSettings() {
			SerializationContext context = new SerializationContext(SerializationContext.Context.Savegame);
			
			using (var stream = new FileStream(settingsFilePath, FileMode.Create))
			{
				using (var writer = new StreamWriter(stream))
				{
					writer.WriteLine(Json.Serialize(Serializer.serialize(context, settings)));
				}
			}
		}
		#endregion

		#region IRC events
		private void onIrcConnected(object sender, EventArgs e) {
			addIrcLogEntry("Connected, logging into account...");
		}

		private void onIrcRegistered(object sender, EventArgs e) {
			addIrcLogEntry("Logged in, joining channel...");

			ircClient.LocalUser.JoinedChannel += onIrcJoinedChannel;
			ircClient.Channels.Join("#" + settings.twitchChannelName.ToLower());

			// request membership state events so we receive PART messages
			//ircClient.SendRawMessage("CAP REQ :twitch.tv/membership");
		}

		private void onIrcDisconnected(object sender, EventArgs e) {
			addIrcLogEntry("Disconnected.");
		}

		private void onIrcError(object sender, IrcErrorEventArgs e) {
			addIrcLogEntry("Error: " + e.Error.ToString());
		}

		private void onIrcErrorMessageReceived(object sender, IrcErrorMessageEventArgs e) {
			addIrcLogEntry("Error msg: " + e.Message);
		}

		private void onIrcJoinedChannel(object sender, IrcChannelEventArgs e) {
			addIrcLogEntry("Joined channel!");

			ircChannel = e.Channel;
			ircChannel.MessageReceived += onIrcChannelMessageReceived;
			ircClient.LocalUser.SendMessage(ircChannel, "Hi, I'm the Parkitect Twitch Integration!");
		}

		private void onIrcRawMessageReceived(object sender, IrcRawMessageEventArgs e) {
#if DEBUG_LOGGING
			Debug.Log(e.RawContent);
#endif
		}

		private void onIrcChannelMessageReceived(object sender, IrcMessageEventArgs e) {
			addIrcLogEntry(e.Source.Name + ": " + e.Text);
			messageQueue.Enqueue(e);
		}
		#endregion

		private void addIrcLogEntry(string logEntry) {
			ircLog.Add(logEntry);
			if (ircLog.Count > 30) {
				ircLog.RemoveAt(0);
			}

			ircLogStringBuilder.Length = 0;
			for (int i = 0; i < ircLog.Count; i++) {
				ircLogStringBuilder.AppendLine(ircLog[i]);
			}

			ircLogString = ircLogStringBuilder.ToString();
		}

		private void parseMessage(IrcMessageEventArgs message) {
			if (string.IsNullOrEmpty(message.Text)) {
				return;
			}

			List<string> arguments = new List<string>(message.Text.Split(' '));
			string command = arguments[0];

			if (command.Equals("!alert")) {
				if (!isPermitted(message.Source.Name, settings.authPostAlerts)) {
					return;
				}

				NotificationBar.Instance.addNotification(message.Source.Name + ": " + message.Text.Remove(0, command.Length + 1));
			}
			else if (command.Equals("!spawn")) {
				if (!isPermitted(message.Source.Name, settings.authSpawnGuests)) {
					return;
				}

				if (twitchUserGuestAssoc.ContainsKey(message.Source.Name)) {
					if (twitchUserGuestAssoc[message.Source.Name] == null) {
						twitchUserGuestAssoc.Remove(message.Source.Name);
					}
					else {
						// we already know the guest of this user
						return;
					}
				}

				Guest userGuest = null;

				// check if the guest of this user already exists in the park (e.g. loaded from savegame, or user reconnected)
				foreach (Guest parkGuest in GameController.Instance.park.getGuests()) {
					if (parkGuest.nickname.Equals(message.Source.Name)) {
						userGuest = parkGuest;
						break;
					}
				}

				// no matching guest found, spawn a new one
				if (userGuest == null) {
					userGuest = GameController.Instance.park.spawnUnInitializedPerson(Prefabs.Guest) as Guest;
					userGuest.nickname = message.Source.Name;

					Match match = Regex.Match(message.Text, @"""(\w+)""(?:\s*""(\w+)"")?", RegexOptions.IgnoreCase);
					
					if (match.Success) {
						userGuest.forename = match.Groups[1].Value;
						if (match.Groups[2].Success) {
							userGuest.surname = match.Groups[2].Value;
						}
					}

					userGuest.Initialize();
				}

				twitchUserGuestAssoc.Add(message.Source.Name, userGuest);
			}
			else if (command.Equals("!thoughts")) {
				Guest guest = twitchUserGuestAssoc[message.Source.Name];
				if (guest == null) {
					return;
				}

				if (guest.thoughts.Count > 0) {
					ircClient.LocalUser.SendMessage(ircChannel, "Thoughts of " + guest.getName() + ": " + guest.thoughts[guest.thoughts.Count - 1].text);
				}
			}
			else if (command.Equals("!actions")) {
				Guest guest = twitchUserGuestAssoc[message.Source.Name];
				if (guest == null) {
					return;
				}

				List<Experience> experiences = guest.experienceLog.getExperiences();
				if (experiences.Count > 0) {
					ircClient.LocalUser.SendMessage(ircChannel, "Actions of " + guest.getName() + ": " + experiences[experiences.Count - 1].getDescription());
				}
			}
			else if (command.Equals("!status")) {
				Guest guest = twitchUserGuestAssoc[message.Source.Name];
				if (guest == null) {
					return;
				}

				ircClient.LocalUser.SendMessage(ircChannel, "Status of " + guest.getName() + ": " + guest.currentBehaviour.getDescription() + ".");
			}
			else if (command.Equals("!inventory")) {
				Guest guest = twitchUserGuestAssoc[message.Source.Name];
				if (guest == null) {
					return;
				}

				List<string> inventoryItems = new List<string>();
				foreach (Item item in guest.getFromInventory<Item>()) {
					inventoryItems.Add(item.getDescription());
				}
				ircClient.LocalUser.SendMessage(ircChannel, "Inventory of " + guest.getName() + ": " + string.Join(", ", inventoryItems.ToArray()));
			}
		}

		private bool isPermitted(string user, Settings.AuthorizationLevel authorizationLevel) {
			if (authorizationLevel == Settings.AuthorizationLevel.ANYONE) {
				return true;
			}
			else if (authorizationLevel == Settings.AuthorizationLevel.NOBODY) {
				return false;
			}
			else if (authorizationLevel == Settings.AuthorizationLevel.SUBSCRIBERS) {
				WWW www = new WWW("https://api.twitch.tv/kraken/channels/" + settings.twitchChannelName.ToLower() + "/subscriptions/" + user, null, twitchApiHeaders);
				// FIXME ok this is a shitty thing to do, we really shouldn't block the main thread
				while (!www.isDone) { }

				if (www.error != null) {
					ircClient.LocalUser.SendMessage(ircChannel, user + ": You need to be subscribed to this channel to do this.");
					return false;
				}

				return true;
			}

			return false;
		}
	}
}

