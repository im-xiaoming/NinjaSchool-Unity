using System;
using System.Collections.Generic;
using System.IO;
using NsoServer.Data;

namespace NsoServer.World
{
    public sealed class Waypoint
    {
        public short Next, MinX, MinY, MaxX, MaxY, X, Y;
    }

    public sealed class NpcSpawn
    {
        public int Id;
        public int TemplateId, X, Y, Status;
    }

    public sealed class MobSpawn
    {
        public short TemplateId, X, Y;
        public bool IsBeast;
    }

    public sealed class ItemTree
    {
        public int Id, X, Y;
    }

    public sealed class MobTemplate
    {
        public int Id;
        public string Name;
        public sbyte Type, RangeMove;
        public int Hp;
        public short Level;
        public bool IsBoss;
        public sbyte TypeFly, NumberImage;
        public sbyte[] FrameBossMove, IndexSplash;
        public sbyte[][] FrameBossAttack, FrameChar;
        public short[] Sequence;
        public MobImageInfo[] ImageInfo;
        public MobFrame[] Frames;
    }

    public sealed class MobImageInfo
    {
        public int Id, X, Y, W, H;
    }

    public sealed class MobFrame
    {
        public int[] Dx, Dy, IdImg, Flip, OnTop;
    }

    public sealed class ItemTemplateData
    {
        public int Id;
        public sbyte Type, Gender, Level;
        public short Part, Fashion;
        public bool IsUpToUp;
        public string Name = "";
    }

    // Port of com.nsoz.map.TileMap (the part that is loaded from the `map` table).
    public sealed class TileMap
    {
        public const int MapNormal = 0;

        public int Id;
        public string Name;
        public sbyte TileId, BgId, Type;
        public int ZoneNumber;
        public List<NpcSpawn> Npcs = new List<NpcSpawn>();
        public List<Waypoint> Waypoints = new List<Waypoint>();
        public List<MobSpawn> Monsters = new List<MobSpawn>();
        public List<int[]> LocationStand = new List<int[]>();
        public List<int> Items = new List<int>();
        public List<ItemTree> Behind = new List<ItemTree>(), Between = new List<ItemTree>(), Front = new List<ItemTree>();

        public bool IsNormal { get { return Type == MapNormal; } }

        // Zone changes are refused on dungeons, battlefields and similar special maps; only ordinary maps are enterable for now.
        public bool IsNotChangeZone { get { return !IsNormal; } }

        public TileGrid Grid;   // terrain, loaded on first use

        // TileMap.findWaypoint: the waypoint box widened by 100 pixels
        public Waypoint FindWaypoint(short x, short y)
        {
            foreach (var w in Waypoints)
                if (x >= w.MinX - 100 && x <= w.MaxX + 100 && y >= w.MinY - 100 && y <= w.MaxY + 100) return w;
            return null;
        }

        // TileMap.isInWaypoint
        public bool IsInWaypoint(int cx, int cy)
        {
            foreach (var w in Waypoints)
                if (cx >= w.MinX && cx <= w.MaxX && cy >= w.MinY && cy <= w.MaxY) return true;
            return false;
        }
    }

    public sealed class Mob
    {
        public int Id;
        public MobTemplate Template;
        public bool IsDisable, IsDontMove, IsFire, IsIce, IsWind;
        public sbyte Sys;
        public int Hp, MaxHp;
        public short Level, X, Y;
        public sbyte Status;
        public sbyte LevelBoss;
        public bool IsBoss, IsBeast, IsDead;
        public int DamageOnPlayer, DamageOnPlayer2;
        public long LastTimeAttack;
        public long AttackDelay = 3000;
        public readonly List<int> Chars = new List<int>();   // ids of the players this monster is hostile to
        public int RecoveryTimeCount;   // Mob.recoveryTimeCount: zone ticks (half seconds) until a dead monster comes back
        public bool IsWaitingRespawn;   // in the zone's waiting list (Zone.waitingListRecoveries)
        // Mob.effects: status effects from skills, by effect type (0 disable, 1 burn, 2 freeze, 3 stun, 14 bound) -> end time (ms)
        public readonly Dictionary<int, long> EffectEnd = new Dictionary<int, long>();
    }

