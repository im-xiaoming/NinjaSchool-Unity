using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class

    /// <summary>
    /// Offline debugging aid (no Java counterpart): a per-session text log of everything the player does and is.
    /// - IN    every message the client sends (the player's actions), with the player's position/HP at that moment;
    /// - OUT   every message the server sends back (optional, <see cref = "LogOutgoing"/>);
    /// - LOG   every GameServer.Log line, unfiltered (the Unity Console only shows lines containing "error");
    /// - STATE a full snapshot of each online character every <see cref = "SnapshotSeconds"/> seconds.
    /// Destinations: the log database player_log.db (PlayerLogDb, one session per Play, <see cref = "ToDatabase"/>) and/or
    /// one text file per Play session (<see cref = "ToFile"/>); the newest <see cref = "KeepFiles"/> sessions / files are kept.
    /// Thread-safe; buffered and flushed from the server tick (at most half a second of lines can be lost on a hard crash).
    /// </summary>
    public static class PlayerLog
    {
        /// <summary>Write the log at all (NsoServerControl, applies on next Play).</summary>
        public static bool Enabled = true;
        /// <summary>Also log server-to-client messages (much larger files).</summary>
        public static bool LogOutgoing = true;
        /// <summary>Seconds between two STATE snapshots of a character (0 = no snapshots).</summary>
        public static int SnapshotSeconds = 5;
        /// <summary>Payload bytes shown in hex per message; the rest is summarised by the length.</summary>
        public static int MaxPayloadBytes = 48;
        /// <summary>Old log files beyond this count are deleted when a new session starts.</summary>
        public static int KeepFiles = 20;
        /// <summary>Write the lines to the SQLite log database (Logs/PlayerLog/player_log.db).</summary>
        public static bool ToDatabase = true;
        /// <summary>Also write the classic text file player_*.log.</summary>
        public static bool ToFile = false;
        public const string DatabaseFile = "player_log.db";
        /// <summary>AI players (bots) are logged in their own table of the log database (bot_entries), not in the
        /// player's session; their lines older than this many days are deleted when the server starts.</summary>
        public static int BotKeepDays = 20;
        /// <summary>Log the AI players' actions and states at all.</summary>
        public static bool LogBots = true;
        private static readonly object Sync = new object ();
        private static StreamWriter writer;
        private static PlayerLogDb db;
        private static List<PlayerLogDb.Line> pending = new List<PlayerLogDb.Line>();
        private static List<PlayerLogDb.BotLine> pendingBots = new List<PlayerLogDb.BotLine>();
        private static readonly object DbLock = new object (); // one Flush at a time (inserts happen outside Sync)
        private static bool On
        {
            get
            {
                return writer != null || db != null;
            }
        }

        private static readonly Dictionary<PlayerState, long> LastSnapshot = new Dictionary<PlayerState, long>();
        /// <summary>The file being written, or null.</summary>
        public static string FilePath { get; private set; }
        /// <summary>The log database and the current session id (for display), or null.</summary>
        public static string DatabasePath { get; private set; }

        public static long SessionId
        {
            get
            {
                return db != null ? db.SessionId : 0;
            }
        }

        /// <summary>Starts a new log file in the folder (called by ServerBootstrap when the server starts).</summary>
        public static void Open(string folder)
        {
            if (!Enabled || string.IsNullOrEmpty(folder))
                return;
            lock (Sync)
            {
                CloseLocked();
                if (ToDatabase)
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                        db = new PlayerLogDb(Path.Combine(folder, DatabaseFile), KeepFiles, BotKeepDays);
                        DatabasePath = db.FilePath;
                    }
                    catch (Exception)
                    {
                        db = null;
                        DatabasePath = null;
                    } // logging must never stop the game
                }

                if (!ToFile)
                    return;
                try
                {
                    Directory.CreateDirectory(folder);
                    Prune(folder);
                    FilePath = Path.Combine(folder, "player_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log");
                    writer = new StreamWriter(FilePath, false, new UTF8Encoding(false), 1 << 16);
                    writer.WriteLine("# NSO offline player log, started " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine("# time | kind | details. IN = client action, OUT = server reply, LOG = server trace, STATE = snapshot");
                    writer.Flush();
                }
                catch (Exception)
                {
                    writer = null;
                    FilePath = null;
                } // logging must never stop the game
            }
        }

        /// <summary>Flushes and closes the file (server shutdown).</summary>
        public static void Close()
        {
            Flush();
            lock (Sync)
                CloseLocked();
        }

        private static void CloseLocked()
        {
            if (writer != null)
            {
                try
                {
                    writer.Flush();
                    writer.Dispose();
                }
                catch (Exception)
                {
                }
            }

            writer = null;
            if (db != null)
            {
                lock (DbLock)
                {
                    try
                    {
                        db.Insert(pending);
                        db.InsertBots(pendingBots);
                        db.Dispose();
                    }
                    catch (Exception)
                    {
                    }

                    db = null;
                }
            }

            pending.Clear();
            pendingBots.Clear();
            LastSnapshot.Clear();
        }

        private static void Prune(string folder)
        {
            var files = Directory.GetFiles(folder, "player_*.log");
            Array.Sort(files, StringComparer.Ordinal); // names sort by time
            for (int i = 0; i < files.Length - Math.Max(0, KeepFiles - 1); i++)
            {
                try
                {
                    File.Delete(files[i]);
                }
                catch (Exception)
                {
                }
            }
        }

        private static void Write(string kind, string text)
        {
            if (!On)
                return;
            var now = DateTime.Now;
            string time = now.ToString("HH:mm:ss.fff");
            lock (Sync)
            {
                if (db != null && pending.Count < 200000) // a stuck database must not eat the memory
                    pending.Add(new PlayerLogDb.Line { Ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Time = time, Kind = kind.Trim(), Text = text });
                if (writer == null)
                    return;
                try
                {
                    writer.Write(time);
                    writer.Write(" | ");
                    writer.Write(kind);
                    writer.Write(" | ");
                    writer.WriteLine(text);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>Writes buffered lines to disk (server tick): the text file, then one database transaction.</summary>
        public static void Flush()
        {
            if (!On)
                return;
            List<PlayerLogDb.Line> batch = null;
            List<PlayerLogDb.BotLine> bots = null;
            lock (Sync)
            {
                try
                {
                    if (writer != null)
                        writer.Flush();
                }
                catch (Exception)
                {
                }

                if (db != null && pending.Count > 0)
                {
                    batch = pending;
                    pending = new List<PlayerLogDb.Line>();
                }

                if (db != null && pendingBots.Count > 0)
                {
                    bots = pendingBots;
                    pendingBots = new List<PlayerLogDb.BotLine>();
                }
            }

            if (batch == null && bots == null)
                return;
            lock (DbLock)
            {
                try
                {
                    if (db != null && batch != null)
                        db.Insert(batch);
                }
                catch (Exception)
                {
                }

                try
                {
                    if (db != null && bots != null)
                        db.InsertBots(bots);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>A message from the client: the player's action. <paramref name = "me"/> is null before the character enters the game.</summary>
        public static void In(Session s, PlayerState me, Message m)
        {
            if (!On)
                return;
            if (s.IsBot)
            {
                if (!LogBots || m.Command == Cmd.PLAYER_MOVE)
                    return; // every step of 13 walkers would bury the rest
                var bb = new StringBuilder(120);
                bb.Append(CmdName(m.Command, m.Data));
                AppendPayload(bb, m.Data);
                if (me != null)
                    bb.Append(" @ ").Append(Brief(me));
                BotLine(s, "IN", bb.ToString());
                return;
            }

            var b = new StringBuilder(160);
            b.Append('#').Append(s.Id).Append(' ').Append(CmdName(m.Command, m.Data));
            AppendPayload(b, m.Data);
            if (me != null)
                b.Append(" @ ").Append(Brief(me));
            Write("IN ", b.ToString());
        }

        /// <summary>A message to the client (the server's reply).</summary>
        public static void Out(Session s, sbyte command, byte[] data)
        {
            if (!On || !LogOutgoing || s.IsBot)
                return;
            var b = new StringBuilder(96);
            b.Append('#').Append(s.Id).Append(' ').Append(CmdName(command, data));
            AppendPayload(b, data);
            Write("OUT", b.ToString());
        }

        /// <summary>A GameServer.Log line.</summary>
        public static void Trace(string text)
        {
            Write("LOG", text);
        }

        /// <summary>A named event with the player's state (map change, death, quest step...).</summary>
        public static void Event(PlayerState me, string text)
        {
            if (me != null && me.Session != null && me.Session.IsBot)
            {
                BotLine(me.Session, "EVT", text + " @ " + Brief(me));
                return;
            }

            Write("EVT", text + (me != null ? " @ " + Brief(me) : ""));
        }

        /// <summary>A line of an AI player (table bot_entries): its client messages (IN), events (EVT), what its screen
        /// showed (MSG), its AI decisions (AI) and periodic snapshots (STATE).</summary>
        public static void BotLine(Session s, string kind, string text)
        {
            Bot(s != null ? s.BotAccount : "?", kind, text);
        }

        public static void Bot(string bot, string kind, string text)
        {
            if (db == null || !LogBots)
                return;
            var now = DateTime.Now;
            lock (Sync)
            {
                if (db != null && pendingBots.Count < 200000)
                    pendingBots.Add(new PlayerLogDb.BotLine { Ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Time = now.ToString("MM-dd HH:mm:ss.fff"), Bot = bot ?? "?", Kind = kind, Text = text ?? "" });
            }
        }

        /// <summary>STATE snapshot of the character when its interval is due (server tick, every half second).</summary>
        public static void Tick(PlayerState me, long now)
        {
            if (!On || SnapshotSeconds <= 0 || me == null || me.Record == null)
                return;
            long last;
            lock (Sync)
            {
                int every = me.Session != null && me.Session.IsBot ? Math.Max(30, SnapshotSeconds) : SnapshotSeconds; // AI players: every 30 s
                if (LastSnapshot.TryGetValue(me, out last) && now - last < every * 1000L)
                    return;
                LastSnapshot[me] = now;
            }

            if (me.Session != null && me.Session.IsBot)
                BotLine(me.Session, "STATE", Full(me));
            else
                Write("STATE", Full(me));
        }

        /// <summary>Forgets a character that left the game.</summary>
        public static void Forget(PlayerState me)
        {
            lock (Sync)
                LastSnapshot.Remove(me);
        }

        private static string Brief(PlayerState me)
        {
            var b = new StringBuilder(80);
            if (me.Zone != null)
                b.Append("map ").Append(me.Zone.Tile.Id).Append('/').Append(me.Zone.Id);
            b.Append(" (").Append(me.X).Append(',').Append(me.Y).Append(')');
            b.Append(" hp ").Append(me.Hp).Append('/').Append(me.MaxHp);
            if (me.IsDead)
                b.Append(" DEAD");
            if (me.IsChangeMap)
                b.Append(" changing-map");
            return b.ToString();
        }

        private static string Full(PlayerState me)
        {
            var b = new StringBuilder(256);
            b.Append(me.Record.Name).Append(" id ").Append(me.Record.Id).Append(' ').Append(Brief(me));
            b.Append(" mp ").Append(me.Mp).Append('/').Append(me.MaxMp);
            b.Append(" lv ").Append(me.Level).Append(" exp ").Append(me.Exp);
            b.Append(" class ").Append(me.Record.ClassId).Append(" pk ").Append(me.TypePk);
            var p = me.Progress;
            if (p != null)
            {
                b.Append(" xu ").Append(p.Xu).Append(" yen ").Append(p.Yen).Append(" luong ").Append(p.Luong);
                b.Append(" task ").Append(me.Record.TaskId).Append(p.TaskActive ? "." + p.TaskIndex + " count " + p.TaskCount : " (not taken)");
            }

            b.Append(" dmg ").Append(me.Damage2).Append('-').Append(me.Damage);
            if (me.SelectedSkill != null)
                b.Append(" skill ").Append(me.SelectedSkill.Id);
            b.Append(" effects ").Append(me.Effects.Count);
            if (me.IsAutoPlay)
                b.Append(" autoplay");
            if (me.IsInvisible)
                b.Append(" invisible");
            if (Escort.IsLeading(me))
                b.Append(" escorting");
            return b.ToString();
        }

        private static void AppendPayload(StringBuilder b, byte[] data)
        {
            int n = data == null ? 0 : data.Length;
            b.Append(" len ").Append(n);
            if (n == 0 || MaxPayloadBytes <= 0)
                return;
            b.Append(" [");
            int shown = Math.Min(n, MaxPayloadBytes);
            for (int i = 0; i < shown; i++)
                b.Append(data[i].ToString("x2"));
            if (shown < n)
                b.Append("..");
            b.Append(']');
        }

        // Command names of the original CMD table (top level); sub-commands show their first payload byte.
        private static readonly Dictionary<int, string> Names = new Dictionary<int, string>
        {
            {
                0,
                "PLAYER_DIE"
            },
            {
                1,
                "PLAYER_MOVE"
            },
            {
                2,
                "PLAYER_REMOVE"
            },
            {
                3,
                "PLAYER_ADD"
            },
            {
                11,
                "ITEM_USE"
            },
            {
                12,
                "ITEM_USE_CHANGE_MAP"
            },
            {
                13,
                "ITEM_BUY"
            },
            {
                14,
                "ITEM_SALE"
            },
            {
                15,
                "ITEM_BODY_TO_BAG"
            },
            {
                16,
                "ITEM_BOX_TO_BAG"
            },
            {
                17,
                "ITEM_BAG_TO_BOX"
            },
            {
                19,
                "UPPEARL"
            },
            {
                20,
                "UPPEARL_LOCK"
            },
            {
                21,
                "UPGRADE"
            },
            {
                22,
                "SPLIT"
            },
            {
                28,
                "ZONE_CHANGE"
            },
            {
                29,
                "MENU"
            },
            {
                31,
                "OPEN_UI_BOX"
            },
            {
                36,
                "OPEN_UI_ZONE"
            },
            {
                40,
                "OPEN_UI_MENU"
            },
            {
                41,
                "SKILL_SELECT"
            },
            {
                42,
                "REQUEST_ITEM_INFO"
            },
            {
                47,
                "TASK_GET"
            },
            {
                52,
                "RESET_POINT"
            },
            {
                53,
                "ALERT_MESSAGE"
            },
            {
                60,
                "PLAYER_ATTACK_NPC"
            },
            {
                61,
                "PLAYER_ATTACK_PLAYER"
            },
            {
                62,
                "HAVE_ATTACK_PLAYER"
            },
            {
                72,
                "ME_DIE_EXP_DOWN"
            },
            {
                74,
                "USE_SKILL_MY_BUFF"
            },
            {
                88,
                "RETURN_POINT_MAP"
            },
            {
                92,
                "OPEN_TEXT_BOX_ID"
            },
            {
                117,
                "MAP_ITEM"
            },
            {
                -2,
                "NPC_ATTACK_PLAYER"
            },
            {
                -3,
                "NPC_ATTACK_ME"
            },
            {
                -9,
                "ME_BACK"
            },
            {
                -10,
                "ME_LIVE"
            },
            {
                -11,
                "ME_DIE"
            },
            {
                -12,
                "ME_THROW"
            },
            {
                -14,
                "ITEMMAP_MYPICK"
            },
            {
                -16,
                "MAP_CLEAR"
            },
            {
                -17,
                "MAP_CHANGE"
            },
            {
                -18,
                "MAP_INFO"
            },
            {
                -23,
                "CHAT_MAP"
            },
            {
                -26,
                "SERVER_DIALOG"
            },
            {
                -27,
                "GET_SESSION_ID"
            },
            {
                -28,
                "NOT_MAP"
            },
            {
                -29,
                "NOT_LOGIN"
            },
            {
                -30,
                "SUB_COMMAND"
            },
            {
                -32,
                "FULL_SIZE"
            },
            {
                -123,
                "ME_LOAD_INFO"
            },
        };
        private static string CmdName(sbyte cmd, byte[] data)
        {
            string name;
            string s = Names.TryGetValue(cmd, out name) ? name + "(" + cmd + ")" : "cmd " + cmd;
            if ((cmd == Cmd.SUB_COMMAND || cmd == Cmd.NOT_MAP || cmd == Cmd.NOT_LOGIN) && data != null && data.Length > 0)
                s += " sub " + (sbyte)data[0];
            return s;
        }
    }
}
