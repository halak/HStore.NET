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
        private delegate Tuple<string[], string[]> Serialize(object items);
        private static readonly ConcurrentDictionary<Type, Serialize> serializeMethods;

        private readonly string[] keys;
        private readonly string[] values;

        public string[] Keys => keys;
        public string[] Values => values;

        public HashSet<string> KeysAsSet => new HashSet<string>(keys);
        public HashSet<string> ValuesAsSet => new HashSet<string>(values);

        public string this[string key] => Get(key);

        public Hstore()
        {
            keys = new string[0];
            values = new string[0];
        }

        public Hstore(IDictionary<string, Value> items)
        {
            var index = 0;
            keys = new string[items.Count];
            values = new string[items.Count];
            foreach (var e in items)
            {
                keys[index] = e.Key;
                values[index] = e.Value.value;
                index++;
            }
        }

        public Hstore(IDictionary<string, object> items)
        {
            var index = 0;
            keys = new string[items.Count];
            values = new string[items.Count];
            foreach (var e in items)
            {
                keys[index] = e.Key;
                values[index] = e.Value.ToString();
                index++;
            }
        }

        public Hstore(IDictionary<string, string> items)
        {
            var index = 0;
            keys = new string[items.Count];
            values = new string[items.Count];
            foreach (var e in items)
            {
                keys[index] = e.Key;
                values[index] = e.Value;
                index++;
            }
        }

        public Hstore(object items)
        {
            var keyValues = serializeMethods.GetOrAdd(items.GetType(), BuildSerializeMethod)(items);
            keys = keyValues.Item1;
            keys = keyValues.Item2;
        }

        static Hstore()
        {
            serializeMethods = new ConcurrentDictionary<Type, Serialize>();
        }

        public static Hstore Parse(string s)
        {
            throw new NotImplementedException();
        }

        public string Get(string key)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == key)
                    return values[i];
            }

            return null;
        }
        
        public string[] Get(params string[] keys)
        {
            var result = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                result[i] = Get(keys[i]);

            return result;
        }

        public Hstore Concat(Hstore hstore)
        {
            throw new NotImplementedException();
        }

        public bool Contains(Hstore hstore)
        {
            for (int i = 0; i < hstore.keys.Length; i++)
            {
                var index = IndexOf(hstore.keys[i]);
                if (index == -1 || values[index] != hstore.values[i])
                    return false;
            }

            return true;
        }

        public bool Contains(string key)
        {
            return IndexOf(key) != -1;
        }

        public bool Defined(string key)
        {
            var index = IndexOf(key);
            if (index != -1)
                return values[index] != null;
            else
                return false;
        }

        public bool ContainsAll(params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (Contains(keys[i]) == false)
                    return false;
            }

            return true;
        }

        public bool ContainsAny(params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (Contains(keys[i]))
                    return true;
            }

            return false;
        }

        private int IndexOf(string key)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == key)
                    return i;
            }

            return -1;
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
            for (int i = 0; i < keys.Length; i++)
            {
                hashCode += keys[i].GetHashCode();
                hashCode += values[i].GetHashCode();
            }

            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(CalculateCapacity(7) + 8);  // 7 means ""=>"", 8 means ::hstore

            sb.Append("::hstore");
            return sb.ToString();
        }

        private static bool ContainsSpecialCharacter(string s)
        {
            throw new NotImplementedException(); 
        }

        public string[] ToArray()
        {
            var result = new string[keys.Length * 2];
            var index = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                result[index++] = keys[i];
                result[index++] = values[i];
            }

            return result;
        }

        public string[,] ToMatrix()
        {
            var result = new string[keys.Length, 2];
            for (int i = 0; i < keys.Length; i++)
            {
                result[i, 0] = keys[i];
                result[i, 1] = values[i];
            }

            return result;
        }

        public string ToJson()
        {
            var sb = new StringBuilder(CalculateCapacity(6) + 2);  // 6 means "":"", 2 means {}

            sb.Append('{');
            if (keys.Length > 0)
            {
                sb.Append('"');
                sb.Append(EscapeForJson(keys[0]));
                sb.Append("\":\"");
                sb.Append(EscapeForJson(values[0]));
                sb.Append('"');
            }

            for (int i = 1; i < keys.Length; i++)
            {
                sb.Append(",\"");
                sb.Append(EscapeForJson(keys[i]));
                sb.Append("\":\"");
                sb.Append(EscapeForJson(values[i]));
                sb.Append('"');
            }
            sb.Append('}');

            return sb.ToString();
        }

        public string ToJsonLoose()
        {
            var sb = new StringBuilder(CalculateCapacity(6) + 2);  // 6 means "":"", 2 means {}

            sb.Append('{');
            if (keys.Length > 0)
            {
                sb.Append('"');
                sb.Append(EscapeForJson(keys[0]));
                sb.Append("\":\"");
                sb.Append(EscapeForJson(values[0]));
                sb.Append('"');
            }

            for (int i = 1; i < keys.Length; i++)
            {
                sb.Append(",\"");
                sb.Append(EscapeForJson(keys[i]));
                sb.Append("\":");
                sb.Append(EncodeJsonValue(values[i]));
            }
            sb.Append('}');

            return sb.ToString();
        }

        public KeyValuePair<string, string>[] Each()
        {
            var result = new KeyValuePair<string, string>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                result[i] = new KeyValuePair<string, string>(keys[i], values[i]);

            return result;
        }

        private int CalculateCapacity(int defaultCapacityPerEntry)
        {
            var capacity = keys.Length * defaultCapacityPerEntry;
            for (int i = 0; i < keys.Length; i++)
            {
                capacity += keys[i].Length;
                capacity += values[i].Length;
            }

            return capacity;
        }

        private static string EscapeForJson(string s)
        {
            // TODO: 
            return s;
        }

        private static string EncodeJsonValue(string s)
        {
            // TODO: s의 형태에 따라서 숫자, 문자열, 불린형을 구분하여 Json 값에 맞게 인코딩한다.
            return '"' + s + '"';
        }

        private static Serialize BuildSerializeMethod(Type t)
        {
            throw new NotImplementedException();
        }

        public struct Value
        {
            internal readonly string value;

            public Value(string value)
            {
                this.value = value;
            }
            
            public static implicit operator Value(bool value)
            {
                return new Value(value ? "t" : "f");
            }

            public static implicit operator Value(sbyte value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(byte value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(char value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(short value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(ushort value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(int value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(uint value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(long value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(ulong value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(float value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(double value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(decimal value)
            {
                return new Value(value.ToString());
            }

            public static implicit operator Value(string value)
            {
                return new Value(value);
            }
        }
    }
}