    // One row of the `skill` table: a skill at one level (Skill.java).
    public sealed class SkillData
    {
        public int Id, TemplateId, MaxFight, Level, ManaUse, CoolDown, Point, Dx, Dy;
        public int[] OptionIds = new int[0], OptionParams = new int[0];
    }

    public sealed class Zone
    {
        public int Id;
        public TileMap Tile;
        public readonly List<Mob> Mobs = new List<Mob>();
        public readonly List<NpcSpawn> Npcs = new List<NpcSpawn>();
        public int NumberChief, NumberElitez;
        // players currently inside (NsoServer.Net.PlayerState; typed as object so the world model stays independent of networking)
        public readonly List<object> Players = new List<object>();
        public int NumberChar { get { return Players.Count; } }
        // ItemMap list: items lying on the ground, and the counter used for their ids (Zone.numberDropItem)
        public readonly List<ItemMapEntry> ItemMaps = new List<ItemMapEntry>();
        public short NumberDropItem;
    }

    // com.nsoz.map.item.ItemMap: an item lying on the ground
    public sealed class ItemMapEntry
    {
        public short Id, X, Y;
        public int OwnerId = -1;            // only the owner may pick it up during the first 20 s
        public long CreatedAt;              // removed after 30 s
        public bool PickedUp;
        public NsoServer.Store.PlayerItem Item;
    }

    // Static game world built from the original database tables: maps, monsters, NPC placement.
    public sealed partial class GameWorld
    {
        private const int MobHopBiAn = 225, MobJuubiShinjuu = 237;
        private const int MaxMobsPerZone = 127;
        private const int MaxZonesPerMap = 5;

        private readonly Random rng = new Random();
        private readonly Dictionary<int, MobTemplate> mobTemplates = new Dictionary<int, MobTemplate>();
        private readonly Dictionary<int, ItemTemplateData> itemTemplates = new Dictionary<int, ItemTemplateData>();
        private readonly Dictionary<int, TileMap> tileMaps = new Dictionary<int, TileMap>();
        private readonly Dictionary<int, List<Zone>> zones = new Dictionary<int, List<Zone>>();
        private readonly List<Zone> allZones = new List<Zone>();
        private readonly object sync = new object();

        // All game state changes (messages from players and the monster tick) take this lock.
        public readonly object SyncRoot = new object();

        public readonly string DataDirectory;

        public GameWorld(Dictionary<string, SqlTable> tables, string dataDirectory)
        {
            DataDirectory = dataDirectory;
            LoadMobTemplates(tables["monster"]);
            LoadItemTemplates(tables["item"]);
            LoadMaps(tables["map"]);
            LoadSkills(tables["skill"]);
            LoadExps(tables["others"]);
            LoadItemOptionTypes(tables["item_option"]);
            LoadGameData(tables);   // quests, shops, skill templates (GameData.cs)
        }

        // item_option.type: 3..7 are options that only count from a certain upgrade level (AbilityFromEquip)
        private readonly Dictionary<int, int> itemOptionTypes = new Dictionary<int, int>();

        public int ItemOptionType(int optionId)
        {
            int t;
            return itemOptionTypes.TryGetValue(optionId, out t) ? t : 0;
        }

        private void LoadItemOptionTypes(SqlTable t)
        {
            int id = t.Col("id"), type = t.Col("type");
            foreach (var r in t.Rows) itemOptionTypes[int.Parse(r[id])] = int.Parse(r[type]);
        }

        // ---- skills and levels (GameData.getSkill, Server.exps, NinjaUtils.getLevel) ----
        private readonly List<SkillData> skills = new List<SkillData>();
        private long[] exps = new long[0];

        // Server.EXP_MAX: the sum of the level table minus one
        public long ExpMax { get; private set; }

        // GameData.getSkill(classId, templateId, point): the row of that skill template at that point
        public SkillData FindSkill(int templateId, int point)
        {
            foreach (var s in skills) if (s.TemplateId == templateId && s.Point == point) return s;
            return null;
        }

