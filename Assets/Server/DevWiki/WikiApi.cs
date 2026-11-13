using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NsoServer.Data;

namespace NsoServer.DevWiki
{
    using Math = System.Math; // the client has its own global Math class

    // JSON API over the wiki database, for scripts and AI agents (served by WikiServer under /api):
    //   GET /api                               help: collections, counts, query syntax
    //   GET /api/search?q=kiem go              every collection; hits as {collection, id, name, status}
    //   GET /api/{collection}?q=...            documents of one collection; q = free words and field:value terms
    //        &field=value                      extra exact filters (any top-level field, e.g. status=todo, map=22)
    //        &fields=id,name                   keep only these top-level fields
    //        &limit=50&offset=0                paging (default limit 100, max 2000)
    //   GET /api/{collection}/{id}             one document (404 if missing)
    // Matching: case and Vietnamese accents are ignored; a number matches exactly; text matches by "contains";
    // field:value also looks inside lists (e.g. maps?q=npc... / npcs?q=map:22 finds NPCs standing on map 22).
    public static class WikiApi
    {
        public const int DefaultLimit = 100, MaxLimit = 2000;
        private static readonly HashSet<string> Reserved = new HashSet<string>
        {
            "q",
            "fields",
            "limit",
            "offset"
        };
        // returns (status code, JSON body)
        public static KeyValuePair<int, string> Handle(Dictionary<string, object> db, string path, string query)
        {
            var colls = db["collections"] as Dictionary<string, object>;
            var args = ParseQuery(query);
            string[] parts = path.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries); // "api", coll, id
            var w = new JsonWriter();
            if (parts.Length == 1)
            {
                Help(w, db, colls);
                return Ok(w);
            }

            string coll = Uri.UnescapeDataString(parts[1]);
            if (coll == "search")
                return Search(w, colls, Arg(args, "q"), IntArg(args, "limit", DefaultLimit));
            object listObj;
            if (!colls.TryGetValue(coll, out listObj))
                return Error(404, "unknown collection '" + coll + "'; see /api");
            var list = (List<object>)listObj;
            if (parts.Length >= 3)
            {
                string id = Uri.UnescapeDataString(string.Join("/", parts, 2, parts.Length - 2));
                foreach (var o in list)
                {
                    var d = o as Dictionary<string, object>;
                    if (d != null && Convert.ToString(d["id"], CultureInfo.InvariantCulture) == id)
                    {
                        w.Value(d);
                        return Ok(w);
                    }
                }

                return Error(404, coll + "/" + id + " not found");
            }

            var terms = Terms(Arg(args, "q"));
            foreach (var kv in args)
                if (!Reserved.Contains(kv.Key))
                    terms.Add(new Term { Field = kv.Key, Value = Fold(kv.Value) });
            int limit = Math.Min(MaxLimit, Math.Max(1, IntArg(args, "limit", DefaultLimit))), offset = Math.Max(0, IntArg(args, "offset", 0));
            string fieldsArg = Arg(args, "fields");
            var fields = string.IsNullOrEmpty(fieldsArg) ? null : new HashSet<string>(fieldsArg.Split(','));
            var hits = new List<Dictionary<string, object>>();
            foreach (var o in list)
            {
                var d = o as Dictionary<string, object>;
                if (d != null && Matches(d, terms))
                    hits.Add(d);
            }

            w.BeginObject();
            w.Prop("collection", coll);
            w.Prop("total", hits.Count);
            w.Prop("offset", offset);
            w.Prop("limit", limit);
            w.Key("docs");
            w.BeginArray();
            for (int i = offset; i < hits.Count && i < offset + limit; i++)
            {
                if (fields == null)
                {
                    w.Value(hits[i]);
                    continue;
                }

                w.BeginObject();
                foreach (var kv in hits[i])
                    if (fields.Contains(kv.Key))
                        w.Prop(kv.Key, kv.Value);
                w.EndObject();
            }

            w.EndArray();
            w.EndObject();
            return Ok(w);
        }

        private static void Help(JsonWriter w, Dictionary<string, object> db, Dictionary<string, object> colls)
        {
            w.BeginObject();
            w.Prop("generatedAt", db.ContainsKey("generatedAt") ? db["generatedAt"] : null);
            w.Key("collections");
            w.BeginObject();
            foreach (var kv in colls)
            {
                var l = kv.Value as List<object>;
                w.Key(kv.Key);
                w.BeginObject();
                w.Prop("count", l == null ? 0 : l.Count);
                // field names of the first document, so an agent knows what it can filter on
                var first = l != null && l.Count > 0 ? l[0] as Dictionary<string, object> : null;
                w.Key("fields");
                w.Value(first == null ? new List<object>() : new List<object>(first.Keys));
                w.EndObject();
            }

            w.EndObject();
            w.Key("usage");
            w.Value(new List<object> { "GET /api/search?q=<words>  -> hits in every collection", "GET /api/<collection>?q=<words and field:value>&<field>=<value>&fields=id,name&limit=100&offset=0", "GET /api/<collection>/<id>  -> one document", "Accents and case are ignored. Numbers match exactly, text by contains; field:value also searches inside lists.", "status values: done (ported), partial, todo (not ported), skip (quest step skippable offline)", "Examples: /api/items?q=kiem go | /api/quests/2 | /api/npcs?status=todo&fields=id,name | /api/maps/22 | /api/npcs?q=maps:\"lang tone\" | /api/constraints?area=Cửa hàng" });
            w.EndObject();
        }

