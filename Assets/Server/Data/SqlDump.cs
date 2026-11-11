using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NsoServer.Data
{
    public sealed class SqlTable
    {
        public string Name;
        public List<string> Columns = new List<string>();
        // Each cell is null (SQL NULL) or its text; numbers keep their literal text.
        public List<string[]> Rows = new List<string[]>();

        public int Col(string name)
        {
            int i = Columns.IndexOf(name);
            if (i < 0) throw new KeyNotFoundException(Name + "." + name);
            return i;
        }
    }

    // Compact text form of the static tables (no accounts, payments or other personal data). Written once by a tool,
    // read at runtime: "#table<TAB>col1<TAB>col2..." then one escaped row per line; NULL is written as \N.
    public static class TableFile
    {
        public static readonly string[] StaticTables =
        {
            "clazz", "effect", "effect_data", "effect_data_auto", "item", "item_option", "map", "monster",
            "nj_arrow", "nj_effect", "nj_image", "nj_part", "nj_skill", "npc", "options", "others",
            "skill", "skill_option", "skill_template", "stores", "store_data", "store_upgrade",
            "task", "task_template", "weapon_store"
        };

        private static string Esc(string s)
        {
            if (s == null) return "\\N";
            return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string Unesc(string s)
        {
            if (s == "\\N") return null;
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    sb.Append(n == 't' ? '\t' : n == 'n' ? '\n' : n == 'r' ? '\r' : n);
                }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public static void Save(string path, IDictionary<string, SqlTable> tables)
        {
            using (var w = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                foreach (var t in tables.Values)
                {
                    w.Write("#" + t.Name);
                    foreach (var c in t.Columns) w.Write("\t" + c);
                    w.Write("\n");
                    foreach (var row in t.Rows)
                    {
                        for (int i = 0; i < row.Length; i++) { if (i > 0) w.Write("\t"); w.Write(Esc(row[i])); }
                        w.Write("\n");
                    }
                }
            }
        }

        public static Dictionary<string, SqlTable> Load(string path)
        {
            var tables = new Dictionary<string, SqlTable>();
            SqlTable cur = null;
            foreach (var line in File.ReadLines(path, new UTF8Encoding(false)))
            {
                if (line.Length > 0 && line[0] == '#')
                {
                    var f = line.Substring(1).Split('\t');
                    cur = new SqlTable { Name = f[0] };
                    for (int i = 1; i < f.Length; i++) cur.Columns.Add(f[i]);
                    tables[cur.Name] = cur;
                }
                else if (cur != null)
                {
                    var f = line.Split('\t');
                    for (int i = 0; i < f.Length; i++) f[i] = Unesc(f[i]);
                    cur.Rows.Add(f);
                }
            }
            return tables;
        }
    }

    // Reads the INSERT statements of a mysqldump file (the dump is the original server's database) into memory,
    // so the offline server needs neither MySQL nor MariaDB.
    public static class SqlDump
    {
        public static Dictionary<string, SqlTable> Load(string path, ICollection<string> only = null)
        {
            var tables = new Dictionary<string, SqlTable>();
            using (var r = new StreamReader(path, new UTF8Encoding(false)))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    if (!line.StartsWith("INSERT INTO `", StringComparison.Ordinal)) continue;
                    int e = line.IndexOf('`', 13);
                    string name = line.Substring(13, e - 13);
                    SqlTable t;
                    bool keep = only == null || only.Contains(name);
                    if (!tables.TryGetValue(name, out t)) { t = new SqlTable { Name = name }; if (keep) tables[name] = t; }
                    // always parsed (string values can span lines); unwanted tables are simply not retained
                    new Parser(r, line.Substring(e + 1)).ParseInsert(t);
                }
            }
            return tables;
        }

        private sealed class Parser
        {
            private readonly StreamReader r;
            private string s;
            private int i;

            public Parser(StreamReader r, string firstLineRest) { this.r = r; s = firstLineRest; }

            private bool Fill()
            {
                if (i < s.Length) return true;
                string next = r.ReadLine();
                if (next == null) return false;
                s = "\n" + next; // a string value may continue across lines
                i = 0;
                return true;
            }

            private char Peek() { return Fill() ? s[i] : '\0'; }
            private char Next() { Fill(); return s[i++]; }

            private void SkipWs()
            {
                while (Fill() && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
            }

            public void ParseInsert(SqlTable t)
            {
                SkipWs();
                var cols = new List<string>();
                if (Peek() == '(')
                {
                    Next();
                    while (true)
                    {
                        SkipWs();
                        if (Peek() == '`')
                        {
                            Next();
                            var sb = new StringBuilder();
                            while (Peek() != '`') sb.Append(Next());
                            Next();
                            cols.Add(sb.ToString());
                        }
                        SkipWs();
                        char c = Next();
                        if (c == ')') break;
                    }
                }
                if (t.Columns.Count == 0) t.Columns = cols;
                // "VALUES"
                SkipWs();
                while (char.IsLetter(Peek())) Next();
                while (true)
                {
                    SkipWs();
                    if (Peek() != '(') break;
                    Next();
                    var row = new List<string>();
                    while (true)
                    {
                        SkipWs();
                        row.Add(ParseValue());
                        SkipWs();
                        char c = Next();
                        if (c == ')') break;
                    }
                    t.Rows.Add(row.ToArray());
                    SkipWs();
                    char d = Next();
                    if (d == ';') break;
                }
            }

            private string ParseValue()
            {
                char c = Peek();
                if (c == '\'')
                {
                    Next();
                    var sb = new StringBuilder();
                    while (true)
                    {
                        char ch = Next();
                        if (ch == '\\')
                        {
                            char n = Next();
                            switch (n)
                            {
                                case 'n': sb.Append('\n'); break;
                                case 'r': sb.Append('\r'); break;
                                case 't': sb.Append('\t'); break;
                                case '0': sb.Append('\0'); break;
                                case 'b': sb.Append('\b'); break;
                                case 'Z': sb.Append('\x1a'); break;
                                default: sb.Append(n); break;
                            }
                        }
                        else if (ch == '\'')
                        {
                            if (Peek() == '\'') { Next(); sb.Append('\''); }
                            else break;
                        }
                        else sb.Append(ch);
                    }
                    return sb.ToString();
                }
                var tok = new StringBuilder();
                while (true)
                {
                    char ch = Peek();
                    if (ch == ',' || ch == ')' || ch == '\0') break;
                    tok.Append(Next());
                }
                string v = tok.ToString().Trim();
                return v == "NULL" ? null : v;
            }
        }
    }
}