        // NinjaUtils.getLevel: level reached with this much total experience
        public int GetLevel(long exp)
        {
            for (int i = 0; i < exps.Length; i++)
            {
                if (exp < exps[i]) return i;
                exp -= exps[i];
            }
            return 1;
        }

        private void LoadSkills(SqlTable t)
        {
            int id = t.Col("id"), tpl = t.Col("template_id"), maxFight = t.Col("max_fight"), level = t.Col("level"),
                mana = t.Col("mana_use"), cool = t.Col("cooldown"), point = t.Col("point"), dx = t.Col("dx"), dy = t.Col("dy"),
                options = t.Col("options");
            foreach (var r in t.Rows)
            {
                var s = new SkillData
                {
                    Id = int.Parse(r[id]), TemplateId = int.Parse(r[tpl]), MaxFight = int.Parse(r[maxFight]), Level = int.Parse(r[level]),
                    ManaUse = int.Parse(r[mana]), CoolDown = int.Parse(r[cool]), Point = int.Parse(r[point]),
                    Dx = int.Parse(r[dx]), Dy = int.Parse(r[dy])
                };
                // options: [{"param":10,"id":0}, ...]
                var ids = new List<int>(); var pars = new List<int>();
                foreach (var o in Json.Parse(r[options] ?? "[]") as List<object> ?? new List<object>())
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    ids.Add(System.Convert.ToInt32(d["id"])); pars.Add(System.Convert.ToInt32(d["param"]));
                }
                s.OptionIds = ids.ToArray(); s.OptionParams = pars.ToArray();
                skills.Add(s);
            }
        }

        // `others` row "exp": the experience needed for each level
        private void LoadExps(SqlTable t)
        {
            int name = t.Col("name"), value = t.Col("value");
            foreach (var r in t.Rows)
            {
                if (r[name] != "exp") continue;
                var list = new List<long>();
                foreach (var v in Json.Parse(r[value]) as List<object>) list.Add(System.Convert.ToInt64(v));
                exps = list.ToArray();
                long max = 0;
                foreach (long e in exps) max += e;
                ExpMax = max - 1;
            }
        }

        public TileMap FindMap(int id)
        {
            TileMap t;
            return tileMaps.TryGetValue(id, out t) ? t : null;
        }

        public MobTemplate FindMob(int id)
        {
            MobTemplate t;
            return mobTemplates.TryGetValue(id, out t) ? t : null;
        }

        public ItemTemplateData FindItem(int id)
        {
            ItemTemplateData t;
            return itemTemplates.TryGetValue(id, out t) ? t : null;
        }

        public List<Zone> GetZones(int mapId)
        {
            lock (sync)
            {
                List<Zone> list;
                if (!zones.TryGetValue(mapId, out list))
                {
                    var tile = FindMap(mapId);
                    if (tile == null) return null;
                    list = new List<Zone>();
                    int zoneCount = System.Math.Min(System.Math.Max(0, tile.ZoneNumber), MaxZonesPerMap);
                    for (int i = 0; i < zoneCount; i++) list.Add(CreateZone(i, tile));
                    zones[mapId] = list;
                    allZones.AddRange(list);
                }
                return list;
            }
        }

        // every zone created so far (zones are created the first time somebody enters their map)
        public List<Zone> AllZones()
        {
            return allZones;
        }

        public Zone GetZone(int mapId, int zoneId)
        {
            var list = GetZones(mapId);
            return list != null && zoneId >= 0 && zoneId < list.Count ? list[zoneId] : null;
        }

        // NinjaUtils.randomZoneId: the first zone that is not full (24 players), or a random one; -1 if the map has none
        public int RandomZoneId(int mapId)
        {
            var list = GetZones(mapId);
            if (list == null || list.Count == 0) return -1;
            int id = 0;
            foreach (var z in list) { if (z.NumberChar >= 24) id++; else break; }
            if (id >= list.Count) id = rng.Next(list.Count);
            return id;
        }

