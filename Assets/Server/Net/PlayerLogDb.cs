using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class

    /// <summary>
    /// The log database (offline debugging aid, separate from the save database nso.db): Logs/PlayerLog/player_log.db.
    /// Tables:
    ///   sessions(id, started_at, started_ms)               one row per Play session
    ///   entries(id, session_id, ts_ms, time, kind, text)   kind: IN, OUT, LOG, EVT, STATE (see PlayerLog)
    ///   bot_entries(id, ts_ms, time, bot, kind, text)      the AI players (bots), kept keepBotDays days
    ///     SELECT time, kind, text FROM bot_entries WHERE bot = 'ai_raiden' ORDER BY id DESC LIMIT 100;
    /// Lines are buffered by PlayerLog and inserted in one transaction per Flush (server tick, every 500 ms), so the
    /// game threads never wait for the disk. Not thread-safe: PlayerLog serializes the calls.
    /// Example: SELECT time, kind, text FROM entries WHERE session_id = (SELECT MAX(id) FROM sessions) AND kind = 'EVT';
    /// </summary>
    public sealed class PlayerLogDb : IDisposable
    {
        public struct Line
        {
            public long Ms;
            public string Time, Kind, Text;
        }

        public struct BotLine
        {
            public long Ms;
            public string Time, Bot, Kind, Text;
        }

        private static readonly object InitLock = new object ();
        private static bool initialized;
        private readonly SqliteConnection conn;
        public long SessionId { get; private set; }
        public string FilePath { get; private set; }

        public PlayerLogDb(string file, int keepSessions, int keepBotDays = 20)
        {
            lock (InitLock)
            {
                if (!initialized)
                {
                    SQLitePCL.Batteries_V2.Init();
                    initialized = true;
                }
            }

            FilePath = file;
            conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString());
            conn.Open();
            Exec("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
            Exec(@"CREATE TABLE IF NOT EXISTS sessions (id INTEGER PRIMARY KEY, started_at TEXT NOT NULL, started_ms INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS entries (id INTEGER PRIMARY KEY, session_id INTEGER NOT NULL, ts_ms INTEGER NOT NULL, time TEXT NOT NULL,
  kind TEXT NOT NULL, text TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS entries_session_kind ON entries(session_id, kind);
CREATE TABLE IF NOT EXISTS bot_entries (id INTEGER PRIMARY KEY, ts_ms INTEGER NOT NULL, time TEXT NOT NULL, bot TEXT NOT NULL,
  kind TEXT NOT NULL, text TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS bot_entries_bot_ts ON bot_entries(bot, ts_ms);
CREATE INDEX IF NOT EXISTS bot_entries_ts ON bot_entries(ts_ms);");
            // bot lines older than keepBotDays days are forgotten
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "DELETE FROM bot_entries WHERE ts_ms < $cut";
                c.Parameters.AddWithValue("$cut", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Math.Max(1, keepBotDays) * 86400000L);
                c.ExecuteNonQuery();
            }

            // forget the oldest sessions (PlayerLog.KeepFiles applies to sessions here)
            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"DELETE FROM entries WHERE session_id NOT IN (SELECT id FROM sessions ORDER BY id DESC LIMIT $keep);
DELETE FROM sessions WHERE id NOT IN (SELECT id FROM sessions ORDER BY id DESC LIMIT $keep);";
                c.Parameters.AddWithValue("$keep", Math.Max(0, keepSessions - 1));
                c.ExecuteNonQuery();
            }

            using (var c = conn.CreateCommand())
            {
                c.CommandText = "INSERT INTO sessions (started_at, started_ms) VALUES ($at, $ms); SELECT last_insert_rowid();";
                c.Parameters.AddWithValue("$at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                c.Parameters.AddWithValue("$ms", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                SessionId = Convert.ToInt64(c.ExecuteScalar());
            }
        }

        private void Exec(string sql)
        {
            using (var c = conn.CreateCommand())
            {
                c.CommandText = sql;
                c.ExecuteNonQuery();
            }
        }

        /// <summary>Inserts buffered lines in one transaction.</summary>
        public void Insert(List<Line> lines)
        {
            if (lines.Count == 0)
                return;
            using (var tx = conn.BeginTransaction())
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "INSERT INTO entries (session_id, ts_ms, time, kind, text) VALUES ($s, $ms, $t, $k, $x)";
                var s = c.Parameters.Add("$s", SqliteType.Integer);
                var ms = c.Parameters.Add("$ms", SqliteType.Integer);
                var t = c.Parameters.Add("$t", SqliteType.Text);
                var k = c.Parameters.Add("$k", SqliteType.Text);
                var x = c.Parameters.Add("$x", SqliteType.Text);
                s.Value = SessionId;
                foreach (var l in lines)
                {
                    ms.Value = l.Ms;
                    t.Value = l.Time;
                    k.Value = l.Kind;
                    x.Value = l.Text ?? "";
                    c.ExecuteNonQuery();
                }

                tx.Commit();
            }
        }

        /// <summary>Inserts buffered bot lines in one transaction.</summary>
        public void InsertBots(List<BotLine> lines)
        {
            if (lines.Count == 0)
                return;
            using (var tx = conn.BeginTransaction())
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "INSERT INTO bot_entries (ts_ms, time, bot, kind, text) VALUES ($ms, $t, $b, $k, $x)";
                var ms = c.Parameters.Add("$ms", SqliteType.Integer);
                var t = c.Parameters.Add("$t", SqliteType.Text);
                var b = c.Parameters.Add("$b", SqliteType.Text);
                var k = c.Parameters.Add("$k", SqliteType.Text);
                var x = c.Parameters.Add("$x", SqliteType.Text);
                foreach (var l in lines)
                {
                    ms.Value = l.Ms;
                    t.Value = l.Time;
                    b.Value = l.Bot;
                    k.Value = l.Kind;
                    x.Value = l.Text ?? "";
                    c.ExecuteNonQuery();
                }

                tx.Commit();
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
