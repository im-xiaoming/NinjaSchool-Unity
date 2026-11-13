using System;
using System.Collections.Generic;
using NsoServer.Net;

namespace NsoServer.Ai
{
    using Math = System.Math; // the client has its own global Math class

    /// <summary>Who the AI player is: character name, look, the school it wants to join and traits (0..1) that both
    /// planners read.</summary>
    public sealed class AgentPersonality
    {
        /// <summary>Character name: lower-case letters and digits, 5-15 characters (Controller.CreateCharacter rule).</summary>
        public string Name;
        public string Bio;
        /// <summary>The class it chooses at the school (1 kiếm, 2 phi tiêu: Hirosaki; 3 kunai, 4 cung: Ookaza; 5 đao, 6 quạt: Haruna).</summary>
        public int ClassId;
        public int Gender, Head;
        public float Aggression, Caution, Greed, Curiosity, Sociability;
        public string[] Greetings;
        public string Traits()
        {
            return "aggression " + Aggression.ToString("0.0") + ", caution " + Caution.ToString("0.0") + ", greed " + Greed.ToString("0.0") + ", curiosity " + Curiosity.ToString("0.0") + ", sociability " + Sociability.ToString("0.0");
        }

        private static AgentPersonality P(string name, int cls, int gender, int head, float ag, float ca, float gr, float cu, float so, string bio, params string[] hi)
        {
            return new AgentPersonality
            {
                Name = name,
                ClassId = cls,
                Gender = gender,
                Head = head,
                Aggression = ag,
                Caution = ca,
                Greed = gr,
                Curiosity = cu,
                Sociability = so,
                Bio = bio,
                Greetings = hi
            };
        }

        // heads: CreateCharScr.hairID (male 11/26/27/28, female 2/23/24/25)
        public static readonly AgentPersonality[] Presets =
        {
            P("raiden", 1, 0, 26, 0.9f, 0.2f, 0.4f, 0.3f, 0.4f, "Hot-headed swordsman who wants to be the strongest ninja of the village. Loves hard fights, hates waiting.", "Đấu với ta không?", "Ta sẽ mạnh nhất làng này!", "Đi săn quái cùng không?"),
            P("hanabi", 4, 1, 23, 0.4f, 0.7f, 0.8f, 0.4f, 0.6f, "Careful trader's daughter. Counts every yen, avoids risky fights and always keeps potions.", "Chào bạn! Có bán gì không?", "Hôm nay kiếm được nhiều yên ghê.", "Cẩn thận quái mạnh đấy."),
            P("sorano", 5, 0, 27, 0.5f, 0.4f, 0.3f, 0.9f, 0.5f, "Wanderer who wants to see every map of the world. Gets bored staying in one place.", "Bạn đã tới Rừng Moshio chưa?", "Thế giới rộng lớn quá!", "Mình vừa khám phá chỗ mới."),
            P("yukine", 3, 1, 2, 0.3f, 0.6f, 0.4f, 0.5f, 0.9f, "Friendly girl who loves meeting other ninjas and chatting in the village between hunts.", "Xin chào! Bạn mới tới à?", "Hôm nay trời đẹp ghê.", "Cùng làm nhiệm vụ nhé!"),
            P("kenta", 2, 0, 11, 0.6f, 0.5f, 0.6f, 0.3f, 0.2f, "Quiet, methodical grinder. Hunts monsters of his level in long efficient sessions.", "...", "Đừng làm phiền.", "Ừ, chào."),
            P("mikasa", 6, 1, 24, 0.7f, 0.3f, 0.5f, 0.7f, 0.7f, "Cheerful adventurer who takes on any challenge and tells everyone about it.", "Ê! Đi phiêu lưu không?", "Mình vừa hạ được con quái to lắm!", "Chào nha~"),
            P("haruto", 1, 0, 28, 0.6f, 0.5f, 0.3f, 0.4f, 0.5f, "Dutiful student who follows every quest of the village elders to the letter.", "Trưởng làng giao nhiệm vụ mới rồi!", "Học hành chăm chỉ nhé.", "Chào bạn đồng môn."),
            P("akane", 4, 1, 25, 0.5f, 0.5f, 0.6f, 0.6f, 0.4f, "Archer with sharp eyes for loot. Picks up everything and sells nothing.", "Có thấy đồ rơi không?", "Túi mình sắp đầy rồi.", "Bắn trúng rồi!"),
            P("renji", 2, 0, 11, 0.8f, 0.3f, 0.4f, 0.5f, 0.3f, "Shuriken thrower who loves speed. Rushes to the next quest as soon as he can.", "Nhanh lên nào!", "Chậm quá đó.", "Tới lượt ta!"),
            P("takumi", 3, 0, 26, 0.4f, 0.8f, 0.5f, 0.3f, 0.4f, "Cautious kunai user. Never fights without a full stack of potions.", "An toàn là trên hết.", "Bạn có đủ bình máu chưa?", "Đừng liều quá."),
            P("sakura", 6, 1, 2, 0.5f, 0.4f, 0.4f, 0.6f, 0.8f, "Fan ninja who cheers on every newcomer and likes busy villages.", "Chào mừng tới làng!", "Cố lên nha!", "Hôm nay vui ghê."),
            P("daichi", 5, 0, 27, 0.9f, 0.4f, 0.3f, 0.2f, 0.3f, "Blade ninja built like a rock. Picks the strongest monster he can survive.", "Quái càng mạnh càng tốt.", "Hừ.", "Thử sức không?"),
            P("natsuki", 3, 1, 24, 0.4f, 0.6f, 0.7f, 0.5f, 0.6f, "Practical girl saving yen for better equipment. Plans every trip.", "Tiết kiệm là quan trọng.", "Mua đồ ở đâu rẻ nhỉ?", "Chào bạn nhé."),
        };
    }
}
