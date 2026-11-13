using System.Text;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Syntax-only maintenance: never rewrite expressions, names, string literals or field initializers.
var root = Path.GetFullPath(args.Length > 1 ? args[1] : ".");
var mode = args.Length > 0 ? args[0] : "check";
if (mode == "audit")
{
    var current = new Dictionary<string, string>();
    var original = new Dictionary<string, string>();
    foreach (string directory in new[]
    {
        "Assets/Server",
        "Assets/Scripts",
        "Assets/Editor"
    }

    )
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, directory), "*.cs", SearchOption.AllDirectories))
            Members(File.ReadAllText(file), current);
    foreach (var file in Git("ls-files", "Assets/Server/*.cs", "Assets/Server/**/*.cs", "Assets/Scripts/*.cs", "Assets/Scripts/**/*.cs", "Assets/Editor/*.cs").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        Members(Git("show", "HEAD:" + file.TrimEnd('\r')), original);
    int verified = 0;
    foreach (var entry in original)
    {
        string name = entry.Key;
        bool integration = new[]
        {
            "/ServerBootstrap/",
            "/NsoGameControl/",
            "/NsoServerControl/",
            "/NsoUiLayout/",
            "/AgentRag/",
            "/LlmReplyFormat/"
        }.Any(name.Contains) || name.Contains("/GameKnowledge/") && !new[]
        {
            "/UseInfo(",
            "/Food(",
            "/UsableFromBag("
        }.Any(name.Contains) || name.Contains("/LlmPlanner/") && !new[]
        {
            "/Short(",
            "/IsLocal(",
            "/RecallQuery("
        }.Any(name.Contains) || name.Contains("/AgentManager/DebugObject(") || name.Contains("/AgentManager/Decide(") || name.Contains("/AgentManager/DrainResults(") || name.Contains("/StrategicService/Worker(");
        if (integration)
            continue;
        if (!current.TryGetValue(name, out var body) || body != entry.Value)
            throw new Exception("Unexpected member change: " + name);
        verified++;
    }

    Console.WriteLine($"PASS {verified} original fields/properties/methods retain their exact syntax tokens across formatting and extraction");
    return;
}

