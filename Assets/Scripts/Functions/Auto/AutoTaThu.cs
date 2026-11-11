namespace Functions.Auto
{
    public class AutoTaThu : Auto
    {
        private TaskOrder taskOder;
        public int killId;
        public static bool start;
        public static long x;
        public override void Run()
        {
            //if (mapID > 0)
            //{
            //    bool var10000;
            //    int var1;
            //    if (Auto.checkChar())
            //    {
            //        if (Actions.AttackParty && TileMap.mapID == this.mapID && TileMap.zoneID == this.zoneID && Char.getMyChar().mobFocus != null && Char.getMyChar().mobFocus.hp < Char.getMyChar().mobFocus.maxHp / 20)
            //        {
            //            var1 = 0;

            //            while (true)
            //            {
            //                if (var1 >= GameScr.vParty.size())
            //                {
            //                    var10000 = false;
            //                    break;
            //                }

            //                Party var2;
            //                if ((var2 = (Party)GameScr.vParty.elementAt(var1)).c != null && var2.c.cHP > 0)
            //                {
            //                    var10000 = true;
            //                    break;
            //                }

            //                ++var1;
            //            }
            //        }
            //        else
            //        {
            //            var10000 = false;
            //        }
            //        if (!var10000)
            //        {
            //            Hoisinh(true);
            //            return;
            //        }
            //    }
            //    else if (TileMap.mapID ==this.mapID && TileMap.zoneID == super.khuAuto)
            //    {
            //        if (this.y != null && this.y.b >= this.y.c)
            //        {
            //            GameScr.c("Xong Tà Thú");
            //            Code.c();
            //            return;
            //        }

            //        if (Char.i().bd.equals(Code.h))
            //        {
            //            if (Char.i().co != null && Char.i().co.b < Char.i().co.c / 10)
            //            {
            //                if (!Class_fl.ag())
            //                {
            //                    Service.a().k("waitGr");
            //                    Class_fl.a(200000L);
            //                    Service.a().k("notifyGr");
            //                }

            //                var10000 = false;
            //            }
            //            else
            //            {
            //                var10000 = false;
            //            }
            //        }
            //        else
            //        {
            //            if (w && System.currentTimeMillis() - x > 120000L)
            //            {
            //                w = false;
            //            }

            //            var10000 = w;
            //        }

            //        if (!var10000)
            //        {
            //            this.b(this.a, 8);
            //        }

            //        if (Char.i().z < Char.i().aa * Char.er / 100)
            //        {
            //            Char.i().e(17);
            //        }

            //        if (Char.i().ab < Char.i().ad * Char.eq / 100)
            //        {
            //            var1 = (int)(System.currentTimeMillis() / 1000L);

            //            for (int var4 = 0; var4 < Char.i().bw.size(); ++var4)
            //            {
            //                Effect var3;
            //                if ((var3 = (Effect)Char.i().bw.elementAt(var4)).e.a == 21 && var3.c - (var1 - var3.b) >= 2)
            //                {
            //                    return;
            //                }
            //            }

            //            Char.i().e(16);
            //            return;
            //        }
            //    }
            //    else
            //    {
            //        this.a(super.mapAuto, super.khuAuto, super.xAuto, super.yAuto);
            //    }
            //}
        }
        public void Init()
        {
            this.Update();
            this.taskOder = Char.FindTask(1);
            if (this.taskOder != null)
            {
                this.killId = this.taskOder.killId;
                this.mapID = this.taskOder.mapId;
                if (TileMap.mapID == this.taskOder.mapId && TileMap.zoneID % 5 == 0)
                {
                    this.zoneID = TileMap.zoneID;
                    return;
                }
            }

            this.zoneID = 5;
            start = false;
        }

        public void Init2(int var1, int var2)
        {
            this.Update();
            this.taskOder = null;
            this.killId = var2;
            this.mapID = var1;
            if (TileMap.mapID == var1 && TileMap.zoneID % 5 == 0)
            {
                this.mapID = TileMap.zoneID;
            }
            else
            {
                this.zoneID = 5;
            }

        }
        public void getTask()
        {
            this.taskOder = Char.FindTask(1);

        }
    }
}
