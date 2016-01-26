using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace IrcDotNet.Collections
{
    [Serializable]
    public class IrcDotNetTuple<T1, T2, T3> : IIrcDotNetStructuralEquatable, IIrcDotNetStructuralComparable, IComparable
    {
        private readonly T1 _item1;
        private readonly T2 _item2;
        private readonly T3 _item3;

        public IrcDotNetTuple(T1 item1, T2 item2, T3 item3)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
        }

        public T1 Item1
        {
            get
            {
                return _item1;
            }
        }

        public T2 Item2
        {
            get
            {
                return _item2;
            }
        }

        public T3 Item3
        {
            get
            {
                return _item3;
            }
        }

        public override bool Equals(object obj)
        {
            return ((IIrcDotNetStructuralEquatable)this).Equals(obj, EqualityComparer<object>.Default);
        }

        public override int GetHashCode()
        {
            return ((IIrcDotNetStructuralEquatable)this).GetHashCode(EqualityComparer<object>.Default);
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "Microsoft's Design")]
		int IIrcDotNetStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            return CompareTo(other, comparer);
        }

		bool IIrcDotNetStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            var tuple = other as IrcDotNetTuple<T1, T2, T3>;
            if (tuple == null)
            {
                return false;
            }
            else
            {
                return
                    comparer.Equals(_item1, tuple._item1) &&
                    comparer.Equals(_item2, tuple._item2) &&
                    comparer.Equals(_item3, tuple._item3);
            }
        }

		int IIrcDotNetStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            int hash = comparer.GetHashCode(_item1);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item2);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item3);
            return hash;
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "Microsoft's Design")]
        int System.IComparable.CompareTo(object obj)
        {
            return CompareTo(obj, Comparer<object>.Default);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", _item1, _item2, _item3);
        }

        private int CompareTo(object other, IComparer comparer)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                var tuple = other as IrcDotNetTuple<T1, T2, T3>;
                if (tuple == null)
                {
                    throw new ArgumentException("other");
                }
                else
                {
                    int result = comparer.Compare(_item1, tuple._item1);
                    if (result == 0)
                    {
                        result = comparer.Compare(_item2, tuple._item2);
                    }
                    if (result == 0)
                    {
                        result = comparer.Compare(_item3, tuple._item3);
                    }
                    return result;
                }
            }
        }
    }
}