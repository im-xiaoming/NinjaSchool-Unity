using System;
using NsoServer.Store;
using System.IO;

namespace NsoServer.Net
{
    // Static payloads the client downloads after login (images, parts, skills, items, maps...).
    // Phase 3 fills these from the original Data/ files and the SQL dump; until then they are empty.
    public interface IStaticData
    {
        byte[] Version { get; }
        byte[] Data { get; }
        byte[] Map { get; }
        byte[] Skill { get; }
        byte[] Item { get; }
    }

    public sealed class EmptyStaticData : IStaticData
    {
        private static readonly byte[] None = new byte[0];
        public byte[] Version { get { return None; } }
        public byte[] Data { get { return None; } }
        public byte[] Map { get { return None; } }
        public byte[] Skill { get { return None; } }
        public byte[] Item { get { return None; } }
    }

    // Port of com.nsoz.network.Controller for the pre-game flow:
    // client info -> login -> data download -> client ok -> character list -> create / select character.
    public sealed class Controller
    {
        private readonly Session session;
        private const string LocalAccount = "player";
        private const string LocalPassword = "local";
        private bool clientInfoSet;
        private int zoomLevel = 1;
        private bool clientOk;
        private UserRecord user;

        public Controller(Session session) { this.session = session; }

        // One lock for all game state: player messages and the monster tick never run at the same time.
        private object Gate { get { return session.Server.World != null ? session.Server.World.SyncRoot : this; } }

        public void OnMessage(Message m)
        {
            lock (Gate) Handle(m);
        }

        private void Handle(Message m)
        {
            try
            {
                switch (m.Command)
                {
                    case Cmd.NOT_LOGIN: OnNotLogin(m); break;
                    case Cmd.NOT_MAP: OnNotMap(m); break;
                    case Cmd.ME_BACK: if (actions != null) actions.ReturnTownFromDead(); break;
                    case Cmd.ME_LIVE: if (actions != null) actions.ReviveInPlace(); break;
                    case Cmd.PLAYER_MOVE: if (actions != null) actions.Move(m); break;
                    case Cmd.MAP_CHANGE: if (actions != null) actions.RequestChangeMap(); break;
                    case Cmd.OPEN_UI_ZONE: if (actions != null) actions.OpenUiZone(); break;
                    case Cmd.ZONE_CHANGE: if (actions != null) actions.ChangeZone(m); break;
                    case 41: if (actions != null) PlayerCombat.SelectSkill(m, actions.State, session.Server.World); break; // SKILL_SELECT
                    case 60: if (actions != null) PlayerCombat.AttackMonster(m, actions.State, session.Server.World); break; // PLAYER_ATTACK_NPC
                    // NPC menus and quests (PlayerTasks)
                    case 40: if (actions != null) PlayerTasks.OpenMenu(m, actions.State, session.Server.World); break;   // OPEN_UI_MENU
                    case 29: if (actions != null) PlayerTasks.Menu(m, actions.State, session.Server.World); break;       // MENU
                    case 47: if (actions != null) PlayerTasks.GetTask(m, actions.State, session.Server.World); break;    // TASK_GET
                    // items and shops (PlayerItems)
                    case 11: if (actions != null) PlayerItems.UseItem(m, actions.State, session.Server.World); break;        // ITEM_USE
                    case 13: if (actions != null) PlayerItems.BuyItem(m, actions.State, session.Server.World); break;        // ITEM_BUY
                    case 15: if (actions != null) PlayerItems.ItemBodyToBag(m, actions.State, session.Server.World); break;  // ITEM_BODY_TO_BAG
                    case 42: if (actions != null) PlayerItems.RequestItemInfo(m, actions.State, session.Server.World); break; // REQUEST_ITEM_INFO
                    case -14: if (actions != null) PlayerItems.PickItem(m, actions.State, session.Server.World); break;      // ITEMMAP_MYPICK
                    case -23: if (actions != null) DevTerminal.Run(m, actions, session.Server.World); break; // CHAT_MAP: dev terminal (offline)
                    case Cmd.SUB_COMMAND: if (actions != null) OnSubCommand(m); break;
                    default: session.Server.Log("Client " + session.Id + ": cmd " + m.Command); break;
                }
            }
            catch (Exception ex)
            {
                session.Server.Log("Client " + session.Id + " message error: " + ex.Message);
            }
        }

