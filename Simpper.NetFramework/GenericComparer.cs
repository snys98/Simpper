using System;
using System.Collections.Generic;

namespace Simpper.NetFramework
{
    public class GenericComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, object> _selector;

        public static GenericComparer<T> _instance;

        private GenericComparer(Func<T, object> selector)
        {
            _selector = selector;
        }
        public bool Equals(T x, T y)
        {
            return _selector.Invoke(x).Equals(_selector.Invoke(y));
        }

        public int GetHashCode(T obj)
        {
            return _selector.Invoke(obj).GetHashCode();
        }

        public static GenericComparer<T> Create(Func<T, object> selector)
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new GenericComparer<T>(selector);
            return _instance;
        }
    }
}
