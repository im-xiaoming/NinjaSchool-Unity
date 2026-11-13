using System.Threading;

namespace Functions.Auto
{
    public class Clock
    {
        public static object typeUI2;
        public static object typeUI4;
        public static object typeUI6;
        public static object typeUI7;
        public static object typeUI8;
        public static object typeUI9;
        public static object typeUI14;
        public static object typeUI15;
        public static object typeUI16;
        public static object typeUI18;
        public static object typeUI19;
        public static object typeUI20;
        public static object typeUI21;
        public static object typeUI22;
        public static object typeUI23;
        public static object typeUI24;
        public static object typeUI25;
        public static object typeUI17;
        public static object typeUI26;
        public static object typeUI27;
        public static object typeUI28;
        public static object typeUI29;
        public static object typeUI32;
        public static object typeUI34;
        public static object typeUI35;
        public static object typeUI51;
        public static bool istypeUI2;
        public static bool istypeUI4;
        public static bool istypeUI6;
        public static bool istypeUI7;
        public static bool istypeUI8;
        public static bool istypeUI9;
        public static bool istypeUI14;
        public static bool istypeUI15;
        public static bool istypeUI16;
        public static bool istypeUI17;
        public static bool istypeUI18;
        public static bool istypeUI19;
        public static bool istypeUI20;
        public static bool istypeUI21;
        public static bool istypeUI22;
        public static bool istypeUI23;
        public static bool istypeUI24;
        public static bool istypeUI25;
        public static bool istypeUI26;
        public static bool istypeUI27;
        public static bool istypeUI28;
        public static bool istypeUI29;
        public static bool istypeUI32;
        public static bool istypeUI34;
        public static bool istypeUI35;
        public static bool istypeUI51;
        public static object objBuy;
        public static bool isBuy;
        private static bool f = false;
        private static object r = new object ();
        public static void LockTypeUI(int typeUI, int milisSeconds = 1000)
        {
            switch (typeUI)
            {
                case 2:
                    istypeUI2 = true;
                    lock (typeUI2)
                    {
                        Monitor.Wait(typeUI2, milisSeconds);
                        break;
                    }

                case 4:
                    istypeUI4 = true;
                    lock (typeUI4)
                    {
                        Monitor.Wait(typeUI4, milisSeconds);
                        break;
                    }

                case 6:
                    istypeUI6 = true;
                    lock (typeUI6)
                    {
                        Monitor.Wait(typeUI6, milisSeconds);
                        break;
                    }

                case 7:
                    istypeUI7 = true;
                    lock (typeUI7)
                    {
                        Monitor.Wait(typeUI7, milisSeconds);
                        break;
                    }

                case 8:
                    istypeUI8 = true;
                    lock (typeUI8)
                    {
                        Monitor.Wait(typeUI8, milisSeconds);
                        break;
                    }

                case 9:
                    istypeUI9 = true;
                    lock (typeUI9)
                    {
                        Monitor.Wait(typeUI9, milisSeconds);
                        break;
                    }

                case 14:
                    istypeUI14 = true;
                    lock (typeUI14)
                    {
                        Monitor.Wait(typeUI14, milisSeconds);
                        break;
                    }

                case 15:
                    istypeUI15 = true;
                    lock (typeUI15)
                    {
                        Monitor.Wait(typeUI15, milisSeconds);
                        break;
                    }

                case 16:
                    istypeUI16 = true;
                    lock (typeUI16)
                    {
                        Monitor.Wait(typeUI16, milisSeconds);
                        break;
                    }

                case 17:
                    istypeUI17 = true;
                    lock (typeUI17)
                    {
                        Monitor.Wait(typeUI17, milisSeconds);
                        break;
                    }

                case 18:
                    istypeUI18 = true;
                    lock (typeUI18)
                    {
                        Monitor.Wait(typeUI18, milisSeconds);
                        break;
                    }

                case 19:
                    istypeUI19 = true;
                    lock (typeUI19)
                    {
                        Monitor.Wait(typeUI19, milisSeconds);
                        break;
                    }

                case 20:
                    istypeUI20 = true;
                    lock (typeUI20)
                    {
                        Monitor.Wait(typeUI20, milisSeconds);
                        break;
                    }

                case 21:
                    istypeUI21 = true;
                    lock (typeUI21)
                    {
                        Monitor.Wait(typeUI21, milisSeconds);
                        break;
                    }

                case 22:
                    istypeUI22 = true;
                    lock (typeUI22)
                    {
                        Monitor.Wait(typeUI22, milisSeconds);
                        break;
                    }

                case 23:
                    istypeUI23 = true;
                    lock (typeUI23)
                    {
                        Monitor.Wait(typeUI23, milisSeconds);
                        break;
                    }

                case 24:
                    istypeUI24 = true;
                    lock (typeUI24)
                    {
                        Monitor.Wait(typeUI24, milisSeconds);
                        break;
                    }

                case 25:
                    istypeUI25 = true;
                    lock (typeUI25)
                    {
                        Monitor.Wait(typeUI25, milisSeconds);
                        break;
                    }

                case 26:
                    istypeUI26 = true;
                    lock (typeUI26)
                    {
                        Monitor.Wait(typeUI26, milisSeconds);
                        break;
                    }

                case 27:
                    istypeUI27 = true;
                    lock (typeUI27)
                    {
                        Monitor.Wait(typeUI27, milisSeconds);
                        break;
                    }

                case 28:
                    istypeUI28 = true;
                    lock (typeUI28)
                    {
                        Monitor.Wait(typeUI28, milisSeconds);
                        break;
                    }

                case 29:
                    istypeUI29 = true;
                    lock (typeUI29)
                    {
                        Monitor.Wait(typeUI29, milisSeconds);
                        break;
                    }

                case 32:
                    istypeUI32 = true;
                    lock (typeUI32)
                    {
                        Monitor.Wait(typeUI32, milisSeconds);
                        break;
                    }

                case 34:
                    istypeUI34 = true;
                    lock (typeUI34)
                    {
                        Monitor.Wait(typeUI34, milisSeconds);
                        break;
                    }

                case 35:
                    istypeUI35 = true;
                    lock (typeUI35)
                    {
                        Monitor.Wait(typeUI35, milisSeconds);
                        break;
                    }

                case 51:
                    istypeUI51 = true;
                    lock (typeUI51)
                    {
                        Monitor.Wait(typeUI51, milisSeconds);
                        break;
                    }
            }
        }