        public TileGrid GetGrid(TileMap tile)
        {
            lock (sync)
            {
                if (tile.Grid == null) tile.Grid = new TileGrid(LoadFile("Map/" + tile.Id), tile.TileId, tile.LocationStand);
                return tile.Grid;
            }
        }

        public byte[] LoadFile(string relative)
        {
            return File.ReadAllBytes(Path.Combine(DataDirectory, relative));
        }

        private void LoadMobTemplates(SqlTable t)
        {
            int id = t.Col("id"), name = t.Col("name"), level = t.Col("level"), boss = t.Col("boss"), hp = t.Col("hp"),
                type = t.Col("type"), range = t.Col("range_move"), typeFly = t.Col("type_fly"), nImg = t.Col("n_img"),
                move = t.Col("move"), attack = t.Col("attack"), sprites = t.Col("sprites"), frames = t.Col("frames"),
                sequence = t.Col("sequence"), frameChar = t.Col("frame_char"), splash = t.Col("index_splash");
            foreach (var r in t.Rows)
            {
                var m = new MobTemplate
                {
                    Type = sbyte.Parse(r[type]),
                    RangeMove = sbyte.Parse(r[range]),
                    Id = int.Parse(r[id]),
                    Name = r[name],
                    Level = short.Parse(r[level]),
                    Hp = int.Parse(r[hp]),
                    IsBoss = r[boss] != "0",
                    TypeFly = sbyte.Parse(r[typeFly]),
                    NumberImage = sbyte.Parse(r[nImg]),
                    FrameBossMove = ByteArray(r[move]),
                    FrameBossAttack = ByteArrays(r[attack]),
                    ImageInfo = ParseImageInfo(r[sprites]),
                    Frames = ParseFrames(r[frames]),
                    Sequence = ShortArray(r[sequence]),
                    FrameChar = ByteArrays(r[frameChar]),
                    IndexSplash = ByteArray(r[splash])
                };
                mobTemplates[m.Id] = m;
            }
        }

        private void LoadItemTemplates(SqlTable t)
        {
            int id = t.Col("id"), type = t.Col("type"), gender = t.Col("gender"), level = t.Col("level"),
                part = t.Col("part"), fashion = t.Col("fashion"), stack = t.Col("isUpToUp"), name = t.Col("name");
            foreach (var r in t.Rows)
            {
                var item = new ItemTemplateData
                {
                    Id = int.Parse(r[id]), Type = sbyte.Parse(r[type]), Gender = sbyte.Parse(r[gender]),
                    Level = sbyte.Parse(r[level]), Part = short.Parse(r[part]), Fashion = short.Parse(r[fashion]),
                    IsUpToUp = r[stack] == "1", Name = r[name] ?? ""
                };
                itemTemplates[item.Id] = item;
            }
        }

        private static sbyte[] ByteArray(string json)
        {
            var values = Json.Array(json);
            var result = new sbyte[values.Count];
            for (int i = 0; i < result.Length; i++) result[i] = unchecked((sbyte)Json.Long(values[i]));
            return result;
        }

        private static sbyte[][] ByteArrays(string json)
        {
            var values = Json.Array(json);
            var result = new sbyte[values.Count][];
            for (int i = 0; i < result.Length; i++) result[i] = ByteArray(SerializeJsonArray(values[i]));
            return result;
        }

        private static short[] ShortArray(string json)
        {
            var values = Json.Array(json);
            var result = new short[values.Count];
            for (int i = 0; i < result.Length; i++) result[i] = unchecked((short)Json.Long(values[i]));
            return result;
        }

        private static MobImageInfo[] ParseImageInfo(string json)
        {
            var values = Json.Array(json);
            var result = new MobImageInfo[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var d = (Dictionary<string, object>)values[i];
                result[i] = new MobImageInfo { Id = (int)Json.Long(d["id"]), X = (int)Json.Long(d["x"]),
                    Y = (int)Json.Long(d["y"]), W = (int)Json.Long(d["w"]), H = (int)Json.Long(d["h"]) };
            }
            return result;
        }