// Parse the active Unity branches as code, not disabled trivia (which Roslyn can grow on repeated formatting).
var responseFiles = Directory.Exists(Path.Combine(root, "Library/Bee")) ? Directory.EnumerateFiles(Path.Combine(root, "Library/Bee"), "Assembly-CSharp.rsp", SearchOption.AllDirectories).ToArray() : Array.Empty<string>();
var symbols = responseFiles.Length > 0 ? File.ReadAllLines(responseFiles[0]).Where(line => line.StartsWith("-define:")).Select(line => line.Substring(8)).ToArray() : new[]
{
    "UNITY_EDITOR",
    "UNITY_5_3_OR_NEWER",
    "ENABLE_INPUT_SYSTEM"
};
var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: symbols);
var utf8 = new UTF8Encoding(false, true);
int changed = 0, skipped = 0;
var paths = new[]
{
    "Assets/Server",
    "Assets/Scripts",
    "Assets/Editor",
    "Tools/ServerTests",
    "Tools/Learning/RlHost",
    "Tools/Brain/ContractHost",
    "Tools/CodeMaintenance"
}.SelectMany(p => Directory.EnumerateFiles(Path.Combine(root, p), "*.cs", SearchOption.AllDirectories)).Where(p => !p.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part == "bin" || part == "obj")).ToArray();
foreach (var path in paths)
{
    var bytes = File.ReadAllBytes(path);
    bool bom = bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 });
    string source;
    try
    {
        source = utf8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
    }
    catch (DecoderFallbackException)
    {
        Console.WriteLine("Preserved non-UTF8: " + Path.GetRelativePath(root, path));
        skipped++;
        continue;
    }

    var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
    var syntax = tree.GetCompilationUnitRoot();
    if (syntax.ContainsDiagnostics && tree.GetDiagnostics().Any(x => x.Severity == DiagnosticSeverity.Error))
        throw new InvalidOperationException("Cannot safely format " + path);
    if (mode == "refine")
    {
        var type = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (type == null)
            continue;
        string name = type.Identifier.ValueText;
        if (!((name == "GameScr" && Path.GetFileName(path) == "GameScr.cs") || (name == "Char" && Path.GetFileName(path) == "Char.cs") || (name == "AgentTactics" && Path.GetFileName(path) == "TacticalLayer.cs")))
            continue;
        var groups = type.Members.Where(m => !m.ContainsDirectives && !m.GetLeadingTrivia().Any(t => t.IsDirective)).Select(m => (Member: m, Group: RefineGroup(name, m))).Where(x => x.Group != null).GroupBy(x => x.Group).ToList();
        var moved = groups.SelectMany(g => g.Select(x => x.Member)).ToHashSet();
        foreach (var group in groups)
        {
            string destination = Path.Combine(Path.GetDirectoryName(path)!, name + "." + group.Key + ".cs");
            CompilationUnitSyntax part;
            if (File.Exists(destination))
            {
                part = CSharpSyntaxTree.ParseText(File.ReadAllText(destination)).GetCompilationUnitRoot();
                var declaration = part.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                part = part.ReplaceNode(declaration, declaration.AddMembers(group.Select(x => x.Member).ToArray()));
            }
            else
            {
                var declaration = SyntaxFactory.ClassDeclaration(type.Identifier).WithModifiers(type.Modifiers).WithMembers(SyntaxFactory.List(group.Select(x => x.Member)));
                part = syntax.ReplaceNode(type, declaration);
            }

            Save(destination, part.NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n", false);
        }

        Save(path, syntax.ReplaceNode(type, type.WithMembers(SyntaxFactory.List(type.Members.Where(m => !moved.Contains(m))))).NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n", bom);
        Console.WriteLine($"Grouped {moved.Count} remaining members of {name}; field initialization order preserved");
        continue;
    }

    if (mode == "types")
    {
        if (!new[]
        {
            "StrategicLayer.cs",
            "AgentModel.cs"
        }.Contains(Path.GetFileName(path)))
            continue;
        string retainedType = Path.GetFileName(path) == "AgentModel.cs" ? "NinjaAgent" : "IStrategicPlanner";
        var ns = syntax.Members.OfType<NamespaceDeclarationSyntax>().Single();
        foreach (var declaration in ns.Members.OfType<BaseTypeDeclarationSyntax>())
        {
            if (declaration.Identifier.ValueText == retainedType)
                continue;
            var part = syntax.ReplaceNode(ns, ns.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(declaration)));
            string destination = Path.Combine(Path.GetDirectoryName(path)!, declaration.Identifier.ValueText + ".cs");
            if (File.Exists(destination))
                throw new IOException("Will not overwrite " + destination);
            Save(destination, part.NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n", false);
        }

        var contract = ns.Members.OfType<BaseTypeDeclarationSyntax>().Single(d => d.Identifier.ValueText == retainedType);
        Save(path, syntax.ReplaceNode(ns, ns.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(contract))).NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n", bom);
        Console.WriteLine("Extracted planner types without changing their bodies");
        continue;
    }

    if (mode == "split")
    {
        var type = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (type == null)
            continue;
        string typeName = type.Identifier.ValueText;
        string fileName = Path.GetFileName(path);
        bool target = (typeName == "GameScr" && fileName == "GameScr.cs") || (typeName == "Char" && fileName == "Char.cs") || (typeName == "AgentTactics" && fileName == "TacticalLayer.cs") || (typeName == "AgentManager" && fileName == "AgentManager.cs") || (fileName == "Controller.cs" && path.Replace('\\', '/').Contains("Networking/Protocol")) || (typeName == "Service" && fileName == "Service.cs");
        if (!target || type.Modifiers.Any(SyntaxKind.PartialKeyword))
            continue;
        var methods = type.Members.OfType<MethodDeclarationSyntax>().Where(x => !x.ContainsDirectives && !x.GetLeadingTrivia().Any(t => t.IsDirective)).ToList();
        var groups = methods.GroupBy(m => Group(typeName, m.Identifier.ValueText)).Where(g => g.Key != "Core").ToList();
        var moved = groups.SelectMany(g => g).ToHashSet();
        if (moved.Count == 0)
            continue;
        var originalTokens = methods.ToDictionary(m => m.SpanStart, m => Tokens(m));
        var partial = type.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        var remaining = partial.WithMembers(SyntaxFactory.List(type.Members.Where(m => !moved.Contains(m))));
        foreach (var group in groups)
        {
            // The original declaration owns base types, all fields, their initialization order and Unity attributes.
            var declaration = SyntaxFactory.ClassDeclaration(type.Identifier).WithModifiers(partial.Modifiers).WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(group));
            var part = syntax.ReplaceNode(type, declaration);
            foreach (var method in group)
                if (!originalTokens[method.SpanStart].SequenceEqual(Tokens(method)))
                    throw new Exception("Member changed during extraction");
            string destination = Path.Combine(Path.GetDirectoryName(path)!, typeName + "." + group.Key + ".cs");
            if (File.Exists(destination))
                throw new IOException("Will not overwrite " + destination);
            Save(destination, part.NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n", false);
        }

        source = syntax.ReplaceNode(type, remaining).NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n";
        Save(path, source, bom);
        Console.WriteLine($"Split {typeName}: {moved.Count} unchanged methods into {groups.Count} parts");
        continue;
    }

    var normalized = syntax.NormalizeWhitespace("    ", source.Contains("\r\n") ? "\r\n" : "\n");
    string fullText = normalized.ToFullString();
    var trailing = normalized.DescendantTrivia(descendIntoTrivia: true).Where(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia) && trivia.Span.End < fullText.Length && (fullText[trivia.Span.End] == '\r' || fullText[trivia.Span.End] == '\n')).ToArray();
    normalized = normalized.ReplaceTrivia(trailing, (original, rewritten) => default(SyntaxTrivia));
    string formatted = normalized.ToFullString().TrimEnd('\r', '\n') + (source.Contains("\r\n") ? "\r\n" : "\n");
    if (!Tokens(syntax).SequenceEqual(Tokens(CSharpSyntaxTree.ParseText(formatted, parseOptions).GetRoot())))
        throw new InvalidOperationException("Token change: " + path);
    if (formatted == source)
        continue;
    changed++;
    Console.WriteLine("Whitespace: " + Path.GetRelativePath(root, path));
    if (mode == "format")
        Save(path, formatted, bom);
}