        static Clock()
        {
            objBuy = new object ();
            typeUI2 = new object ();
            typeUI4 = new object ();
            typeUI6 = new object ();
            typeUI7 = new object ();
            typeUI8 = new object ();
            typeUI9 = new object ();
            typeUI14 = new object ();
            typeUI15 = new object ();
            typeUI16 = new object ();
            typeUI18 = new object ();
            typeUI19 = new object ();
            typeUI20 = new object ();
            typeUI21 = new object ();
            typeUI22 = new object ();
            typeUI23 = new object ();
            typeUI24 = new object ();
            typeUI25 = new object ();
            typeUI17 = new object ();
            typeUI26 = new object ();
            typeUI27 = new object ();
            typeUI28 = new object ();
            typeUI29 = new object ();
            typeUI32 = new object ();
            typeUI34 = new object ();
            typeUI35 = new object ();
            typeUI51 = new object ();
        }

        public static void HuyLockTypeUI(int typeUI)
        {
            switch (typeUI)
            {
                case 2:
                    if (istypeUI2)
                    {
                        lock (typeUI2)
                        {
                            Monitor.PulseAll(typeUI2);
                        }

                        istypeUI2 = false;
                    }

                    break;
                case 4:
                    if (istypeUI4)
                    {
                        lock (typeUI4)
                        {
                            Monitor.PulseAll(typeUI4);
                        }

                        istypeUI4 = false;
                    }

                    break;
                case 6:
                    if (istypeUI6)
                    {
                        lock (typeUI6)
                        {
                            Monitor.PulseAll(typeUI6);
                        }

                        istypeUI6 = false;
                    }

                    break;
                case 7:
                    if (istypeUI7)
                    {
                        lock (typeUI7)
                        {
                            Monitor.PulseAll(typeUI7);
                        }

                        istypeUI7 = false;
                    }

                    break;
                case 8:
                    if (istypeUI8)
                    {
                        lock (typeUI8)
                        {
                            Monitor.PulseAll(typeUI8);
                        }

                        istypeUI8 = false;
                    }

                    break;
                case 9:
                    if (istypeUI9)
                    {
                        lock (typeUI9)
                        {
                            Monitor.PulseAll(typeUI9);
                        }

                        typeUI9 = false;
                    }

                    break;
                case 14:
                    if (istypeUI14)
                    {
                        lock (typeUI14)
                        {
                            Monitor.PulseAll(typeUI14);
                        }

                        istypeUI14 = false;
                    }

                    break;
                case 15:
                    if (istypeUI15)
                    {
                        lock (typeUI15)
                        {
                            Monitor.PulseAll(typeUI15);
                        }

                        istypeUI15 = false;
                    }

                    break;
                case 16:
                    if (istypeUI16)
                    {
                        lock (typeUI16)
                        {
                            Monitor.PulseAll(typeUI16);
                        }

                        istypeUI16 = false;
                    }

                    break;
                case 17:
                    if (istypeUI17)
                    {
                        lock (typeUI17)
                        {
                            Monitor.PulseAll(typeUI17);
                        }

                        istypeUI17 = false;
                    }

                    break;
                case 18:
                    if (istypeUI18)
                    {
                        lock (typeUI18)
                        {
                            Monitor.PulseAll(typeUI18);
                        }

                        istypeUI18 = false;
                    }

                    break;
                case 19:
                    if (istypeUI19)
                    {
                        lock (typeUI19)
                        {
                            Monitor.PulseAll(typeUI19);
                        }

                        istypeUI19 = false;
                    }

                    break;
                case 20:
                    if (istypeUI20)
                    {
                        lock (typeUI20)
                        {
                            Monitor.PulseAll(typeUI20);
                        }

                        istypeUI20 = false;
                    }

                    break;
                case 21:
                    if (istypeUI21)
                    {
                        lock (typeUI21)
                        {
                            Monitor.PulseAll(typeUI21);
                        }

                        istypeUI21 = false;
                    }

                    break;
                case 22:
                    if (istypeUI22)
                    {
                        lock (typeUI22)
                        {
                            Monitor.PulseAll(typeUI22);
                        }

                        istypeUI22 = false;
                    }

                    break;
                case 23:
                    if (istypeUI23)
                    {
                        lock (typeUI23)
                        {
                            Monitor.PulseAll(typeUI23);
                        }

                        istypeUI23 = false;
                    }

                    break;
                case 24:
                    if (istypeUI24)
                    {
                        lock (typeUI24)
                        {
                            Monitor.PulseAll(typeUI24);
                        }

                        istypeUI24 = false;
                    }

                    break;
                case 25:
                    if (istypeUI25)
                    {
                        lock (typeUI25)
                        {
                            Monitor.PulseAll(typeUI25);
                        }

                        istypeUI25 = false;
                    }

                    break;
                case 26:
                    if (istypeUI26)
                    {
                        lock (typeUI26)
                        {
                            Monitor.PulseAll(typeUI26);
                        }

                        istypeUI26 = false;
                    }

                    break;
                case 27:
                    if (istypeUI27)
                    {
                        lock (typeUI27)
                        {
                            Monitor.PulseAll(typeUI27);
                        }

                        istypeUI27 = false;
                    }

                    break;
                case 28:
                    if (istypeUI28)
                    {
                        lock (typeUI28)
                        {
                            Monitor.PulseAll(typeUI28);
                        }

                        istypeUI28 = false;
                    }

                    break;
                case 29:
                    if (istypeUI29)
                    {
                        lock (typeUI29)
                        {
                            Monitor.PulseAll(typeUI29);
                        }

                        istypeUI29 = false;
                    }

                    break;
                case 32:
                    if (istypeUI32)
                    {
                        lock (typeUI32)
                        {
                            Monitor.PulseAll(typeUI32);
                        }

                        istypeUI32 = false;
                    }

                    break;
                case 34:
                    if (istypeUI34)
                    {
                        lock (typeUI34)
                        {
                            Monitor.PulseAll(typeUI34);
                        }

                        istypeUI34 = false;
                    }

                    break;
                case 35:
                    if (istypeUI35)
                    {
                        lock (typeUI35)
                        {
                            Monitor.PulseAll(typeUI35);
                        }

                        istypeUI35 = false;
                    }

                    break;
                case 51:
                    if (istypeUI51)
                    {
                        lock (typeUI51)
                        {
                            Monitor.PulseAll(typeUI51);
                        }

                        istypeUI51 = false;
                    }

                    break;
            }
        }

        public static void LockBuy(int milisSeconds = 4000)
        {
            isBuy = true;
            lock (objBuy)
            {
                Monitor.Wait(objBuy, milisSeconds);
            }
        }

        public static void HuyLockBuy()
        {
            if (isBuy)
            {
                lock (objBuy)
                {
                    Monitor.Pulse(objBuy);
                }
            }

            isBuy = false;
        }

        public static void Lock()
        {
            f = true;
            lock (r)
            {
                Monitor.Wait(r, 3000);
            }
        }

        public static void CancelLock()
        {
            if (f)
            {
                lock (r)
                {
                    Monitor.PulseAll(r);
                }

                f = false;
            }
        }
    }
}