        public void OnDisconnected()
        {
            lock (Gate)
            {
                if (actions != null) { actions.SaveAndLeave(); actions = null; }
            }
        }

        private void OnNotLogin(Message m)
        {
            if (user != null) return;
            sbyte sub = m.Reader.ReadSByte();
            switch (sub)
            {
                case Cmd.CLIENT_INFO:
                    try { m.Reader.ReadSByte(); int z = m.Reader.ReadSByte(); session.ZoomLevel = z < 1 || z > 4 ? 1 : z; } catch (Exception) { }
                    clientInfoSet = true; // the fields (screen size, platform...) are not needed offline
                    SignInLocalPlayer();
                    break;
                case Cmd.LOGIN:
                    Login(m);
                    break;
                default:
                    session.Server.Log("Client " + session.Id + ": notLogin " + sub);
                    break;
            }
        }

        // Offline build: there is one local account, signed in as soon as the client sends its info (no login message).
        private void SignInLocalPlayer()
        {
            if (user != null) return;
            UserRecord found;
            var result = session.Server.Accounts.Login(LocalAccount, LocalPassword, out found);
            if (found == null) { session.Close(); return; }
            user = found;
            if (result == AccountStore.LoginResult.Created)
                session.Server.Log("Created account " + LocalAccount);
            SendNotMap(Cmd.UPDATE_VERSION, session.Server.Static.Version);
        }

        private void Login(Message m)
        {
            string username = m.Reader.ReadUTF().Trim();
            string password = m.Reader.ReadUTF().Trim();
            if (!clientInfoSet) { session.Close(); return; }

            UserRecord found;
            var result = session.Server.Accounts.Login(username, password, out found);
            switch (result)
            {
                case AccountStore.LoginResult.BadName:
                    Dialog("Tên tài khoản có kí tự lạ.");
                    return;
                case AccountStore.LoginResult.WrongPassword:
                    Dialog("Tài khoản hoặc mật khẩu không chính xác.");
                    return;
            }
            user = found;
            if (result == AccountStore.LoginResult.Created)
                session.Server.Log("Created account " + username);
            SendNotMap(Cmd.UPDATE_VERSION, session.Server.Static.Version);
        }

        private void OnNotMap(Message m)
        {
            if (user == null) return;
            sbyte sub = m.Reader.ReadSByte();
            switch (sub)
            {
                case Cmd.UPDATE_DATA: SendNotMap(sub, session.Server.Static.Data); break;
                case Cmd.UPDATE_MAP: SendNotMap(sub, session.Server.Static.Map); break;
                case Cmd.UPDATE_SKILL: SendNotMap(sub, session.Server.Static.Skill); break;
                case Cmd.UPDATE_ITEM: SendNotMap(sub, session.Server.Static.Item); break;
                case -115: SendIcon(m); break; // REQUEST_ICON
                case -108: SendMobTemplate(m); break; // REQUEST_NPCTEMPLATE
                case Cmd.CLIENT_OK:
                    if (!clientOk) { clientOk = true; SendCharList(); }
                    break;
                case Cmd.CREATE_PLAYER: CreateCharacter(m); break;
                case Cmd.SELECT_PLAYER:
                    SelectCharacter(m);
                    break;
                default:
                    session.Server.Log("Client " + session.Id + ": notMap " + sub);
                    break;
            }
        }

        private PlayerRecord current;
        private PlayerActions actions;

