using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NsoServer.Store
{
    public sealed class UserRecord
    {
        public int Id;
        public string Username;
        public string Salt;
        public string PasswordHash;
    }

    public sealed class PlayerRecord
    {
        public int Id;
        public int UserId;
        public string Name;
        public sbyte Gender;
        public short Head;
        public sbyte ClassId;
        public long Exp;
        public short Weapon = -1;
        public short Body = -1;
        public short Leg = -1;
        public long LastLogout;
        // where the character stands (defaults of the original `players.map`: Lang Tone)
        public int MapId = 22, X = 1741, Y = 264;
        public int TaskId;   // main quest progress; unlocks maps
        // Reserved for later phases: bag, skills, task progress, etc. (JSON, same shape as the Java `players.data`)
        public string Data = "{}";
    }

    // Local replacement for the MySQL `users` / `players` tables. Tab-separated files, rewritten after each change.
    public sealed class AccountStore
    {
        private readonly object sync = new object();
        private readonly string usersFile, playersFile;
        private readonly List<UserRecord> users = new List<UserRecord>();
        private readonly List<PlayerRecord> players = new List<PlayerRecord>();

        public AccountStore(string directory)
        {
            Directory.CreateDirectory(directory);
            usersFile = Path.Combine(directory, "users.tsv");
            playersFile = Path.Combine(directory, "players.tsv");
            Load();
        }

        public enum LoginResult { Ok, Created, WrongPassword, BadName }

        // Offline mode: an unknown username is registered on first login (there is no web registration page).
        public LoginResult Login(string username, string password, out UserRecord user)
        {
            user = null;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || !IsAlnum(username))
                return LoginResult.BadName;
            lock (sync)
            {
                user = users.Find(u => u.Username == username);
                if (user != null)
                    return Hash(user.Salt, password) == user.PasswordHash ? LoginResult.Ok : LoginResult.WrongPassword;
                string salt = Guid.NewGuid().ToString("N");
                user = new UserRecord
                {
                    Id = users.Count == 0 ? 1 : users[users.Count - 1].Id + 1,
                    Username = username,
                    Salt = salt,
                    PasswordHash = Hash(salt, password)
                };
                users.Add(user);
                Save();
                return LoginResult.Created;
            }
        }

        public List<PlayerRecord> GetPlayers(int userId)
        {
            lock (sync)
            {
                var list = players.FindAll(p => p.UserId == userId);
                list.Sort((a, b) => b.LastLogout.CompareTo(a.LastLogout));
                return list.Count > 3 ? list.GetRange(0, 3) : list;
            }
        }

        public bool NameTaken(string name)
        {
            lock (sync) return players.Exists(p => p.Name == name);
        }

        public PlayerRecord AddPlayer(int userId, string name, sbyte gender, short head)
        {
            lock (sync)
            {
                var p = new PlayerRecord
                {
                    Id = players.Count == 0 ? 1 : players[players.Count - 1].Id + 1,
                    UserId = userId,
                    Name = name,
                    Gender = gender,
                    Head = head
                };
                players.Add(p);
                Save();
                return p;
            }
        }

        // Persists changes made to PlayerRecord objects (position, progress).
        public void Flush()
        {
            lock (sync) Save();
        }

        private static bool IsAlnum(string s)
        {
            foreach (char c in s)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))) return false;
            return true;
        }

        private static string Hash(string salt, string password)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
        }

        private static string Esc(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string Unesc(string s)
        {
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

        private void Load()
        {
            if (File.Exists(usersFile))
                foreach (var line in File.ReadAllLines(usersFile, Encoding.UTF8))
                {
                    var f = line.Split('\t');
                    if (f.Length < 4) continue;
                    users.Add(new UserRecord { Id = int.Parse(f[0]), Username = Unesc(f[1]), Salt = f[2], PasswordHash = f[3] });
                }
            if (File.Exists(playersFile))
                foreach (var line in File.ReadAllLines(playersFile, Encoding.UTF8))
                {
                    var f = line.Split('\t');
                    if (f.Length < 12) continue;
                    var rec = new PlayerRecord
                    {
                        Id = int.Parse(f[0]),
                        UserId = int.Parse(f[1]),
                        Name = Unesc(f[2]),
                        Gender = sbyte.Parse(f[3]),
                        Head = short.Parse(f[4]),
                        ClassId = sbyte.Parse(f[5]),
                        Exp = long.Parse(f[6]),
                        Weapon = short.Parse(f[7]),
                        Body = short.Parse(f[8]),
                        Leg = short.Parse(f[9]),
                        LastLogout = long.Parse(f[10]),
                        Data = Unesc(f[11])
                    };
                    if (f.Length >= 15) { rec.MapId = int.Parse(f[12]); rec.X = int.Parse(f[13]); rec.Y = int.Parse(f[14]); }
                    if (f.Length >= 16) rec.TaskId = int.Parse(f[15]);
                    players.Add(rec);
                }
        }

        private void Save()
        {
            var u = new StringBuilder();
            foreach (var x in users)
                u.Append(x.Id).Append('\t').Append(Esc(x.Username)).Append('\t').Append(x.Salt).Append('\t').Append(x.PasswordHash).Append('\n');
            WriteAtomic(usersFile, u.ToString());
            var p = new StringBuilder();
            foreach (var x in players)
                p.Append(x.Id).Append('\t').Append(x.UserId).Append('\t').Append(Esc(x.Name)).Append('\t').Append(x.Gender).Append('\t')
                 .Append(x.Head).Append('\t').Append(x.ClassId).Append('\t').Append(x.Exp).Append('\t').Append(x.Weapon).Append('\t')
                 .Append(x.Body).Append('\t').Append(x.Leg).Append('\t').Append(x.LastLogout).Append('\t').Append(Esc(x.Data)).Append('\t')
                 .Append(x.MapId).Append('\t').Append(x.X).Append('\t').Append(x.Y).Append('\t').Append(x.TaskId).Append('\n');
            WriteAtomic(playersFile, p.ToString());
        }

        private static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text, new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }
}
