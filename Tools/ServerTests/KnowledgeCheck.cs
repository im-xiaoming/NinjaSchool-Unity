using System;
using System.IO;
using System.Linq;

// `knowledgecheck`: builds the shared game knowledge (GameKnowledge, from the dev wiki records) and checks that the
// documents say how items are obtained, what using them does and whether they are usable offline.
static class KnowledgeCheck
{
    public static int Run()
    {
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var tables = NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt"));
        var world = new NsoServer.World.GameWorld(tables, Path.Combine(staticRoot, "Data"));
        string notes = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoWiki\notes.json");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var docs = NsoServer.Ai.GameKnowledge.Build(NsoServer.DevWiki.WikiData.Build(tables, world, notes), world);
        Console.WriteLine(docs.Count + " documents in " + sw.ElapsedMilliseconds + " ms: " + string.Join(", ", docs.GroupBy(d => d[1]).Select(g => g.Key + " " + g.Count())));
        int bad = 0;
        Func<string, string> doc = key =>
        {
            var d = docs.FirstOrDefault(x => x[0] == key);
            return d != null ? d[2] : "";
        };
        var checks = new[]
        {
            new[]
            {
                "item:23",
                "Tabemono",
                "every half second for 30 minutes",
                "usable offline"
            }, // Cơm nắm
            new[]
            {
                "item:13",
                "Kiriko",
                "HP potion",
                "100 yen"
            }, // Bình HP cực tiểu
            new[]
            {
                "item:211",
                "dropped by Thảo dược",
                "quest item"
            }, // Bông thảo dược
            new[]
            {
                "item:1",
                "material"
            }, // Đá cấp 2
            new[]
            {
                "npc:3",
                "Dược phẩm",
                "Làng Tone"
            },
            new[]
            {
                "quest:5",
                "Kiriko",
                "Bông thảo dược"
            },
            new[]
            {
                "shop:9",
                "Tabemono",
                "Cơm nắm 200 yen"
            },
            new[]
            {
                "mob:5",
                "level 10",
                "Đồng Hachi"
            },
            new[]
            {
                "map:5",
                "Dơi",
                "Exits to"
            },
        };
        foreach (var c in checks)
        {
            string text = doc(c[0]);
            Console.WriteLine(c[0] + ": " + text);
            for (int i = 1; i < c.Length; i++)
                if (!text.Contains(c[i]))
                {
                    bad++;
                    Console.WriteLine("FAIL: " + c[0] + " lacks \"" + c[i] + "\"");
                }
        }

        int maxLen = docs.Max(d => d[2].Length);
        Console.WriteLine("longest document " + maxLen + " chars");
        if (docs.Count < 1500)
        {
            bad++;
            Console.WriteLine("FAIL: too few documents");
        }

        Console.WriteLine(bad == 0 ? "knowledgecheck OK" : bad + " FAILURES");
        return bad;
    }
}