Console.WriteLine($"{mode}: {changed} whitespace-only files; {skipped} encoding-preserved files");
if (mode == "check" && changed > 0)
    Environment.ExitCode = 1;
static string[] Tokens(SyntaxNode node) => node.DescendantTokens(descendIntoTrivia: true).Select(t => t.RawKind + ":" + t.Text).ToArray();
static string Git(params string[] arguments)
{
    var info = new ProcessStartInfo("git")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    foreach (var argument in arguments)
        info.ArgumentList.Add(argument);
    using var process = Process.Start(info)!;
    string result = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new Exception(process.StandardError.ReadToEnd());
    return result;
}

static void Members(string source, Dictionary<string, string> members)
{
    var syntax = CSharpSyntaxTree.ParseText(source).GetRoot();
    foreach (var member in syntax.DescendantNodes().OfType<MemberDeclarationSyntax>())
    {
        string name = member switch
        {
            MethodDeclarationSyntax m => m.Identifier.ValueText + "(" + string.Join("", Tokens(m.ParameterList)) + ")",
            ConstructorDeclarationSyntax c => (c.Modifiers.Any(SyntaxKind.StaticKeyword) ? ".cctor(" : ".ctor(") + string.Join("", Tokens(c.ParameterList)) + ")",
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            FieldDeclarationSyntax f => string.Join(",", f.Declaration.Variables.Select(v => v.Identifier.ValueText)),
            _ => null
        };
        if (name == null)
            continue;
        string owner = string.Join("/", member.Ancestors().Reverse().Select(a => a switch
        {
            NamespaceDeclarationSyntax n => n.Name.ToString(),
            BaseTypeDeclarationSyntax t => t.Identifier.ValueText,
            _ => null
        }).Where(x => x != null));
        string key = "/" + owner + "/" + name;
        if (!members.TryAdd(key, string.Join("\n", member.DescendantTokens().Select(t => t.RawKind + ":" + t.Text.Replace("\r\n", "\n")))))
            throw new Exception("Duplicate member " + key);
    }
}

