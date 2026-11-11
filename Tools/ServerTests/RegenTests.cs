using System;
using System.IO;
using NsoServer;
using NsoServer.Net;
using NsoServer.Store;

static partial class Program
{
    // Exercise the actual half-second recovery path, forcing five-second boundaries instead of sleeping.
    // Disconnected fixture sessions discard presentation packets; no world tick or user save is running.
    static int RegenCheck()
    {
        var server = new GameServer(new ServerConfig { DataDirectory = Dir() });
        Func<PlayerState> player = () => {
            var p = new PlayerState { Hp = 50, Mp = 40, MaxHp = 100, MaxMp = 100,
                Record = new PlayerRecord { Id = 1 }, Zone = new NsoServer.World.Zone(),
                Session = new Session(server, new MemoryStream(), 1) };
            p.Zone.Players.Add(p); return p;
        };
        Func<int, int, int, PlayerEffect> effect = (template, type, amount) => new PlayerEffect {
            TemplateId = template, Type = type, Param = amount,
            StartAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), EndAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 60000 };

        Check(PlayerEffects.NaturalRegenPercent == 0, "unsourced natural recovery is disabled by default");
        var bare = player();
        for (int i = 0; i < 12; i++) { bare.LastRegen = 0; PlayerEffects.UpdateHalfSecond(bare, null); }
        Check(bare.Hp == 50 && bare.Mp == 40, "no HP or MP gain across repeated recovery boundaries without sources");

        var food = player(); food.Effects.Add(effect(0, 0, 3)); PlayerEffects.UpdateHalfSecond(food, null);
        Check(food.Hp == 53 && food.Mp == 43, "food still restores its HP and MP amount");
        var potion = player(); potion.Effects.Add(effect(21, 17, 25)); PlayerEffects.UpdateHalfSecond(potion, null);
        Check(potion.Hp == 75 && potion.Mp == 40, "HP potion still restores HP without free MP");

        var equip = player(); equip.Options[99] = 2; equip.Options[30] = 3; equip.Options[120] = 4; equip.Options[27] = 5; equip.Options[119] = 6;
        PlayerEffects.UpdateHalfSecond(equip, null);
        Check(equip.Hp == 59 && equip.Mp == 53, "half-second and five-second equipment recovery remain intact");
        PlayerEffects.UpdateHalfSecond(equip, null);
        Check(equip.Hp == 61 && equip.Mp == 55, "five-second equipment recovery does not repeat before its interval");

        var expired = player(); var old = effect(0, 0, 3); old.EndAt = 1; expired.Effects.Add(old);
        PlayerEffects.UpdateHalfSecond(expired, null);
        Check(expired.Hp == 50 && expired.Mp == 40 && expired.Effects.Count == 0, "expired food cannot restore HP or MP");
        var capped = player(); capped.Hp = capped.Mp = 99; capped.Effects.Add(effect(0, 0, 3)); PlayerEffects.UpdateHalfSecond(capped, null);
        Check(capped.Hp == 100 && capped.Mp == 100, "legitimate recovery remains capped at maximum HP and MP");
        var dead = player(); dead.Hp = 0; dead.IsDead = true; dead.Effects.Add(effect(0, 0, 3)); dead.Options[99] = 2;
        PlayerEffects.UpdateHalfSecond(dead, null);
        Check(dead.Hp == 0 && dead.Mp == 40, "dead characters do not recover from active sources");
        return failures;
    }
}