        private void SelectCharacter(Message m)
        {
            if (current != null) return;
            string name = m.Reader.ReadUTF();
            var chosen = session.Server.Accounts.GetPlayers(user.Id).Find(p => p.Name == name);
            if (chosen == null) { session.Close(); return; }
            if (session.Server.World == null) { Dialog("Dữ liệu thế giới chưa được nạp."); return; }
            current = chosen;
            var state = EnterWorld.Send(session, user, chosen, session.Server.World);
            actions = new PlayerActions(session, session.Server.World, state);
            PlayerTasks.OnEnterWorld(state, session.Server.World);   // quest in progress (User.selectChar)
        }

        private void CreateCharacter(Message m)
        {
            var store = session.Server.Accounts;
            if (store.GetPlayers(user.Id).Count >= 3) { Dialog("Bạn chỉ được tạo tối đa 3 nhân vật."); return; }
            string name = m.Reader.ReadUTF();
            sbyte gender = m.Reader.ReadSByte();
            sbyte head = m.Reader.ReadSByte();
            foreach (char c in name)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                {
                    Dialog("Tên nhân vật không được chứa ký tự đặc biệt!");
                    return;
                }
            sbyte[] heads = gender == 0 ? new sbyte[] { 11, 26, 27, 28 } : new sbyte[] { 2, 23, 24, 25 };
            gender = (sbyte)(gender == 0 ? 0 : 1);
            sbyte chosen = heads[0];
            foreach (sbyte h in heads) if (h == head) { chosen = h; break; }
            if (name.Length < 5 || name.Length > 15) { Dialog("Tên tài khoản chỉ cho phép từ 5 đến 15 ký tự!"); return; }
            if (store.NameTaken(name)) { Dialog("Tên nhân vật đã tồn tại!"); return; }
            store.AddPlayer(user.Id, name, gender, chosen);
            SendCharList();
        }

        private void SendCharList()
        {
            var list = session.Server.Accounts.GetPlayers(user.Id);
            var ms = NotMap(Cmd.SELECT_PLAYER);
            var w = ms.Writer;
            w.WriteByte(list.Count);
            foreach (var p in list)
            {
                w.WriteByte(p.Gender);
                w.WriteUTF(p.Name);
                w.WriteUTF(ClassName(p.ClassId));
                w.WriteByte(Level(p.Exp));
                w.WriteShort(p.Head);
                w.WriteShort(p.Weapon);
                w.WriteShort(p.Body);
                w.WriteShort(p.Leg);
            }
            session.Send(ms);
        }

        private static readonly string[] ClassNames =
        {
            "Chưa vào trường", "Ninja kiếm", "Ninja phi tiêu", "Ninja kunai", "Ninja cung", "Ninja đao", "Ninja quạt"
        };

        private static string ClassName(int id)
        {
            return id >= 0 && id < ClassNames.Length ? ClassNames[id] : ClassNames[0];
        }

        // Level derived from exp: replaced by the real exp table in phase 3. Level 1 until then.
        private static int Level(long exp) { return 1; }

        private Message NotMap(sbyte sub)
        {
            var ms = new Message(Cmd.NOT_MAP);
            ms.Writer.WriteSByte(sub);
            return ms;
        }

        private void SendNotMap(sbyte sub, byte[] payload)
        {
            var ms = NotMap(sub);
            ms.Writer.Write(payload);
            session.Send(ms);
        }

        private void SendIcon(Message m)
        {
            int icon = m.Reader.ReadInt();
            byte[] png;
            try { png = session.Server.World.LoadFile("Img/Small/" + session.ZoomLevel + "/Small" + icon + ".png"); }
            catch (System.IO.IOException) { return; }
            var ms = NotMap(-115);
            ms.Writer.WriteInt(icon);
            ms.Writer.WriteInt(png.Length);
            ms.Writer.Write(png);
            session.Send(ms);
        }

