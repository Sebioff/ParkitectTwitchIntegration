using System.Collections;

namespace IrcDotNet.Collections
{
    public interface IIrcDotNetStructuralComparable
    {
        int CompareTo(object other, IComparer comparer);
    }
}