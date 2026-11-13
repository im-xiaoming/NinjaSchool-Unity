using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Functions.Auto
{
    public class Handle
    {
        public static int dem;
        public static int count;
        public static Thread GetThread;
        public static ThreadStart GetthreadStart;
        public static Auto T;
        public static bool DkChay;
        public static TanSat tanSat;
        public static long timeAutouse;
        public static long w;
        public static MyVector s;
        public static long timeChangeZone;
        public static int demTask;
        public static AutoTask autoTask;
        public static AutoNVHN autoNVHN;
        public static Buff buff;
        public static int ccc;
        public static int CharIDSave;
        public static bool isHackGiay = true;
        public static int HackGiay = 8;
        public static MyVector vCharInMap;
        public static MyVector vItemMap;
        public static bool FirstZone;
        public static bool bagshort;
        public static int test;
        public static MyVector Item;
        public static long timeDo;
        public static bool acUse;
        public static long lastimeAutoDel = 0;
        public static int SpNextMap;
        public static short[] arrItemDell = new short[120];
        public static int NextMinMaxInt => new System.Random().Next(int.MinValue, int.MaxValue);

        public static void Sleep(int milisSeconds)
        {
            try
            {
                Thread.Sleep(milisSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        public static void Paint(string text)
        {
            ChatPopup.addChatPopupMultiLine("[Xin chờ] " + text, 1000, Char.getMyChar());
        }

        public static void addItemDell(short var0)
        {
            int var1;
            for (var1 = 0; var1 < arrItemDell.Length; ++var1)
            {
                if (arrItemDell[var1] == var0)
                {
                    return;
                }
            }

            var1 = -1;
            for (int var2 = 0; var2 < arrItemDell.Length; ++var2)
            {
                if (arrItemDell[var2] < 0)
                {
                    var1 = var2;
                    break;
                }
            }

            if (var1 == -1)
            {
                var1 = arrItemDell.Length;
                short[] var4;
                Array.Copy(var4 = new short[arrItemDell.Length + 10], 0, arrItemDell, 0, arrItemDell.Length);
                for (int var3 = arrItemDell.Length; var3 < var4.Length; ++var3)
                {
                    var4[var3] = -1;
                }

                arrItemDell = var4;
            }

            arrItemDell[var1] = var0;
        }

        public static void removeItemDell(short var0)
        {
            for (int var1 = 0; var1 < arrItemDell.Length; ++var1)
            {
                if (arrItemDell[var1] == var0)
                {
                    arrItemDell[var1] = -1;
                }
            }
        }

        public static void sortItemDel()
        {
            for (int var0 = 0; var0 < arrItemDell.Length; ++var0)
            {
                if (arrItemDell[var0] > 0)
                {
                    for (int var1 = 0; var1 <= var0; ++var1)
                    {
                        if (arrItemDell[var1] == -1)
                        {
                            arrItemDell[var1] = arrItemDell[var0];
                            arrItemDell[var0] = -1;
                            break;
                        }
                    }
                }
            }
        }

        public static void GoMap(int mapID)
        {
            new Thread((ThreadStart)delegate
            {
                GameCanvas.startWaitDlgWithoutCancel();
                try
                {
                    TileMap.GoMap(mapID);
                }
                catch (Exception value)
                {
                    Console.WriteLine(value);
                }

                GC.Collect();
                if (Session_ME.connecting)
                {
                    GameScr.gI().switchToMe();
                }

                GameCanvas.endDlg();
                GameCanvas.isLoading = false;
            }).Start();
        }

        public static void GetPngFile(Image image, string savePath)
        {
            Texture2D texture = image.texture;
            texture.SetPixel(texture.width - 30, texture.height - 30, Color.black);
            texture.Apply();
            File.WriteAllBytes(savePath + ".png", image.texture.EncodeToPNG());
        }

        public static void Fixed()
        {
            dem = new System.Random().Next(0, 7);
            if (Auto.isDie(Char.getMyChar()) && (TileMap.IsDangGoMap || TileMap.MapMod != TileMap.mapID))
            {
                TileMap.HuyLockMap();
            }
        }

        public static void GetImage()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        for (int i = 0; i < GameScr.vMob.size(); i++)
                        {
                            Mob mob = (Mob)GameScr.vMob.elementAt(i);
                            if (mob == null)
                            {
                                continue;
                            }

                            MobTemplate mobTemplate = Mob.arrMobTemplate[mob.templateId];
                            string path = string.Concat("Ảnh/x" + mGraphics.zoomLevel + "/map/", TileMap.mapID, "/mob/", mob.templateId);
                            int num = dem;
                            for (int j = 0; j < num; j++)
                            {
                                string text = string.Concat("Ảnh/x" + mGraphics.zoomLevel + "/map/", TileMap.mapID, "/mob/", mob.templateId, "/", j);
                                Image image = mobTemplate.imgs[j];
                                if (!Directory.Exists("Ảnh/x" + mGraphics.zoomLevel + "/map/" + TileMap.mapID))
                                {
                                    Directory.CreateDirectory("Ảnh/x" + mGraphics.zoomLevel + "/map/" + TileMap.mapID);
                                }

                                if (!Directory.Exists("Ảnh/x" + mGraphics.zoomLevel + "/map/" + TileMap.mapID + "/mob"))
                                {
                                    Directory.CreateDirectory("Ảnh/x" + mGraphics.zoomLevel + "/map/" + TileMap.mapID + "/mob");
                                }

                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                }

                                if (image != null && !File.Exists(text))
                                {
                                    GetPngFile(image, text);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    Sleep(10);
                }
            }
            catch (Exception)
            {
            }
        }

        static Handle()
        {
            Item = new MyVector();
            SpNextMap = 500;
            test = 23123213;
            buff = new Buff();
            autoTask = new AutoTask();
            autoNVHN = new AutoNVHN();
            tanSat = new TanSat();
            s = new MyVector();
            for (int i = 0; i < arrItemDell.Length; ++i)
            {
                arrItemDell[i] = -1;
            }
        }

        public static void NextMap(int way)
        {
            new Thread((ThreadStart)delegate
            {
                GameCanvas.startWaitDlgWithoutCancel();
                try
                {
                    TileMap.NextMap(way);
                }
                catch (Exception value)
                {
                    Console.WriteLine(value);
                }

                GC.Collect();
                if (Session_ME.connecting)
                {
                    GameScr.gI().switchToMe();
                }

                GameCanvas.endDlg();
                GameCanvas.isLoading = false;
            }).Start();
        }

        public static bool delItem(Item var0)
        {
            if (T is AutoTask)
            {
                return false;
            }
            else if (var0 == null)
            {
                return false;
            }
            else if (var0.upgrade > 0)
            {
                var0.v = true;
                return false;
            }
            else
            {
                for (int var1 = 0; var1 < arrItemDell.Length; ++var1)
                {
                    if (arrItemDell[var1] > 0 && var0.template.id == arrItemDell[var1])
                    {
                        return true;
                    }
                }

                if (!var0.v && mSystem.currentTimeMillis() - var0.u >= 5000L)
                {
                    if (var0.template.type == 26 && var0.template.id < 11)
                    {
                        return true;
                    }
                    else
                    {
                        if (var0.template.type < 10 || var0.template.type >= 29 && var0.template.type <= 32)
                        {
                            if (var0.template.type < 10)
                            {
                                if (var0.template.level < 50)
                                {
                                    return true;
                                }

                                return false;
                            }

                            if (!var0.s && mSystem.currentTimeMillis() - var0.t > 5000L)
                            {
                                var0.t = mSystem.currentTimeMillis();
                                Service.gI().requestItemInfo(var0.typeUI, var0.indexUI);
                                if (!var0.s)
                                {
                                    return false;
                                }
                            }

                            if (var0.checkOption(85))
                            {
                                var0.v = true;
                                return false;
                            }

                            if (var0.template.type >= 29 && var0.template.type <= 32)
                            {
                                if (var0.saleCoinLock != 0)
                                {
                                    var0.v = true;
                                    return false;
                                }

                                return true;
                            }

                            if (var0.saleCoinLock >= 0)
                            {
                                return true;
                            }

                            bool var5;
                            if (var0.checkOption(2))
                            {
                                var5 = true;
                            }
                            else
                            {
                                var5 = false;
                            }

                            if (!var5)
                            {
                                return true;
                            }

                            if (var0.template.type == 1)
                            {
                                if (var0.checkOption(0) && var0.checkOption(1))
                                {
                                    if (!var0.checkOption(8) && !var0.checkOption(9))
                                    {
                                        return true;
                                    }

                                    if (!var0.checkOption(10))
                                    {
                                        return true;
                                    }

                                    var0.v = true;
                                    return false;
                                }

                                return true;
                            }

                            if (!var0.checkOption(6) || !var0.checkOption(7))
                            {
                                return true;
                            }

                            if (var0.countOptions(0) < 2)
                            {
                                return true;
                            }

                            if (var0.template.type == 8 && !var0.checkOption(16))
                            {
                                return true;
                            }
                        }

                        var0.v = true;
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static void Run()
        {
            try
            {
                while (DkChay)
                {
                    long num = mSystem.currentTimeMillis();
                    try
                    {
                        Char var3 = Char.getMyChar();
                        if (T != null)
                        {
                            T.Run();
                        }

                        if (mSystem.currentTimeMillis() - GameScr.lastimeAutouse > 2000)
                        {
                            int var9;
                            int var7;
                            int var5;
                            Effect var10;
                            for (var9 = 0; var9 < GameScr.ItemAutoUse.size(); ++var9)
                            {
                                var7 = (int)GameScr.ItemAutoUse.elementAt(var9);
                                ItemTemplate var27 = ItemTemplates.get((short)(var7));
                                if (Char.checkItem(var7))
                                {
                                    bool effectExists = false;
                                    for (var5 = 0; var5 < var3.vEff.size(); ++var5)
                                    {
                                        if ((var10 = (Effect)var3.vEff.elementAt(var5)) != null && var10.template.iconId == var27.iconID)
                                        {
                                            effectExists = true;
                                            break;
                                        }
                                    }

                                    if (!effectExists && (var5 = Char.checkIndexBag(var7)) >= 0)
                                    {
                                        Service.gI().useItem(var5);
                                        continue;
                                    }
                                }
                                else
                                {
                                    GameScr.ItemAutoUse.removeElementAt(var9);
                                    --var9;
                                }
                            }

                            GameScr.lastimeAutouse = mSystem.currentTimeMillis();
                        }
                    }
                    catch (Exception)
                    {
                    }

                    long num2 = mSystem.currentTimeMillis() - num;
                    if (mSystem.currentTimeMillis() - num < 100)
                    {
                        Thread.Sleep((int)(100 - num2));
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SwitchToMe()
        {
            if (!DkChay)
            {
                DkChay = true;
                GetthreadStart = Run;
                GetThread = new Thread(GetthreadStart);
                GetThread.Start();
            }

            CharIDSave = Char.getMyChar().charID;
        }

        public static void Abort()
        {
            if (T != null)
            {
                T = null;
            }

            if (GetThread != null)
            {
                GetThread.Abort();
                GetThread = null;
                if (GetthreadStart != null)
                {
                    GetthreadStart = null;
                }
            }

            GetthreadStart = Run;
            GetThread = new Thread(GetthreadStart);
            GetThread.Start();
        }

        public static void Stop()
        {
            Auto t = T;
            DkChay = false;
            if (T != null)
            {
                T = null;
            }

            if (GetThread != null)
            {
                GetThread.Abort();
                GetThread = null;
                if (GetthreadStart != null)
                {
                    GetthreadStart = null;
                }
            }

            if (GetThread != null)
            {
                GetThread.Abort();
                GetThread = null;
                if (GetthreadStart != null)
                {
                    GetthreadStart = null;
                }
            }

            T = t;
        }

        public static void S(Auto auto)
        {
            auto.T = T;
            T = auto;
        }

        public static bool boolItemBagId(int paramInt)
        {
            Item[] arrItemBag = Char.getMyChar().arrItemBag;
            for (int i = 0; i < arrItemBag.Length; i++)
            {
                if (arrItemBag[i] != null && arrItemBag[i].template.id == paramInt)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Die()
        {
            Npc npc = Char.FindNpc(13);
            if (TileMap.mapID == 20)
            {
                Char.Move(TileMap.pxw, TileMap.pxh);
            }
            else if (npc == null)
            {
                if (!boolItemBagId(37) && !boolItemBagId(35))
                {
                    Char.Move(Char.getMyChar().cx, YDirt(Char.getMyChar().cx, Char.getMyChar().cy));
                    Service.gI().openUIZone();
                }
                else
                {
                    Char.Move(TileMap.pxw, TileMap.pxh);
                }
            }
            else if (Math.abs(npc.cx - Char.getMyChar().cx) <= 200 && Math.abs(npc.cy - Char.getMyChar().cy) <= 200)
            {
                if (!boolItemBagId(37) && !boolItemBagId(35))
                {
                    if (npc.cx < Char.getMyChar().cx)
                    {
                        Char.Move((Char.getMyChar().cx + 200 >= TileMap.pxw) ? (Char.getMyChar().cx - 200 - Math.abs(Char.getMyChar().cx - npc.cx)) : (Char.getMyChar().cx + 200), YDirt(Char.getMyChar().cx + 200, Char.getMyChar().cy));
                    }
                    else
                    {
                        Char.Move((Char.getMyChar().cx - 200 <= 0) ? (Char.getMyChar().cx + 200 + Math.abs(Char.getMyChar().cx - npc.cx)) : (Char.getMyChar().cx - 200), YDirt(Char.getMyChar().cx + 200, Char.getMyChar().cy));
                    }

                    Char.Move(Char.getMyChar().cx, YDirt(Char.getMyChar().cx, Char.getMyChar().cy));
                    Service.gI().openUIZone();
                }
                else
                {
                    Char.Move(Char.getMyChar().cx, TileMap.pxh);
                }
            }
            else if (!boolItemBagId(37) && !boolItemBagId(35))
            {
                Char.Move(Char.getMyChar().cx, YDirt(Char.getMyChar().cx, Char.getMyChar().cy));
                Service.gI().openUIZone();
            }
            else
            {
                Char.Move(Char.getMyChar().cx, TileMap.pxh);
            }
        }

        public static int YDirt(int cx, int cy)
        {
            cy = TileMap.tileYofPixel(cy);
            for (int i = 0; i < 240; i++)
            {
                int num = cy + i * 12;
                if (TileMap.tileTypeAt(cx, num, 2))
                {
                    return num;
                }
            }

            for (int j = 0; j < 240; j++)
            {
                int num2 = cy + j * 12;
                if (TileMap.tileTypeAt(cx, num2, 2))
                {
                    return num2;
                }
            }

            return cy;
        }

        public static void PaintG(mGraphics g)
        {
        }

        public static void Update()
        {
        }

        public static void TS(int templateId, int mobId = -1)
        {
            tanSat.update(templateId, mobId);
            S(tanSat);
        }

        public static void ATNVHN()
        {
            autoNVHN.update();
            S(autoNVHN);
        }

        public static void AT(int phai, bool isLevel10, int LorETask = -1)
        {
            autoTask.update(phai, isLevel10, LorETask);
            S(autoTask);
        }

        public static void BHSX(bool isHsx = true, bool isBuff = true)
        {
            buff.update(isHsx, isBuff);
            S(buff);
        }

        public static bool ToBoolean(int value)
        {
            return value.Equals(0);
        }

        public static int ToInt32(bool value)
        {
            if (value.Equals(obj: true))
            {
                return 0;
            }

            return 1;
        }

        public static void LoadAuto()
        {
            sbyte[] autoData = RMS.loadRMS("NjAuto");
            if (autoData == null || autoData.Length == 0)
                return; // first run: nothing saved yet
            DataInputStream dataInputStream = new DataInputStream(autoData);
            if (dataInputStream != null)
            {
                try
                {
                    myReader dis = dataInputStream.r;
                    Char.isAHP = dis.readBoolean();
                    Char.aHpValue = dis.readInt();
                    Char.isAMP = dis.readBoolean();
                    Char.aMpValue = dis.readInt();
                    Char.isAFood = dis.readBoolean();
                    Char.aFoodValue = dis.readInt();
                    Char.isABuff = dis.readBoolean();
                    int lent = dis.readInt();
                    int i;
                    if (arrItemDell.Length < lent)
                    {
                        arrItemDell = new short[10 * (lent / 10 + 1)];
                    }

                    for (i = 0; i < lent; ++i)
                    {
                        arrItemDell[i] = dis.readShort();
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Load auto err " + e.StackTrace);
                }
                finally
                {
                    try
                    {
                        dataInputStream.close();
                    }
                    catch (Exception ex2)
                    {
                        Out.println(ex2.Message + ex2.StackTrace);
                    }
                }
            }

            bagshort = false;
        }

        public static void SaveAuto()
        {
            timeChangeZone = 0L;
            DataOutputStream dos = new DataOutputStream();
            try
            {
                dos.writeBoolean(Char.isAHP);
                dos.writeInt(Char.aHpValue);
                dos.writeBoolean(Char.isAMP);
                dos.writeInt(Char.aMpValue);
                dos.writeBoolean(Char.isAFood);
                dos.writeInt(Char.aFoodValue);
                dos.writeBoolean(Char.isABuff);
                int lent = 0;
                int i;
                for (i = 0; i < arrItemDell.Length; ++i)
                {
                    if (arrItemDell[i] >= 0)
                    {
                        ++lent;
                    }
                }

                dos.writeInt(lent);
                for (i = 0; i < arrItemDell.Length; ++i)
                {
                    if (arrItemDell[i] >= 0)
                    {
                        dos.writeShort(arrItemDell[i]);
                    }
                }

                RMS.saveRMS("NjAuto", dos.toByteArray());
            }
            catch (Exception e)
            {
                Debug.LogError("Save auto err " + e.Message);
            }
            finally
            {
                try
                {
                    dos.close();
                }
                catch (Exception ex2)
                {
                    Out.println(ex2.Message + ex2.StackTrace);
                }
            }

            string path = Application.persistentDataPath + "/buffmap";
            string path2 = Application.persistentDataPath + "/buffzone";
            File.WriteAllText(path, string.Concat(-2));
            File.WriteAllText(path2, string.Concat(-2));
        }

        public static void atcAutoUse(Item item)
        {
            try
            {
                int var1 = 0;
                do
                {
                    if (!(GameCanvas.currentScreen is GameScr))
                    {
                        return;
                    }

                    if (mSystem.currentTimeMillis() - timeAutouse >= 50)
                    {
                        Service.gI().useItem(item.indexUI);
                        Thread.Sleep(50);
                        timeAutouse = mSystem.currentTimeMillis();
                        var1++;
                        if ((var1 >= item.quantity))
                        {
                            break;
                        }
                    }
                }
                while (Char.solg_hanhtrang() > 0);
            }
            catch (Exception e)
            {
            }
        }
    }
}