        private static MobFrame[] ParseFrames(string json)
        {
            var values = Json.Array(json);
            var result = new MobFrame[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var parts = (List<object>)values[i];
                var frame = new MobFrame { Dx = new int[parts.Count], Dy = new int[parts.Count], IdImg = new int[parts.Count],
                    Flip = new int[parts.Count], OnTop = new int[parts.Count] };
                for (int j = 0; j < parts.Count; j++)
                {
                    var d = (Dictionary<string, object>)parts[j];
                    frame.Dx[j] = (int)Json.Long(d["dx"]); frame.Dy[j] = (int)Json.Long(d["dy"]);
                    frame.IdImg[j] = (int)Json.Long(d["id"]); frame.Flip[j] = (int)Json.Long(d["flip"]);
                    frame.OnTop[j] = (int)Json.Long(d["onTop"]);
                }
                result[i] = frame;
            }
            return result;
        }

        private static string SerializeJsonArray(object value)
        {
            var values = (List<object>)value;
            var text = new System.Text.StringBuilder("[");
            for (int i = 0; i < values.Count; i++) { if (i != 0) text.Append(','); text.Append(Json.Long(values[i])); }
            return text.Append(']').ToString();
        }

        private void LoadMaps(SqlTable t)
        {
            int cId = t.Col("id"), cName = t.Col("name"), cNpc = t.Col("npc"), cWay = t.Col("waypoint"), cMon = t.Col("monster"),
                cZone = t.Col("zone_number"), cStand = t.Col("locationStand"), cTile = t.Col("tileId"), cBg = t.Col("bgId"),
                cType = t.Col("type"), cItem = t.Col("item"), cBehind = t.Col("behind"), cBetwen = t.Col("betwen"), cFront = t.Col("front");
            foreach (var r in t.Rows)
            {
                var m = new TileMap
                {
                    Id = int.Parse(r[cId]),
                    Name = r[cName],
                    TileId = sbyte.Parse(r[cTile]),
                    BgId = sbyte.Parse(r[cBg]),
                    Type = sbyte.Parse(r[cType]),
                    ZoneNumber = (sbyte)int.Parse(r[cZone])
                };
                foreach (Dictionary<string, object> o in Json.Array(r[cNpc]))
                {
                    int tpl = (short)Json.Long(o["templateId"]);
                    // events (Tet, summer, mid-autumn) are not run offline, so their NPCs are not placed
                    if (tpl == 41 || tpl == 42) continue;
                    m.Npcs.Add(new NpcSpawn
                    {
                        Id = m.Npcs.Count,
                        TemplateId = tpl,
                        X = (short)Json.Long(o["x"]),
                        Y = (short)Json.Long(o["y"]),
                        Status = (sbyte)Json.Long(o["status"])
                    });
                }
                foreach (Dictionary<string, object> o in Json.Array(r[cMon]))
                {
                    short tpl = (short)Json.Long(o["templateId"]);
                    if (tpl == 225) continue; // event monster
                    m.Monsters.Add(new MobSpawn
                    {
                        TemplateId = tpl,
                        X = (short)Json.Long(o["x"]),
                        Y = (short)Json.Long(o["y"]),
                        IsBeast = (bool)o["boss"]
                    });
                }
                foreach (Dictionary<string, object> o in Json.Array(r[cWay]))
                {
                    m.Waypoints.Add(new Waypoint
                    {
                        Next = (short)Json.Long(o["next"]),
                        MinX = (short)Json.Long(o["minX"]),
                        MinY = (short)Json.Long(o["minY"]),
                        MaxX = (short)Json.Long(o["maxX"]),
                        MaxY = (short)Json.Long(o["maxY"]),
                        X = (short)Json.Long(o["x"]),
                        Y = (short)Json.Long(o["y"])
                    });
                }
                foreach (List<object> a in Json.Array(r[cStand]))
                    m.LocationStand.Add(new[] { (int)Json.Long(a[0]), (int)Json.Long(a[1]) });
                foreach (object o in Json.Array(r[cItem])) m.Items.Add((int)Json.Long(o));
                ReadTrees(r[cBehind], m.Behind);
                ReadTrees(r[cBetwen], m.Between);
                ReadTrees(r[cFront], m.Front);
                tileMaps[m.Id] = m;
            }
        }

