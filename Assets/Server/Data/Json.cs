using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NsoServer.Data
{
    // Minimal JSON reader for the JSON columns of the original database.
    // Objects become Dictionary<string, object>, arrays List<object>, integers long, other numbers double.
    public static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            object v = ReadValue(text, ref i);
            return v;
        }

        public static List<object> Array(string text)
        {
            return (List<object>)Parse(text);
        }

        public static long Long(object o)
        {
            if (o is long)
                return (long)o;
            if (o is double)
                return (long)(double)o;
            return long.Parse(Convert.ToString(o, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        private static void Ws(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }

        private static object ReadValue(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                i++;
                var d = new Dictionary<string, object>();
                Ws(s, ref i);
                if (s[i] == '}')
                {
                    i++;
                    return d;
                }

                while (true)
                {
                    Ws(s, ref i);
                    string k = ReadString(s, ref i);
                    Ws(s, ref i);
                    i++; // ':'
                    d[k] = ReadValue(s, ref i);
                    Ws(s, ref i);
                    if (s[i++] == '}')
                        return d;
                }
            }

            if (c == '[')
            {
                i++;
                var l = new List<object>();
                Ws(s, ref i);
                if (s[i] == ']')
                {
                    i++;
                    return l;
                }

                while (true)
                {
                    l.Add(ReadValue(s, ref i));
                    Ws(s, ref i);
                    if (s[i++] == ']')
                        return l;
                }
            }

            if (c == '"')
                return ReadString(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0)
            {
                i += 4;
                return true;
            }

            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0)
            {
                i += 5;
                return false;
            }

            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0)
            {
                i += 4;
                return null;
            }

            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
                i++;
            string num = s.Substring(st, i - st);
            long l2;
            if (long.TryParse(num, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out l2))
                return l2;
            return double.Parse(num, CultureInfo.InvariantCulture);
        }

        private static string ReadString(string s, ref int i)
        {
            i++; // opening quote
            var sb = new StringBuilder();
            while (true)
            {
                char c = s[i++];
                if (c == '"')
                    break;
                if (c == '\\')
                {
                    char n = s[i++];
                    switch (n)
                    {
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'u':
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                            break;
                        default:
                            sb.Append(n);
                            break;
                    }
                }
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
