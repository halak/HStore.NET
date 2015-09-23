using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Halak
{
    /// <summary>
    /// Lightweight HStore class for Postgresql.
    /// </summary>
    /// <see cref="http://www.postgresql.org/docs/9.4/static/hstore.html"/>
    public sealed class HStore : IReadOnlyDictionary<string, string>, IEquatable<HStore>
    {
        private delegate Tuple<string[], string[]> Convert(object items);
        
        public static readonly HStore Empty = new HStore();
        private static readonly ConcurrentDictionary<Type, Convert> convertMethods;

        private readonly string[] keys;
        private readonly string[] values;

        public int Count => keys.Length;

        public string[] Keys => keys;
        public string[] Values => values;

        public HashSet<string> KeysAsSet => new HashSet<string>(keys);
        public HashSet<string> ValuesAsSet => new HashSet<string>(values);

        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => keys;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => values;

        public string this[string key] => Get(key);

        #region Constructors
        public HStore()
        {
            keys = new string[0];
            values = new string[0];
        }

        public HStore(IDictionary<string, Value> items)
        {
            var index = 0;
            keys = new string[items.Count];
            values = new string[items.Count];
            foreach (var e in items)
            {
                if (e.Key == null)
                    throw new ArgumentNullException("key");

                keys[index] = e.Key;
                values[index] = e.Value.value;
                index++;
            }
        }

        public HStore(IDictionary<string, object> items)
        {
            var index = 0;
            keys = new string[items.Count];
            values = new string[items.Count];
            foreach (var e in items)
            {
                if (e.Key == null)
                    throw new ArgumentNullException("key");

                keys[index] = e.Key;
                values[index] = e.Value.ToString();
                index++;
            }
        }

        public HStore(IDictionary<string, string> items)
        {
            var index = 0;
            keys = new string[items.Count];
            values = new string[items.Count];
            foreach (var e in items)
            {
                if (e.Key == null)
                    throw new ArgumentNullException("key");

                keys[index] = e.Key;
                values[index] = e.Value;
                index++;
            }
        }

        public HStore(object items)
        {
            var keyValues = convertMethods.GetOrAdd(items.GetType(), BuildConvertMethod)(items);
            keys = keyValues.Item1;
            values = keyValues.Item2;
        }

        private HStore(string[] keys, string[] values)
        {
            this.keys = keys;
            this.values = values;
        }

        private HStore(List<KeyValuePair<string, string>> mutableItems)
        {
            keys = new string[mutableItems.Count];
            values = new string[mutableItems.Count];

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = mutableItems[i].Key;
                values[i] = mutableItems[i].Value;
            }
        }

        static HStore()
        {
            convertMethods = new ConcurrentDictionary<Type, Convert>();
        }
        #endregion

        #region Parse
        public static HStore Parse(string s)
        {
            s = s.Trim();

            if (s.StartsWith("'"))
            {
                if (s.EndsWith("'::hstore", StringComparison.OrdinalIgnoreCase))
                    s = s.Substring(1, s.Length - 10).Trim();
                else if (s.EndsWith("'"))
                    s = s.Substring(1, s.Length - 2).Trim();
                else
                    throw new ArgumentException(s);
            }

            var mutableItems = new List<KeyValuePair<string, string>>(16);
            var cursor = 0;
            while (cursor < s.Length)
            {
                int keyStart;
                int keyEnd;
                if (s[cursor] == '"')
                {
                    keyStart = ++cursor;
                    cursor = SkipWhile(s, cursor, '"', true);
                    keyEnd = cursor++;
                }
                else
                {
                    keyStart = cursor;
                    cursor = SkipWhile(s, cursor, '=', true);
                    keyEnd = cursor++;
                    if (s[cursor] != '>')
                        throw new ArgumentException(s);
                }

                cursor = SkipWhiteSpaces(s, cursor, true);

                int valueStart;
                int valueEnd;
                bool valueIsNullable = false;
                if (s[cursor] == '"')
                {
                    valueStart = ++cursor;
                    cursor = SkipWhile(s, cursor, '"', true);
                    valueEnd = cursor++;
                }
                else
                {
                    valueStart = cursor;
                    cursor = SkipWhile(s, cursor, ',', false);
                    valueEnd = cursor++;
                    valueIsNullable = true;
                }

                mutableItems.Add(
                    new KeyValuePair<string, string>(
                        Decode(s.Substring(keyStart, keyEnd - keyStart)),
                        Decode(s.Substring(valueStart, valueEnd - valueStart), valueIsNullable)));

                cursor = SkipWhiteSpaces(s, cursor, false);
            }

            return new HStore(mutableItems);
        }

        public static bool TryParse(string s, out HStore hstore)
        {
            try
            {
                hstore = Parse(s);
                return true;
            }
            catch (Exception)
            {
                hstore = null;
                return false;
            }
        }

        private static int SkipWhiteSpaces(string s, int cursor, bool exceptionIfEnded)
        {
            while (cursor < s.Length)
            {
                var c = s[cursor];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '=' || c == '>' || c == ',')
                    cursor++;
                else
                    return cursor;
            }

            if (exceptionIfEnded == false)
                return cursor;
            else
                throw new ArgumentException(s);
        }

        private static int SkipWhile(string s, int cursor, char c, bool exceptionIfEnded)
        {
            while (cursor < s.Length)
            {
                if (s[cursor] == '\\')
                    cursor += 2;
                else if (s[cursor] != c)
                    cursor++;
                else
                    return cursor;
            }

            if (exceptionIfEnded == false)
                return cursor;
            else
                throw new ArgumentException(s);
        }

        private static string Decode(string s, bool nullable = false)
        {
            if (nullable && string.Equals(s, "NULL", StringComparison.OrdinalIgnoreCase))
                return null;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\')
                    sb.Append(s[i]);
                else
                {
                    i++;

                    switch (s[i])
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'u':
                            char a = s[++i];
                            char b = s[++i];
                            char c = s[++i];
                            char d = s[++i];
                            sb.Append((char)((Hex(a) * 4096) + (Hex(b) * 256) + (Hex(c) * 16) + (Hex(d))));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
            return sb.ToString();
        }

        private static int Hex(char c)
        {
            return
                ('0' <= c && c <= '9') ?
                    c - '0' :
                ('a' <= c && c <= 'f') ?
                    c - 'a' + 10 :
                    c - 'A' + 10;
        }
        #endregion

        #region Get
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

        public bool TryGetValue(string key, out string value)
        {
            var index = IndexOf(key);
            if (index != -1)
            {
                value = values[index];
                return true;
            }
            else
            {
                value = default(string);
                return false;
            }
        }

        public bool Contains(HStore hstore)
        {
            for (int i = 0; i < hstore.keys.Length; i++)
            {
                var index = IndexOf(hstore.keys[i], hstore.values[i]);
                if (index == -1)
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
                return !string.Equals(values[index], null);
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

        public bool ContainsKey(string key)
        {
            return Contains(key);
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

        private int IndexOf(string key, string value)
        {
            var index = IndexOf(key);
            if (index != -1 && string.Equals(values[index], value))
                return index;
            else
                return -1;
        }
        #endregion

        #region Manipulate
        public HStore Concat(HStore hstore)
        {
            var mutableItems = CreateMutableItems(hstore.keys.Length);
            for (int i = 0; i < hstore.keys.Length; i++)
            {
                var index = IndexOf(hstore.keys[i]);
                if (index != -1)
                    mutableItems[index] = new KeyValuePair<string, string>(hstore.keys[i], hstore.values[i]);
                else
                    mutableItems.Add(new KeyValuePair<string, string>(hstore.keys[i], hstore.values[i]));
            }

            return new HStore(mutableItems);
        }

        public HStore Delete(string key)
        {
            var index = IndexOf(key);
            if (index != -1)
            {
                var mutableItems = CreateMutableItems();
                mutableItems.RemoveAt(index);
                return new HStore(mutableItems);
            }
            else
                return this;
        }

        public HStore Delete(params string[] keys)
        {
            var mutableItems = CreateMutableItems();
            for (int i = 0; i < keys.Length; i++)
            {
                var index = IndexOf(mutableItems, keys[i]);
                if (index != -1)
                    mutableItems.RemoveAt(index);
            }

            return new HStore(mutableItems);
        }

        public HStore Delete(HStore hstore)
        {
            var mutableItems = CreateMutableItems();
            for (int i = 0; i < hstore.keys.Length; i++)
            {
                var index = IndexOf(mutableItems, keys[i]);
                if (index != -1 && string.Equals(mutableItems[index].Value, hstore.values[i]))
                    mutableItems.RemoveAt(index);
            }

            if (mutableItems.Count != keys.Length)
                return new HStore(mutableItems);
            else
                return this;
        }

        public HStore Replace(HStore hstore)
        {
            var mutableKeys = new string[keys.Length];
            var mutableValues = new string[values.Length];
            var modified = false;

            keys.CopyTo(mutableKeys, 0);
            values.CopyTo(mutableValues, 0);

            for (int i = 0; i < hstore.keys.Length; i++)
            {
                var key = hstore.keys[i];
                for (int k = 0; k < mutableKeys.Length; k++)
                {
                    if (mutableKeys[k] == key)
                    {
                        mutableValues[k] = hstore.values[i];
                        modified = true;
                        break;
                    }
                }
            }

            if (modified)
                return new HStore(mutableKeys, mutableValues);
            else
                return this;
        }

        public HStore Slice(params string[] keys)
        {
            var mutableItems = new List<KeyValuePair<string, string>>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                for (int k = 0; k < this.keys.Length; k++)
                {
                    if (this.keys[k] == key)
                    {
                        mutableItems.Add(new KeyValuePair<string, string>(key, values[k]));
                        break;
                    }
                }
            }

            if (mutableItems.Count > 0)
                return new HStore(mutableItems);
            else
                return Empty;
        }

        private List<KeyValuePair<string, string>> CreateMutableItems(int capacityIncrement = 0)
        {
            var mutableItems = new List<KeyValuePair<string, string>>(keys.Length + capacityIncrement);
            for (int i = 0; i < keys.Length; i++)
                mutableItems.Add(new KeyValuePair<string, string>(keys[i], values[i]));

            return mutableItems;
        }

        private static int IndexOf(List<KeyValuePair<string, string>> source, string key)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].Key == key)
                    return i;
            }

            return -1;
        }
        #endregion

        #region Compare
        public override bool Equals(object obj)
        {
            var hstore = obj as HStore;
            if (hstore != null)
                return Equals(hstore);
            else
                return false;
        }

        public bool Equals(HStore other)
        {
            if (keys.Length != other.keys.Length)
                return false;

            for (int i = 0; i < other.keys.Length; i++)
            {
                if (IndexOf(other.keys[i], other.values[i]) == -1)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                hashCode += keys[i].GetHashCode();
                hashCode += values[i] != null ? values[i].GetHashCode() : 0;
            }

            return hashCode;
        }
        #endregion
        
        #region Encode
        public override string ToString()
        {
            var sb = new StringBuilder(CalculateCapacity(7) + 10);  // 7 means ""=>"", 10 means ''::hstore
            sb.Append('\'');
            if (keys.Length > 0)
            {
                Encode(sb, keys[0]);
                sb.Append("=>");
                Encode(sb, values[0]);
            }

            for (int i = 1; i < keys.Length; i++)
            {
                sb.Append(',');
                Encode(sb, keys[i]);
                sb.Append("=>");
                Encode(sb, values[i]);
            }
            sb.Append("'::hstore");
            return sb.ToString();
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
                EncodeJson(sb, keys[0]);
                sb.Append(':');
                EncodeJson(sb, values[0]);
            }

            for (int i = 1; i < keys.Length; i++)
            {
                sb.Append(',');
                EncodeJson(sb, keys[i]);
                sb.Append(':');
                EncodeJson(sb, values[i]);
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
                EncodeJson(sb, keys[0]);
                sb.Append(':');
                EncodeJsonLoose(sb, values[0]);
            }

            for (int i = 1; i < keys.Length; i++)
            {
                sb.Append(',');
                EncodeJson(sb, keys[i]);
                sb.Append(':');
                EncodeJsonLoose(sb, values[i]);
            }
            sb.Append('}');

            return sb.ToString();
        }

        private static void Encode(StringBuilder sb, string s)
        {
            if (s == null)
            {
                sb.Append("NULL");
                return;
            }

            if (s.Length == 0)
            {
                sb.Append("\"\"");
                return;
            }

            var wrapNeeded = false;
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '"' || c == '\'' || c == ',' || c == '=' || c == ' ' || c == '\r' || c == '\n' || c == '\t')
                {
                    wrapNeeded = true;
                    break;
                }
            }

            if (wrapNeeded)
                sb.Append('"');

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '"':
                        sb.Append('\\');
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        sb.Append('\\');
                        break;
                    case '\n':
                        sb.Append('\\');
                        sb.Append('n');
                        break;
                    case '\t':
                        sb.Append('\\');
                        sb.Append('t');
                        break;
                    case '\r':
                        sb.Append('\\');
                        sb.Append('r');
                        break;
                    case '\b':
                        sb.Append('\\');
                        sb.Append('b');
                        break;
                    case '\f':
                        sb.Append('\\');
                        sb.Append('f');
                        break;
                    case '\'':
                        sb.Append('\'');
                        sb.Append('\'');
                        break;
                    default:
                        sb.Append(s[i]);
                        break;
                }
            }

            if (wrapNeeded)
                sb.Append('"');
        }

        private static void EncodeJson(StringBuilder sb, string s)
        {
            if (s != null)
            {
                sb.Append('"');
                for (int i = 0; i < s.Length; i++)
                {
                    switch (s[i])
                    {
                        case '"':
                            sb.Append('\\');
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            sb.Append('\\');
                            break;
                        case '\n':
                            sb.Append('\\');
                            sb.Append('n');
                            break;
                        case '\t':
                            sb.Append('\\');
                            sb.Append('t');
                            break;
                        case '\r':
                            sb.Append('\\');
                            sb.Append('r');
                            break;
                        case '\b':
                            sb.Append('\\');
                            sb.Append('b');
                            break;
                        case '\f':
                            sb.Append('\\');
                            sb.Append('f');
                            break;
                        default:
                            sb.Append(s[i]);
                            break;
                    }
                }
                sb.Append('"');
            }
            else
                sb.Append("null");
        }

        private static void EncodeJsonLoose(StringBuilder sb, string s)
        {
            if (s == null)
            {
                sb.Append("null");
                return;
            }

            if (s.Length == 1)
            {
                if (s[0] == 't')
                {
                    sb.Append("true");
                    return;
                }
                else if (s[0] == 'f')
                {
                    sb.Append("false");
                    return;
                }
            }

            if (PredeciateNumber(s))
            {
                var number = 0.0;
                if (double.TryParse(s, out number))
                {
                    sb.Append(s);
                    return;
                }
            }

            EncodeJson(sb, s);
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

        private static bool PredeciateNumber(string s)
        {
            if (s.Length > 0 && s[0] == '0')
                return false;

            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c < '0' || '9' < c)
                {
                    switch (c)
                    {
                        case '+':
                        case '-':
                        case '.':
                        case ',':
                            break;
                        default:
                            return false;
                    }
                }
            }

            return true;
        }
        #endregion

        #region Enumerate
        public KeyValuePair<string, string>[] Each()
        {
            var result = new KeyValuePair<string, string>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                result[i] = new KeyValuePair<string, string>(keys[i], values[i]);

            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            for (int i = 0; i < keys.Length; i++)
                yield return new KeyValuePair<string, string>(keys[i], values[i]);
        }
        #endregion

        #region Convert
        private static Convert BuildConvertMethod(Type t)
        {
            /*
            var source = (T)sourceParameter;

            return new Tuple<string[], string[]>(
                new string[N] { "A", "B", ... "Z" },
                new string[N] { source.A, source.B, ... , source.Z });
            */

            var sourceParameter = Expression.Parameter(typeof(object));

            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            var toStringMethod = typeof(object).GetMethod(nameof(object.ToString), Type.EmptyTypes);
            var dateTimeToStringMethod = typeof(DateTime).GetMethod(nameof(object.ToString), new Type[] { typeof(string) });

            var source = Expression.Variable(t);
            var keys = new List<Expression>(properties.Length);
            var values = new List<Expression>(properties.Length);
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].GetCustomAttribute<NonSerializedAttribute>() != null)
                    continue;

                keys.Add(Expression.Constant(properties[i].Name));
                if (properties[i].PropertyType == typeof(DateTime))
                    values.Add(Expression.Call(Expression.Property(source, properties[i]), dateTimeToStringMethod, Expression.Constant("O")));
                else
                    values.Add(Expression.Call(Expression.Property(source, properties[i]), toStringMethod));
            }

            var variables = new ParameterExpression[] { source };
            var stringPairType = typeof(Tuple<string[], string[]>);
            var stringPairConstructor = stringPairType.GetConstructor(new Type[] { typeof(string[]), typeof(string[]) });
            var statements = new Expression[]
            {
                Expression.Assign(source, Expression.Convert(sourceParameter, source.Type)),
                Expression.New(stringPairConstructor,
                    Expression.NewArrayInit(typeof(string), keys),
                    Expression.NewArrayInit(typeof(string), values)),
            };

            return Expression.Lambda<Convert>(Expression.Block(variables, statements), sourceParameter).Compile();
        }
        #endregion

        #region Value
        public struct Value
        {
            internal readonly string value;

            public Value(string value)
            {
                this.value = value;
            }
            
            public static implicit operator Value(bool value) => new Value(value ? "t" : "f");
            public static implicit operator Value(sbyte value) => new Value(value.ToString());
            public static implicit operator Value(byte value) => new Value(value.ToString());
            public static implicit operator Value(char value) => new Value(value.ToString());
            public static implicit operator Value(short value) => new Value(value.ToString());
            public static implicit operator Value(ushort value) => new Value(value.ToString());
            public static implicit operator Value(int value) => new Value(value.ToString());
            public static implicit operator Value(uint value) => new Value(value.ToString());
            public static implicit operator Value(long value) => new Value(value.ToString());
            public static implicit operator Value(ulong value) => new Value(value.ToString());
            public static implicit operator Value(float value) => new Value(value.ToString());
            public static implicit operator Value(double value) => new Value(value.ToString());
            public static implicit operator Value(decimal value) => new Value(value.ToString());
            public static implicit operator Value(string value) => new Value(value);
            public static implicit operator Value(DateTime v) => new Value(v.ToString("O"));
        }
        #endregion
    }
}