static void Save(string path, string source, bool bom) => File.WriteAllText(path, source, new UTF8Encoding(bom));
static string RefineGroup(string type, MemberDeclarationSyntax member)
{
    if (type == "AgentTactics")
        return member is ClassDeclarationSyntax c && c.Identifier.ValueText == "QuestPlay" ? "Quests" : null;
    if (!(member is MethodDeclarationSyntax method))
        return null;
    string n = method.Identifier.ValueText.ToLowerInvariant();
    if (type == "GameScr")
    {
        if (n.Contains("ngoc") || n.Contains("giaodo"))
            return "Inventory";
        if (n.Contains("thanthu") || n.Contains("bijuu"))
            return "Companions";
        if (n.StartsWith("act") || n.StartsWith("setcmd"))
            return "Commands";
        if (n.Contains("chat") || n.Contains("terminal"))
            return "Chat";
        if (n.StartsWith("load") || n.StartsWith("read") || n.StartsWith("clear") || n.StartsWith("reset") || n.StartsWith("init"))
            return "Lifecycle";
    }

    if (type == "Char")
    {
        if (n.Contains("focus") || n == "ischarinscreen" || n == "getindexchar")
            return "Targeting";
        if (n.StartsWith("getframe") || n.Contains("horse") || n.Contains("mount") || n.Contains("thoitrang") || n == "getheadid" || n == "getlegid")
            return "Appearance";
        if (n.Contains("die") || n.Contains("dead") || n == "changestatusstand")
            return "Lifecycle";
    }

    return null;
}

static string Group(string type, string name)
{
    string n = name.ToLowerInvariant();
    if (type == "AgentManager")
    {
        if (new[]
        {
            "Load",
            "Save",
            "Restore"
        }.Contains(name))
            return "Persistence";
        if (new[]
        {
            "Snapshot",
            "CurrentPriorities",
            "IsVillage"
        }.Contains(name))
            return "Observation";
        if (new[]
        {
            "MaybeDecide",
            "Decide",
            "DrainResults",
            "Apply",
            "BeginStep",
            "StepDone",
            "StepFailed"
        }.Contains(name))
            return "Planning";
        if (new[]
        {
            "Report",
            "Remember",
            "OnDied",
            "OnKill",
            "OnPicked",
            "OnQuestProgress",
            "Say"
        }.Contains(name))
            return "Events";
        if (new[]
        {
            "ForceReplan",
            "Summary",
            "LlmStatus",
            "DebugObject"
        }.Contains(name))
            return "Diagnostics";
        return "Core";
    }

    if (type == "AgentTactics")
    {
        if (new[]
        {
            "Update",
            "HpPercent",
            "MpPercent"
        }.Contains(name))
            return "Core";
        if (new[]
        {
            "MoveTo",
            "Travel",
            "MapName",
            "PlanFastTravel",
            "RunFastTravel",
            "WalkTo",
            "OverObstacle",
            "Wander",
            "GoToNpc",
            "NpcName",
            "Explore",
            "Idle"
        }.Contains(name))
            return "Travel";
        if (new[]
        {
            "Hunt",
            "FindMob",
            "Suitable",
            "ChooseMob",
            "ApproachPoint",
            "InRange"
        }.Contains(name))
            return "Combat";
        if (new[]
        {
            "TryLootHabit",
            "TrySpreadZone",
            "PickUp",
            "Collect"
        }.Contains(name))
            return "Foraging";
        if (new[]
        {
            "GetSword",
            "QuestGuide",
            "NpcAt",
            "MobAt"
        }.Contains(name))
            return "Quests";
        return "Supplies";
    }

    if (n.Contains("paint") || n.Contains("draw") || n.Contains("render"))
        return "Rendering";
    if (n.Contains("key") || n.Contains("pointer") || n.Contains("mouse") || n.Contains("navigate"))
        return "Input";
    if (new[]
    {
        "bag",
        "box",
        "item",
        "equip",
        "wear",
        "upgrade",
        "crystal",
        "gem",
        "craft",
        "split"
    }.Any(n.Contains))
        return "Inventory";
    if (new[]
    {
        "skill",
        "attack",
        "fight",
        "hit",
        "eff",
        "damage"
    }.Any(n.Contains))
        return "Combat";
    if (new[]
    {
        "task",
        "quest",
        "npc",
        "shop",
        "store",
        "menu"
    }.Any(n.Contains))
        return "NpcQuest";
    if (new[]
    {
        "move",
        "jump",
        "fall",
        "waypoint",
        "map",
        "zone"
    }.Any(n.Contains))
        return "Movement";
    if (n.StartsWith("update"))
        return "Update";
    if (n.StartsWith("perform") || n.StartsWith("do"))
        return "Commands";
    if (type == "Service")
        return "Messages";
    return "Core";
}
