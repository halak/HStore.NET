using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halak
{
    public sealed class Hstore
    {
        private delegate KeyValuePair<string, string>[] Serialize(object items);
        private static readonly ConcurrentDictionary<Type, Serialize> serializeMethods;

        private readonly KeyValuePair<string, string>[] items;

        public string[] Keys
        {
            get { throw new NotImplementedException(); }
        }

        public HashSet<string> KeysAsSet
        {
            get { throw new NotImplementedException(); }
        }

        public string[] Values
        {
            get { throw new NotImplementedException(); }
        }

        public HashSet<string> ValuesAsSet
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Hstore()
        {
            throw new NotImplementedException();
        }

        public Hstore(IDictionary<string, Value> items)
        {
            throw new NotImplementedException();
        }

        public Hstore(object items)
        {
            throw new NotImplementedException();
        }

        public string Get(string key)
        {
            throw new NotImplementedException();
        }
        
        public string[] Get(params string[] keys)
        {
            throw new NotImplementedException();
        }

        public Hstore Concat(Hstore hstore)
        {
            throw new NotImplementedException();
        }

        public bool Contains(Hstore hstore)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key)
        {
            throw new NotImplementedException();
        }

        public bool Defined(string key)
        {
            throw new NotImplementedException();
        }

        public bool ContainsAll(params string[] keys)
        {
            throw new NotImplementedException();
        }

        public bool ContainsAny(params string[] keys)
        {
            throw new NotImplementedException();
        }

        public Hstore Delete(string key)
        {
            throw new NotImplementedException();
        }

        public Hstore Delete(params string[] keys)
        {
            throw new NotImplementedException();
        }

        public Hstore Delete(Hstore hstore)
        {
            throw new NotImplementedException();
        }

        public Hstore Replace(Hstore hstore)
        {
            throw new NotImplementedException();
        }

        public Hstore Slice(params string[] keys)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            for (int i = 0; i < items.Length; i++)
            {
                hashCode += items[i].Key.GetHashCode();
                hashCode += items[i].Value.GetHashCode();
            }

            return hashCode;
        }

        public override string ToString()
        {
            var capacity = 0;
            for (int i = 0; i < items.Length; i++)
            {
                throw new NotImplementedException();
            }

            var sb = new StringBuilder(capacity);

            sb.Append("::hstore");
            return sb.ToString();
        }

        private static bool ContainsSpecialCharacter(string s)
        {
            throw new NotImplementedException(); 
        }

        public void ToArray()
        {
            throw new NotImplementedException();
        }

        public void ToMatrix()
        {
            throw new NotImplementedException();
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }

        public string ToJsonLoose()
        {
            throw new NotImplementedException();
        }

        public KeyValuePair<string, Value>[] Each()
        {
            throw new NotImplementedException();
        }

        public struct Value
        {
            private readonly string value;

            public Value(string value)
            {
                this.value = value;
            }
        }
    }
}