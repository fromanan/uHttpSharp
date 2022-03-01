using System;
using System.Collections.Generic;
using System.Linq;

namespace uhttpsharp.Headers
{
    /// <summary>
    /// A trivial implementation of <see cref="IHttpHeaders"/>
    /// that is composed from multiple <see cref="IHttpHeaders"/>.
    /// 
    /// If value is found in more then one header,
    /// Gets the first available value from by the order of the headers
    /// given in the c'tor.
    /// </summary>
    public class CompositeHttpHeaders : IHttpHeaders
    {
        private static readonly IEqualityComparer<KeyValuePair<string, string>> HeaderComparer =
            new KeyValueComparer<string, string, string>(k => k.Key, StringComparer.InvariantCultureIgnoreCase);

        private readonly IEnumerable<IHttpHeaders> children;

        public CompositeHttpHeaders(IEnumerable<IHttpHeaders> children)
        {
            this.children = children;
        }

        public CompositeHttpHeaders(params IHttpHeaders[] children)
        {
            this.children = children;
        }

        public string GetByName(string name)
        {
            foreach (IHttpHeaders child in children)
            {
                if (child.TryGetByName(name, out string value))
                {
                    return value;
                }
            }

            throw new KeyNotFoundException($"Header {name} was not found in any of the children headers.");
        }

        public bool TryGetByName(string name, out string value)
        {
            foreach (IHttpHeaders child in children)
            {
                if (child.TryGetByName(name, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return children.SelectMany(c => c).Distinct(HeaderComparer).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class KeyValueComparer<TKey, TValue, TOutput> : IEqualityComparer<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<KeyValuePair<TKey, TValue>, TOutput> outputFunc;
        
        private readonly IEqualityComparer<TOutput> outputComparer;
        
        public KeyValueComparer(Func<KeyValuePair<TKey, TValue>, TOutput> outputFunc, IEqualityComparer<TOutput> outputComparer)
        {
            this.outputFunc = outputFunc;
            this.outputComparer = outputComparer;
        }

        public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return outputComparer.Equals(outputFunc(x), outputFunc(y));
        }

        public int GetHashCode(KeyValuePair<TKey, TValue> obj)
        {
            return outputComparer.GetHashCode(outputFunc(obj));
        }
    }
}