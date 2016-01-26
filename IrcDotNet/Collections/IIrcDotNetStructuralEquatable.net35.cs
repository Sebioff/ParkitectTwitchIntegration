using System.Collections;

namespace IrcDotNet.Collections
{
    public interface IIrcDotNetStructuralEquatable
    {
        bool Equals(object other, IEqualityComparer comparer);

        int GetHashCode(IEqualityComparer comparer);
    }
}