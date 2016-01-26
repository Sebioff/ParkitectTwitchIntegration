using System;
using UnityEngine;


namespace TwitchIntegration {
	public class Main : IMod, IModSettings {
		private TwitchIntegration __instance;

		public void onEnabled() {
			GameObject go = new GameObject(Name);
			__instance = go.AddComponent<TwitchIntegration>();
		}
		
		public void onDisabled() {
			if (__instance != null) {
				UnityEngine.Object.Destroy(__instance.gameObject);
				__instance = null;
			}
		}

		public void onDrawSettingsUI() {
			__instance.renderSettingsUI();
		}

		public void onSettingsOpened() { }
		public void onSettingsClosed() { }
		
		public string Name {
			get { return "Twitch Integration"; }
		}

		public string Identifier {
			get { return "com.themeparkitect.TwitchIntegration"; }
		}

		public string Description {
			get { return "Allows viewers of your Twitch livestream to interact with the game."; }
		}
	}
}