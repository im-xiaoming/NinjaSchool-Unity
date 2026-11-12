using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NsoServer.Data
{
    // Minimal JSON writer (no reflection; values: string, numbers, bool, null, lists, dictionaries from Json.Parse).
    public sealed class JsonWriter
    {
        private readonly StringBuilder sb = new StringBuilder(4096);
        private bool needComma;

        private void Sep() { if (needComma) sb.Append(','); needComma = false; }

        public void BeginObject() { Sep(); sb.Append('{'); }
        public void EndObject() { sb.Append('}'); needComma = true; }
        public void BeginArray() { Sep(); sb.Append('['); }
        public void EndArray() { sb.Append(']'); needComma = true; }
        public void Key(string k) { Sep(); Str(k); sb.Append(':'); }
        public void Prop(string k, object v) { Key(k); Value(v); }

        public void Value(object v)
        {
            if (v is List<object> || v is System.Collections.IList && !(v is string))
            {
                BeginArray();
                foreach (var o in (System.Collections.IEnumerable)v) Value(o);
                EndArray();
                return;
            }
            if (v is System.Collections.IDictionary d)
            {
                BeginObject();
                foreach (System.Collections.DictionaryEntry kv in d) Prop((string)kv.Key, kv.Value);
                EndObject();
                return;
            }
            Sep();
            if (v == null) sb.Append("null");
            else if (v is string s) Str(s);
            else if (v is bool b) sb.Append(b ? "true" : "false");
            else if (v is double db) sb.Append(db.ToString("R", CultureInfo.InvariantCulture));
            else if (v is float f) sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
            else sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture));
            needComma = true;
        }

        private void Str(string s)
        {
            sb.Append('"');
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20 || ch == (char)0x2028 || ch == (char)0x2029) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        public override string ToString() { return sb.ToString(); }
    }
}