        private static void ReadTrees(string json, List<ItemTree> into)
        {
            foreach (Dictionary<string, object> o in Json.Array(json))
                into.Add(new ItemTree { Id = (int)Json.Long(o["id"]), X = (int)Json.Long(o["x"]), Y = (int)Json.Long(o["y"]) });
        }

        // Zone.init / Zone.createMonster / MobFactory.createMonster
        private Zone CreateZone(int id, TileMap tile)
        {
            var z = new Zone { Id = id, Tile = tile };
            int mobId = 0;
            foreach (var pos in tile.Monsters)
            {
                var tpl = FindMob(pos.TemplateId);
                var mob = new Mob
                {
                    Id = mobId++,
                    Template = tpl,
                    Level = tpl.Level,
                    X = pos.X,
                    Y = pos.Y,
                    Status = 5,
                    IsBoss = tpl.IsBoss,
                    IsBeast = pos.IsBeast && id % 5 == 0
                };
                SetupMob(z, mob);
                if (tpl.IsBoss && tile.IsNormal && tpl.Id != MobHopBiAn && tpl.Id != MobJuubiShinjuu)
                {
                    // bosses on ordinary maps start dead and are respawned by the server
                    if (mob.LevelBoss == 2) z.NumberChief--; else if (mob.LevelBoss == 1) z.NumberElitez--;
                    if (z.NumberChief < 0) z.NumberChief = 0;
                    if (z.NumberElitez < 0) z.NumberElitez = 0;
                    mob.Hp = 0; mob.Status = 0; mob.IsDead = true;
                }
                z.Mobs.Add(mob);
                if (mobId >= MaxMobsPerZone) break;
            }
            foreach (var n in tile.Npcs)
                z.Npcs.Add(new NpcSpawn { Id = n.Id, TemplateId = n.TemplateId, X = n.X, Y = n.Y, Status = n.Status });
            return z;
        }

        // Mob.setLevelBoss for ordinary maps. Special map kinds (dungeons, battlefields...) arrive with their own phase.
        // Mob.setClass + setLevelBoss + setHP + setDamage: used when a zone is created and when a dead monster comes back
        private void SetupMob(Zone z, Mob mob)
        {
            var tpl = mob.Template;
            mob.Sys = (sbyte)rng.Next(1, 3);
            SetLevelBoss(z, mob);
            int hp = tpl.Hp;
            mob.Hp = mob.MaxHp = mob.LevelBoss == 1 ? hp * 10 : mob.LevelBoss == 2 ? hp * 100 : mob.LevelBoss == 3 ? hp * 200 : hp;
            if (mob.MaxHp < 0) mob.Hp = mob.MaxHp = int.MaxValue;
            // Mob.setDamage
            int d = (int)(mob.Level + (System.Math.Pow(mob.Level, 2) / 5));
            if (mob.IsBoss) d *= 20; else if (mob.LevelBoss == 1) d *= 2; else if (mob.LevelBoss == 2) d *= 3;
            mob.DamageOnPlayer = d;
            mob.DamageOnPlayer2 = d - d / 10;
        }

        // Mob.recovery(): the monster comes back alive with fresh class, rank, HP and damage
        public void RecoverMob(Zone z, Mob mob)
        {
            lock (sync)
            {
                mob.IsDead = false;
                SetupMob(z, mob);
                mob.Status = 5;
                mob.IsFire = mob.IsIce = mob.IsWind = mob.IsDontMove = mob.IsDisable = false;
            }
        }

        private void SetLevelBoss(Zone z, Mob mob)
        {
            if (mob.IsBeast) { mob.LevelBoss = 3; return; }
            if (z.NumberChief < 1 && rng.Next(100) == 1 && mob.Level >= 10 && !mob.IsBoss) { mob.LevelBoss = 2; z.NumberChief++; }
            else if (z.NumberElitez < 2 && rng.Next(50) == 1 && mob.Level >= 10 && !mob.IsBoss) { mob.LevelBoss = 1; z.NumberElitez++; }
            else mob.LevelBoss = 0;
        }
    }
}