        private void SendMobTemplate(Message request)
        {
            var world = session.Server.World;
            if (world == null) return;
            int id = request.Reader.ReadShort();
            var mob = world.FindMob(id);
            if (mob == null) return;

            var ms = NotMap(-108);
            var w = ms.Writer;
            w.WriteShort(id);
            w.WriteSByte(mob.TypeFly);
            w.WriteSByte(mob.NumberImage);
            try
            {
                bool singleImage = id == 98 || id == 99;
                int count = singleImage ? 1 : mob.NumberImage;
                for (int i = 0; i < count; i++)
                {
                    string file = "Img/Mob/" + session.ZoomLevel + "/" + id + (singleImage ? "" : "_" + i) + ".png";
                    byte[] png = world.LoadFile(file);
                    w.WriteInt(png.Length);
                    w.Write(png);
                }
            }
            catch (IOException ex)
            {
                session.Server.Log("Missing mob image " + id + " (zoom " + session.ZoomLevel + "): " + ex.Message);
                return;
            }

            w.WriteBoolean(mob.IsBoss);
            if (mob.IsBoss)
            {
                w.WriteByte(mob.FrameBossMove.Length);
                foreach (var value in mob.FrameBossMove) w.WriteSByte(value);
                w.WriteByte(mob.FrameBossAttack.Length);
                foreach (var attack in mob.FrameBossAttack)
                {
                    w.WriteByte(attack.Length);
                    foreach (var value in attack) w.WriteSByte(value);
                }
                w.WriteInt(1);
                WriteMobFrames(w, mob, id >= 236 && id != 259);
            }
            else w.WriteInt(0);
            session.Send(ms);
        }

        private static void WriteMobFrames(JavaWriter w, NsoServer.World.MobTemplate mob, bool modern)
        {
            w.WriteByte(mob.ImageInfo.Length);
            foreach (var image in mob.ImageInfo)
            {
                w.WriteByte(image.Id); w.WriteByte(image.X); w.WriteByte(image.Y); w.WriteByte(image.W); w.WriteByte(image.H);
            }
            w.WriteShort(mob.Frames.Length);
            foreach (var frame in mob.Frames)
            {
                w.WriteByte(frame.IdImg.Length);
                for (int i = 0; i < frame.IdImg.Length; i++)
                {
                    w.WriteShort(frame.Dx[i]); w.WriteShort(frame.Dy[i]); w.WriteByte(frame.IdImg[i]);
                    if (modern) { w.WriteByte(frame.Flip[i]); w.WriteByte(frame.OnTop[i]); }
                }
            }
            if (!modern) { w.WriteShort(0); return; }
            w.WriteByte(mob.Sequence.Length);
            foreach (short value in mob.Sequence) w.WriteShort(value);
            w.WriteByte(mob.FrameChar.Length);
            foreach (var frames in mob.FrameChar)
            {
                w.WriteByte(frames.Length);
                foreach (var value in frames) w.WriteSByte(value);
            }
            foreach (var value in mob.IndexSplash) w.WriteSByte(value);
        }

        // Controller.messageSubCommand: shop list, potential and skill points
        private void OnSubCommand(Message m)
        {
            sbyte sub = m.Reader.ReadSByte();
            var world = session.Server.World;
            switch (sub)
            {
                case -103: PlayerItems.RequestShop(m, actions.State, world); break;     // REQUEST_ITEM
                case -109: PlayerSkills.UpPotential(m, actions.State, world); break;    // POTENTIAL_UP
                case -108: PlayerSkills.UpSkill(m, actions.State, world); break;        // SKILL_UP
                case -107: PlayerItems.BagSort(actions.State, world); break;            // BAG_SORT
                default: session.Server.Log("Client " + session.Id + ": sub " + sub); break;
            }
        }

        private void NpcNotAvailable()
        {
            var ms = new Message(Cmd.ALERT_MESSAGE);
            ms.Writer.WriteUTF("Th\u00f4ng b\u00e1o");
            ms.Writer.WriteUTF("Ch\u1ee9c n\u0103ng n\u00e0y ch\u01b0a c\u00f3 trong b\u1ea3n offline.");
            session.Send(ms);
        }

        private void Dialog(string text)
        {
            var ms = new Message(Cmd.SERVER_DIALOG);
            ms.Writer.WriteUTF(text);
            session.Send(ms);
        }
    }
}
