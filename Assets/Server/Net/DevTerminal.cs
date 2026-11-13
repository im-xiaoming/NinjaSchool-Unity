using System;
using NsoServer.World;

namespace NsoServer.Net
{
    // Offline deviation: CHAT_MAP (-23) is not public chat here. The client's chat box is a dev terminal and
    // every line is a command run against the local player. Replies use SERVER_MESSAGE (-24) prefixed with
    // ReplyMarker, so the client puts them in the terminal window instead of the yellow InfoMe lines.
    // Commands: /help, /goto, /next, /skip, /lv, /tolv n, /agents, /replan [name] (/clear is client-side). Add cases to Run.
    using Math = System.Math;

    public static class DevTerminal
    {
        /// <summary>Master switch. When false every command is refused.</summary>
        public static bool Enabled = true;

        /// <summary>First character of a terminal reply (client: NsoTerminal.ReplyMarker). Must match the client.</summary>
        public const char ReplyMarker = (char)7;

        public static void Run(Message m, PlayerActions actions, GameWorld world)
        {
            string line = m.Reader.ReadUTF();
            var me = actions.State;
            if (!Enabled) { Say(me, "Terminal đang tắt."); return; }
            string[] a = (line ?? "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (a.Length == 0) return;
            // Commands are written /name (the client already refuses lines without the prefix; /clear stays on the client).
            if (!a[0].StartsWith("/")) { Say(me, "Lệnh phải bắt đầu bằng / (gõ /help)."); return; }
            string cmd = a[0].Substring(1).ToLowerInvariant();
            string cmdFull = cmd;
            if (cmd.StartsWith("tolv") && cmd.Length > 4) { cmdFull = cmd; cmd = "tolv"; }
            switch (cmd)
            {
                case "help": Say(me, "/help - danh sách lệnh"); Say(me, "/clear - xoá màn hình terminal"); Say(me, "/goto - tới đầu map làm nhiệm vụ chính hiện tại"); Say(me, "/next - sang bước tiếp theo của nhiệm vụ đang nhận"); Say(me, "/skip - hoàn thành nhiệm vụ hiện tại"); Say(me, "/lv - lên 1 cấp"); Say(me, "/tolv n - lên tới cấp n (0.." + MaxTargetLevel + ")"); Say(me, "/agents - trạng thái các ninja AI"); Say(me, "/replan [tên] - bắt ninja AI quyết định lại"); Say(me, "Gõ không có / là chat công cộng."); break;
                case "agents": foreach (var l in NsoServer.Ai.AgentManager.Summary()) Say(me, l); break;   // offline AI debug
                case "replan":
                    int changed = NsoServer.Ai.AgentManager.ForceReplan(a.Length > 1 ? a[1] : null);
                    Say(me, changed > 0 ? changed + " ninja AI sẽ quyết định lại." : "Không có ninja AI nào (sai tên, đang chết hoặc đang chờ LLM).");
                    break;
                case "next": Say(me, PlayerTasks.NextTaskStep(me, world)); break;
                case "goto": Say(me, QuestTravel.GoToObjective(me, world)); break;
                case "skip": Say(me, PlayerTasks.SkipTask(me, world)); break;
                case "lv": Say(me, LevelTo(me, world, me.Level + 1)); break;
                case "tolv":
                    // "/tolv 50", "/tolv50" and "/tolv[50]" are all accepted
                    string arg = (a.Length > 1 ? a[1] : "").Trim('[', ']');
                    if (cmdFull != "tolv") arg = cmdFull.Substring(4).Trim('[', ']');
                    int n;
                    if (!int.TryParse(arg, out n) || n < 0 || n > MaxTargetLevel) Say(me, "Cú pháp: /tolv n (n từ 0 đến " + MaxTargetLevel + ").");
                    else Say(me, LevelTo(me, world, n));
                    break;
                default: Say(me, "Không rõ lệnh '/" + cmd + "'. Gõ /help."); break;
            }
        }

        /// <summary>Highest level /tolv accepts (also capped by the server level table).</summary>
        public static int MaxTargetLevel = 150;

        // Offline deviation (dev terminal /lv, /tolv): gives exactly the experience needed to reach the level, through
        // PlayerCombat.AddExp so the normal level-up path runs (potential/skill points, client sync, quest hooks).
        private static string LevelTo(PlayerState me, GameWorld world, int target)
        {
            target = Math.Min(target, Math.Min(MaxTargetLevel, world.MaxLevel));
            if (target <= me.Level) return "Đang ở cấp " + me.Level + ", không thể lên cấp " + target + ".";
            long need = world.ExpForLevel(target) - me.Record.Exp;
            if (need <= 0) return "Đang ở cấp " + me.Level + ".";
            PlayerCombat.AddExp(me, world, need + me.ExpDown, false);   // AddExp pays off lost experience first
            return "Đã lên cấp " + me.Level + ".";
        }

        private static void Say(PlayerState me, string text) { PlayerService.ServerMessage(me, ReplyMarker + text); }
    }
}
