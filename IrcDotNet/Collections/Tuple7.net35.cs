using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace IrcDotNet.Collections
{
    [Serializable]
    public class IrcDotNetTuple<T1, T2, T3, T4, T5, T6, T7> : IIrcDotNetStructuralEquatable, IIrcDotNetStructuralComparable, IComparable
    {
        private readonly T1 _item1;
        private readonly T2 _item2;
        private readonly T3 _item3;
        private readonly T4 _item4;
        private readonly T5 _item5;
        private readonly T6 _item6;
        private readonly T7 _item7;

        public IrcDotNetTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
            _item4 = item4;
            _item5 = item5;
            _item6 = item6;
            _item7 = item7;
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

        public T4 Item4
        {
            get
            {
                return _item4;
            }
        }

        public T5 Item5
        {
            get
            {
                return _item5;
            }
        }

        public T6 Item6
        {
            get
            {
                return _item6;
            }
        }

        public T7 Item7
        {
            get
            {
                return _item7;
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
            var tuple = other as IrcDotNetTuple<T1, T2, T3, T4, T5, T6, T7>;
            if (tuple == null)
            {
                return false;
            }
            else
            {
                return
                    comparer.Equals(_item1, tuple._item1) &&
                    comparer.Equals(_item2, tuple._item2) &&
                    comparer.Equals(_item3, tuple._item3) &&
                    comparer.Equals(_item4, tuple._item4) &&
                    comparer.Equals(_item5, tuple._item5) &&
                    comparer.Equals(_item6, tuple._item6) &&
                    comparer.Equals(_item7, tuple._item7);
            }
        }

		int IIrcDotNetStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            int hash = comparer.GetHashCode(_item1);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item2);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item3);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item4);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item5);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item6);
            hash = (hash << 5) - hash + comparer.GetHashCode(_item7);
            return hash;
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "Microsoft's Design")]
        int System.IComparable.CompareTo(object obj)
        {
            return CompareTo(obj, Comparer<object>.Default);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3}, {4}, {5}, {6})", _item1, _item2, _item3, _item4, _item5, _item6, _item7);
        }

        private int CompareTo(object other, IComparer comparer)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                var tuple = other as IrcDotNetTuple<T1, T2, T3, T4, T5, T6, T7>;
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
                    if (result == 0)
                    {
                        result = comparer.Compare(_item4, tuple._item4);
                    }
                    if (result == 0)
                    {
                        result = comparer.Compare(_item5, tuple._item5);
                    }
                    if (result == 0)
                    {
                        result = comparer.Compare(_item6, tuple._item6);
                    }
                    if (result == 0)
                    {
                        result = comparer.Compare(_item7, tuple._item7);
                    }
                    return result;
                }
            }
        }
    }
}