        private static KeyValuePair<int, string> Search(JsonWriter w, Dictionary<string, object> colls, string q, int limit)
        {
            var terms = Terms(q);
            if (terms.Count == 0)
                return Error(400, "q is required, e.g. /api/search?q=tajima");
            limit = Math.Min(MaxLimit, Math.Max(1, limit));
            int total = 0;
            w.BeginObject();
            w.Prop("q", q);
            w.Key("hits");
            w.BeginArray();
            foreach (var kv in colls)
            {
                var list = kv.Value as List<object>;
                if (list == null)
                    continue;
                foreach (var o in list)
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null || !Matches(d, terms))
                        continue;
                    total++;
                    if (total > limit)
                        continue;
                    w.BeginObject();
                    w.Prop("collection", kv.Key);
                    w.Prop("id", d["id"]);
                    w.Prop("name", First(d, "name", "title", "rule", "desc", "covers"));
                    if (d.ContainsKey("status"))
                        w.Prop("status", d["status"]);
                    w.Prop("url", "/api/" + kv.Key + "/" + Uri.EscapeDataString(Convert.ToString(d["id"], CultureInfo.InvariantCulture)));
                    w.EndObject();
                }
            }

            w.EndArray();
            w.Prop("total", total);
            w.EndObject();
            return Ok(w);
        }

        private static object First(Dictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.ContainsKey(k))
                    return d[k];
            return null;
        }

        // ---- matching (same rules as the web page) ----
        private sealed class Term
        {
            public string Field, Value;
        }

        // words, "quoted phrases" and field:value / field:"quoted value"
        private static List<Term> Terms(string q)
        {
            var list = new List<Term>();
            if (string.IsNullOrEmpty(q))
                return list;
            int i = 0;
            while (i < q.Length)
            {
                while (i < q.Length && char.IsWhiteSpace(q[i]))
                    i++;
                if (i >= q.Length)
                    break;
                string field = null;
                int colon = -1;
                for (int j = i; j < q.Length && !char.IsWhiteSpace(q[j]) && q[j] != '"'; j++)
                    if (q[j] == ':')
                    {
                        colon = j;
                        break;
                    }

                if (colon > i)
                {
                    field = q.Substring(i, colon - i);
                    i = colon + 1;
                }

                string value;
                if (i < q.Length && q[i] == '"')
                {
                    int end = q.IndexOf('"', i + 1);
                    if (end < 0)
                        end = q.Length;
                    value = q.Substring(i + 1, end - i - 1);
                    i = Math.Min(q.Length, end + 1);
                }
                else
                {
                    int start = i;
                    while (i < q.Length && !char.IsWhiteSpace(q[i]))
                        i++;
                    value = q.Substring(start, i - start);
                }

                list.Add(new Term { Field = field, Value = Fold(value) });
            }

            return list;
        }

        private static bool Matches(Dictionary<string, object> d, List<Term> terms)
        {
            foreach (var t in terms)
            {
                if (t.Field != null)
                {
                    object v;
                    if (!d.TryGetValue(t.Field, out v) || !MatchValue(v, t.Value, true))
                        return false;
                }
                else if (!MatchValue(d, t.Value, false))
                    return false;
            }

            return true;
        }

        // exact: numbers must be equal (for field:value); otherwise any text containing the word
        private static bool MatchValue(object v, string want, bool exactNumbers)
        {
            if (v == null)
                return false;
            var l = v as List<object>;
            if (l != null)
            {
                foreach (var x in l)
                    if (MatchValue(x, want, exactNumbers))
                        return true;
                return false;
            }

            var d = v as Dictionary<string, object>;
            if (d != null)
            {
                foreach (var x in d.Values)
                    if (MatchValue(x, want, exactNumbers))
                        return true;
                return false;
            }

            string s = v is bool ? ((bool)v ? "true" : "false") : Convert.ToString(v, CultureInfo.InvariantCulture);
            if (exactNumbers && (v is long || v is double || IsInt(want)))
                return s == want;
            return Fold(s).Contains(want);
        }

        private static bool IsInt(string s)
        {
            long n;
            return long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out n);
        }

        // lower case without Vietnamese accents (đ -> d)
        public static string Fold(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(ch == 'đ' || ch == 'Đ' ? 'd' : char.ToLowerInvariant(ch));
            }

            return sb.ToString();
        }

        // ---- query string ----
        private static Dictionary<string, string> ParseQuery(string query)
        {
            var d = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query))
                return d;
            foreach (var pair in query.Split('&'))
            {
                if (pair.Length == 0)
                    continue;
                int e = pair.IndexOf('=');
                string k = Decode(e < 0 ? pair : pair.Substring(0, e)), v = e < 0 ? "" : Decode(pair.Substring(e + 1));
                d[k] = v;
            }

            return d;
        }

        private static string Decode(string s)
        {
            return Uri.UnescapeDataString(s.Replace('+', ' '));
        }

        private static string Arg(Dictionary<string, string> a, string k)
        {
            string v;
            return a.TryGetValue(k, out v) ? v : null;
        }

        private static int IntArg(Dictionary<string, string> a, string k, int def)
        {
            int n;
            return int.TryParse(Arg(a, k), out n) ? n : def;
        }

        private static KeyValuePair<int, string> Ok(JsonWriter w)
        {
            return new KeyValuePair<int, string>(200, w.ToString());
        }

        private static KeyValuePair<int, string> Error(int code, string message)
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Prop("error", message);
            w.EndObject();
            return new KeyValuePair<int, string>(code, w.ToString());
        }
    }
}
