using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace NsoServer.Store
{
    /// <summary>
    /// The save database (offline replacement for the Java server's MySQL `users` / `players` tables, and for the
    /// former users.tsv / players.tsv files): one SQLite file in the save folder.
    /// Tables: users, players (the character columns plus `data`, the JSON of bag, equipment, quest, skills...,
    /// same shape as the Java `players.data`), kv (named JSON documents such as the AI agents) and meta
    /// (schema version). Not thread-safe: AccountStore calls it under its own lock.
    /// Native library: Assets/Plugins/Sqlite (Microsoft.Data.Sqlite 8.0.11 + SQLitePCLRaw e_sqlite3 2.1.10).
    /// </summary>
    public sealed class SqliteSaveDb : IDisposable
    {
        public const int SchemaVersion = 1;
        private static readonly object InitLock = new object ();
        private static bool initialized;
        private readonly SqliteConnection conn;
        public string FilePath { get; private set; }

        public SqliteSaveDb(string file)
        {
            lock (InitLock)
            {
                // Microsoft.Data.Sqlite.Core does not pick a native provider by itself
                if (!initialized)
                {
                    SQLitePCL.Batteries_V2.Init();
                    initialized = true;
                }
            }

            FilePath = file;
            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = file,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };
            conn = new SqliteConnection(cs.ToString());
            conn.Open();
            Exec("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
            Exec(@"CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY, username TEXT NOT NULL UNIQUE, salt TEXT NOT NULL, password_hash TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS players (
  id INTEGER PRIMARY KEY, user_id INTEGER NOT NULL, name TEXT NOT NULL, gender INTEGER NOT NULL, head INTEGER NOT NULL,
  class_id INTEGER NOT NULL, exp INTEGER NOT NULL, weapon INTEGER NOT NULL, body INTEGER NOT NULL, leg INTEGER NOT NULL,
  last_logout INTEGER NOT NULL, map_id INTEGER NOT NULL, x INTEGER NOT NULL, y INTEGER NOT NULL, task_id INTEGER NOT NULL,
  data TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS players_user ON players(user_id);
CREATE INDEX IF NOT EXISTS players_name ON players(name);
CREATE TABLE IF NOT EXISTS kv (key TEXT PRIMARY KEY, value TEXT NOT NULL, updated_at INTEGER NOT NULL);
INSERT OR IGNORE INTO meta (key, value) VALUES ('schema_version', '" + SchemaVersion + "');");
        }

        private void Exec(string sql)
        {
            using (var c = conn.CreateCommand())
            {
                c.CommandText = sql;
                c.ExecuteNonQuery();
            }
        }

        /// <summary>True when the database has no account yet (a new file: the TSV files may be imported).</summary>
        public bool IsEmpty
        {
            get
            {
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = "SELECT COUNT(*) FROM users";
                    return Convert.ToInt64(c.ExecuteScalar()) == 0;
                }
            }
        }

        public void Load(List<UserRecord> users, List<PlayerRecord> players)
        {
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT id, username, salt, password_hash FROM users ORDER BY id";
                using (var r = c.ExecuteReader())
                    while (r.Read())
                        users.Add(new UserRecord { Id = r.GetInt32(0), Username = r.GetString(1), Salt = r.GetString(2), PasswordHash = r.GetString(3) });
            }

            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT id, user_id, name, gender, head, class_id, exp, weapon, body, leg, last_logout, map_id, x, y, task_id, data FROM players ORDER BY id";
                using (var r = c.ExecuteReader())
                    while (r.Read())
                        players.Add(new PlayerRecord { Id = r.GetInt32(0), UserId = r.GetInt32(1), Name = r.GetString(2), Gender = (sbyte)r.GetInt32(3), Head = (short)r.GetInt32(4), ClassId = (sbyte)r.GetInt32(5), Exp = r.GetInt64(6), Weapon = (short)r.GetInt32(7), Body = (short)r.GetInt32(8), Leg = (short)r.GetInt32(9), LastLogout = r.GetInt64(10), MapId = r.GetInt32(11), X = r.GetInt32(12), Y = r.GetInt32(13), TaskId = r.GetInt32(14), Data = r.GetString(15) });
            }
        }

        /// <summary>Writes every user and character in one transaction (insert or update by id; nothing is deleted,
        /// as with the TSV files, which never removed a character either).</summary>
        public void SaveAll(List<UserRecord> users, List<PlayerRecord> players)
        {
            using (var tx = conn.BeginTransaction())
            {
                using (var c = conn.CreateCommand())
                {
                    c.Transaction = tx;
                    c.CommandText = @"INSERT INTO users (id, username, salt, password_hash) VALUES ($id, $u, $s, $h)
ON CONFLICT(id) DO UPDATE SET username = excluded.username, salt = excluded.salt, password_hash = excluded.password_hash";
                    var id = c.Parameters.Add("$id", SqliteType.Integer);
                    var u = c.Parameters.Add("$u", SqliteType.Text);
                    var s = c.Parameters.Add("$s", SqliteType.Text);
                    var h = c.Parameters.Add("$h", SqliteType.Text);
                    foreach (var x in users)
                    {
                        id.Value = x.Id;
                        u.Value = x.Username ?? "";
                        s.Value = x.Salt ?? "";
                        h.Value = x.PasswordHash ?? "";
                        c.ExecuteNonQuery();
                    }
                }

                using (var c = conn.CreateCommand())
                {
                    c.Transaction = tx;
                    c.CommandText = @"INSERT INTO players (id, user_id, name, gender, head, class_id, exp, weapon, body, leg, last_logout, map_id, x, y, task_id, data)
VALUES ($id, $uid, $name, $g, $head, $cls, $exp, $w, $b, $l, $lo, $map, $x, $y, $task, $data)
ON CONFLICT(id) DO UPDATE SET user_id = excluded.user_id, name = excluded.name, gender = excluded.gender, head = excluded.head,
  class_id = excluded.class_id, exp = excluded.exp, weapon = excluded.weapon, body = excluded.body, leg = excluded.leg,
  last_logout = excluded.last_logout, map_id = excluded.map_id, x = excluded.x, y = excluded.y, task_id = excluded.task_id, data = excluded.data";
                    var p = new Dictionary<string, SqliteParameter>();
                    foreach (var n in new[]
                    {
                        "$id",
                        "$uid",
                        "$g",
                        "$head",
                        "$cls",
                        "$exp",
                        "$w",
                        "$b",
                        "$l",
                        "$lo",
                        "$map",
                        "$x",
                        "$y",
                        "$task"
                    }

                    )
                        p[n] = c.Parameters.Add(n, SqliteType.Integer);
                    p["$name"] = c.Parameters.Add("$name", SqliteType.Text);
                    p["$data"] = c.Parameters.Add("$data", SqliteType.Text);
                    foreach (var x in players)
                    {
                        p["$id"].Value = x.Id;
                        p["$uid"].Value = x.UserId;
                        p["$name"].Value = x.Name ?? "";
                        p["$g"].Value = (int)x.Gender;
                        p["$head"].Value = (int)x.Head;
                        p["$cls"].Value = (int)x.ClassId;
                        p["$exp"].Value = x.Exp;
                        p["$w"].Value = (int)x.Weapon;
                        p["$b"].Value = (int)x.Body;
                        p["$l"].Value = (int)x.Leg;
                        p["$lo"].Value = x.LastLogout;
                        p["$map"].Value = x.MapId;
                        p["$x"].Value = x.X;
                        p["$y"].Value = x.Y;
                        p["$task"].Value = x.TaskId;
                        p["$data"].Value = x.Data ?? "{}";
                        c.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
        }

        /// <summary>A named JSON document (kv table), or null.</summary>
        public string GetDocument(string key)
        {
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT value FROM kv WHERE key = $k";
                c.Parameters.AddWithValue("$k", key);
                var o = c.ExecuteScalar();
                return o == null || o is DBNull ? null : (string)o;
            }
        }

        public void SetDocument(string key, string value)
        {
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "INSERT INTO kv (key, value, updated_at) VALUES ($k, $v, $t) ON CONFLICT(key) DO UPDATE SET value = excluded.value, updated_at = excluded.updated_at";
                c.Parameters.AddWithValue("$k", key);
                c.Parameters.AddWithValue("$v", value ?? "");
                c.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                c.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            try
            {
                Exec("PRAGMA wal_checkpoint(TRUNCATE);");
            }
            catch (Exception)
            {
            }

            conn.Dispose();
        }
    }
}
