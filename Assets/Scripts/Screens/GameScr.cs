using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr : mScreen, IActionListener, IChatable
{
    public static GameScr instance;
    public static int gW;
    public static bool CheDoDem;
    public static int gH;
    public static bool nhanvat;
    public static bool isTudanh;
    public static bool HoiSinhLuong;
    public static bool HutVatPham;
    public static Image night;
    public static int gW2;
    public static bool GiuLV;
    public static int gssw;
    public static int gssh;
    public static int gH34;
    public static int gW3;
    public static int gH3;
    public static int gH23;
    public static int gW23;
    public static int gH2;
    public static int csPadMaxH;
    public static int cmdBarH;
    public static int gW34;
    public static int gW6;
    public static int gH6;
    public static int cmx;
    public static int cmy;
    public static int cmdx;
    public static int cmdy;
    public static int cmvx;
    public static int cmvy;
    public static int cmtoX;
    public static int cmtoY;
    public static int cmxLim;
    public static int cmyLim;
    public static int gssx;
    public static int gssy;
    public static int gssxe;
    public static int gssye;
    public Command cmdback;
    public Command cmdBag;
    public Command cmdSkill;
    public Command cmdTiemnang;
    public Command cmdtrangbi;
    public Command cmdInfo;
    public Command cmdFocus;
    public Command cmdFire;
    public Command cmdBijuu;
    public Command cmdTrangbi2;
    public static int d;
    public static int mpPotion;
    public static int hpPotion;
    public static SkillPaint[] sks;
    public static Arrowpaint[] arrs;
    public static Part[] parts;
    public static EffectCharPaint[] efs;
    public static CharCountDown charcountdonw;
    private int moveUp;
    private int moveDow;
    private int idTypeTask;
    private bool isstarOpen;
    private bool isChangeSkill;
    private bool isShortcut;
    public static MyVector vMobSoul;
    public static MyVector vClan;
    public static MyVector vParty;
    public static MyVector vPtMap;
    public static MyVector vFriend;
    public static MyVector vList;
    public static MyVector vFriendWait;
    public static MyVector vEnemies;
    public static MyVector vCharInMap;
    public static MyVector vItemMap;
    public static MyVector vMobAttack;
    public static MyVector vSet;
    public static MyVector vMob;
    public static MyVector vNpc;
    public static MyVector vBuNhin;
    public static MyVector vLanterns;
    public static NClass[] nClasss;
    public static int indexSize;
    public static int indexTitle;
    public static int indexSelect;
    public static int indexRow;
    public static int indexRowMax;
    public static int indexMenu;
    public static int indexCard;
    public Item itemFocus;
    public static ItemOptionTemplate[] iOptionTemplates;
    public static SkillOptionTemplate[] sOptionTemplates;
    private static Scroll scrInfo;
    public static Scroll scrMain;
    private static GamePad gamePad;
    public static Item[] arrItemNonNam;
    public static Item[] arrItemNonNu;
    public static Item[] arrItemAoNam;
    public static Item[] arrItemAoNu;
    public static Item[] arrItemGangTayNam;
    public static Item[] arrItemGangTayNu;
    public static Item[] arrItemQuanNam;
    public static Item[] arrItemQuanNu;
    public static Item[] arrItemGiayNam;
    public static Item[] arrItemGiayNu;
    public static Item[] arrItemLien;
    public static Item[] arrItemNhan;
    public static Item[] arrItemNgocBoi;
    public static Item[] arrItemPhu;
    public static Item[] arrItemWeapon;
    public static Item[] arrItemStack;
    public static Item[] arrItemStackLock;
    public static Item[] arrItemGrocery;
    public static Item[] arrItemGroceryLock;
    public static Item[] arrItemStore;
    public static Item[] arrItemElites;
    public static Item[] arrItemClanShop;
    public static Item[] arrItemBook;
    public static Item[] arrItemFashion;
    public static Item[] arrItemUpPeal;
    public static Item[] arrItemUpGrade;
    public static Item[] arrItemSplit;
    public static Item[] arrItemTradeMe;
    public static Item[] arrItemTradeOrder;
    public static Item[] arrItemConvert;
    public static ItemStands[] arrItemStands;
    public static Item itemUpGrade;
    public static Item itemSplit;
    public static Item itemSell;
    public static short[] arrItemSprin;
    public int numSprinLeft;
    public static MyVector vItemUpGrade;
    public static bool isTypeXu;
    public static bool isViewNext;
    public static bool isViewClanMemOnline;
    public static bool isSortClanByPointWeek;
    public static bool isViewClanInvite;
    public static bool isChop;
    public static bool isMessageMenu;
    public static string titleInputText;
    public static bool isPaintRankedList;
    public static bool isPaintAuctionSale;
    public static bool isPaintAlert;
    public static bool isPaintTask;
    public static bool isPaintTeam;
    public static bool isPaintFindTeam;
    public static bool isPaintFriend;
    public static bool isPaintList;
    public static bool isPaintEnemies;
    public static bool isPaintItemInfo;
    public static bool isPaintSelectSkill;
    public static bool isPaintInfoMe;
    public static bool isPaintStore;
    public static bool isPaintEliteShop;
    public static bool isPaintNonNam;
    public static bool isPaintNonNu;
    public static bool isPaintAoNam;
    public static bool isPaintAoNu;
    public static bool isPaintGangTayNam;
    public static bool isPaintGangTayNu;
    public static bool isPaintQuanNam;
    public static bool isPaintQuanNu;
    public static bool isPaintGiayNam;
    public static bool isPaintGiayNu;
    public static bool isPaintLien;
    public static bool isPaintNhan;
    public static bool isPaintNgocBoi;
    public static bool isPaintPhu;
    public static bool isPaintWeapon;
    public static bool isPaintStack;
    public static bool isPaintStackLock;
    public static bool isPaintGrocery;
    public static bool isPaintGroceryLock;
    public static bool isPaintUpGrade;
    public static bool isPaintConvert;
    public static bool isPaintUpGradeGold;
    public static bool isPaintUpPearl;
    public static bool isPaintBox;
    public static bool isPaintItemDell;
    public static bool isPaintSplit;
    public static bool isPaintCharInMap;
    public static bool isPaintTrade;
    public static bool isPaintZone;
    public static bool isPaintAuto;
    public static bool isPaintMessage;
    public static bool isPaintClan;
    public static bool isRequestMember;
    public static bool isPaintLuckySpin;
    public static bool isPaintAuctionBuy;
    public static bool isPaintLuyenThach;
    public static bool isPaintTinhluyen;
    public static bool isPaintDichChuyen;
    public static bool isPaintLuyenNgoc;
    public static bool isPaintKhamNgoc;
    public static bool isPaintGotNgoc;
    public static bool isPaintThaoNgoc;
    public static bool isPaintLucky_Draw;
    public static bool isPaintGiaoDo;
    public static bool isShopOnline;
    public static Char currentCharViewInfo;
    public static long[] exps;
    public static int[] crystals;
    public static int[] upClothe;
    public static int[] upAdorn;
    public static int[] upWeapon;
    public static int[] coinUpCrystals;
    public static int[] coinUpClothes;
    public static int[] coinUpAdorns;
    public static int[] coinUpWeapons;
    public static int[] maxPercents;
    public static int[] goldUps;
    public static int[] coinGotngoc;
    public int zoneCol = 6;
    public int[] zones;
    public int[] pts;
    public int typeTrade;
    public int typeTradeOrder;
    public int coinTrade;
    public int coinTradeOrder;
    public int timeTrade;
    public int typeSortPrice;
    public int typeSortLevel;
    public int typeSortName;
    public int indexItemUse = -1;
    public int cLastFocusID = -1;
    public int cPreFocusID = -1;
    public bool isLockKey;
    public static sbyte[][] tasks;
    public static sbyte[][] mapTasks;
    public MyVector texts;
    public string textsTitle;
    public TField tfText;
    public static sbyte vcData;
    public static sbyte vcMap;
    public static sbyte vcSkill;
    public static sbyte vcItem;
    public static sbyte vsData;
    public static sbyte vsMap;
    public static sbyte vsSkill;
    public static sbyte vsItem;
    public static Image imgTopBar;
    public static Image imgArrow;
    public static Image imgArrow2;
    public static Image imgChat;
    public static Image imgMenu;
    public static Image imgFocus;
    public static Image imgButton;
    public static Image imgButton2;
    public static Image imgHpp;
    public static Image imgMpp;
    public static Image imgRight;
    public static Image imgRight2;
    public static Image imgSkill;
    public static Image imgMapBorder;
    public static Image imgLbtn;
    public static Image imgLbtnFocus;
    public static Image imgSelect;
    public static Image imgAnalog1;
    public static Image imgAnalog2;
    public static Image imgMenu1;
    public static Image imgMenu2;
    public static Image imgTf;
    public static Image imgMatcho;
    public static Image imgFiremoto;
    public string tradeName = string.Empty;
    public string tradeItemName = string.Empty;
    public int timeLengthMap;
    public int timeStartMap;
    public static sbyte typeViewInfo;
    public static sbyte typeActive;
    private int[] xM = new int[2];
    private int[] yM = new int[2];
    public long timePoint;
    private int[] xMounts;
    private int[] yMounts;
    public string[] YenCards = new string[9]
    {
        "10000",
        "20000",
        "30000",
        "50000",
        "100000",
        "200000",
        "500000",
        "1000000",
        "5000000"
    };
    public int yenTemp;
    public int typeba;
    public string[] yenValue;
    public static MyVector vItemTreeFront;
    public static MyVector vItemTreeBehind;
    public static MyVector vItemTreeBetwen;
    private static GamePad tgamePad;
    public static bool isTouchKey;
    public int wColoerMp = 85;
    public int hColorMp = 11;
    public static Image imgEggMonters;
    public static Image imgEffMob;
    public static Image imgEffMob1;
    public static Image imgEffMob2;
    public static Image imgEffMob3;
    public static Image imgsmokeFollow;
    public static Command cmd240;
    public static Command cmd360;
    public static Command cmdallmap;
    public static bool isUseitemAuto;
    public static bool isAllmap;
    private static Skill[] keySkill;
    private static Skill[] onScreenSkill;
    public Command menu;
    private Command cmdPotentialAdd;
    private Command cmdSkillUp;
    private Command cmdAddFriend;
    private Command cmdBijuuAddPoint;
    private Command cmdBijuuSkillUp;
    public static int shaking;
    public static int count;
    private static int deltaY;
    private int saveAuto;
    private long lastMove;
    private long lastFire;
    public static int auto;
    public bool lockAutoMove;
    private int nSkill;
    private int selectedIndexSkill = -1;
    private long longPress;
    public static bool isManualFocus;
    private long lastSendUpdatePostion;
    public int runArrow;
    private static int yTouchBar;
    private static int xL;
    private static int yL;
    private const int topIconSize = 24;
    private const int topIconGap = 14;
    private static int chatIconSize = 34;
    private static int xC;
    private static int yC;
    private static int xR;
    private static int yR;
    private static int xF;
    private static int yF;
    private static int xU;
    private static int yU;
    private static int xHP;
    private static int yHP;
    private static int xMP;
    private static int yMP;
    private static int xTG;
    private static int yTG;
    private static int[] xS;
    private static int[] yS;
    private static int xSkill;
    private static int ySkill;
    private static int padSkill;
    public static string[] flyTextString;
    public static int[] flyTextX;
    public static int[] flyTextY;
    public static int[] flyTextDx;
    public static int[] flyTextDy;
    public static int[] flyTextState;
    public static int[] flyTextColor;
    private static readonly int[] flyPaintX = new int[5], flyPaintY = new int[5];
    private static readonly int[] splashPaintX = new int[2], splashPaintY = new int[2];
    public static int[] splashX;
    public static int[] splashY;
    public static int[] splashState;
    public static int[] splashF;
    public static int[] splashDir;
    public static Image[] imgSplash;
    public static int cmdBarX;
    public static int cmdBarY;
    public static int cmdBarW;
    public static int hpBarX;
    public static int hpBarY;
    public static int hpBarW;
    public static int mpBarW;
    public static int expBarW;
    public static int lvPosX;
    public static int moneyPosX;
    public static int hpBarH;
    public static int girlHPBarY;
    public static int popupY;
    public static int popupX;
    public static int isborderIndex;
    public static int isselectedRow;
    private static Image imgNolearn;
    public static int indextabTrangbi;
    private int fhorsetrangbi;
    private int indexMember;
    private string[] arrClanInfo;
    private string[] arrClanDongGop;
    public int cmxp;
    public int cmvxp;
    public int cmdxp;
    public int cmxLimp;
    public int cmyLimp;
    public int cmyp;
    public int cmvyp;
    public int cmdyp;
    private int indexTiemNang;
    protected Command cmdGiaoDoMoveOut;
    private Command cmdTradeLock;
    private Command cmdTradeAccept;
    protected Command cmdKhamNgocMoveOut_Ngoc;
    protected Command cmdKhamNgocMoveOut_Item;
    protected Command cmdKhamNgocMoveOut_Stone;
    protected Command cmdUpgradeMoveOut;
    protected Command cmdConvertMoveOut;
    protected Command cmdSplitMoveOut;
    private Command cmdTradeSendMoney;
    private Command cmdTradeMoveOut;
    private Command cmdTradeViewItemInfo;
    private Command cmdTradeSelectItem;
    private Command cmdTradeSelectInBag;
    private Command cmdTradeSelectInList;
    public Command cmdCloseAll;
    private Command cmdEliteShopBuy;
    private Command cmdEliteShopView;
    private Command cmdStoreBuy;
    private Command cmdStoreView;
    private Command cmdStoreLockBuy;
    private Command cmdStoreLockView;
    private Command cmdClanStoreBuy;
    private Command cmdClanStoreView;
    private Command cmdStoreFashionBuy;
    private Command cmdStoreFashionView;
    private Command cmdNonNamBuy;
    private Command cmdNonNamView;
    private Command cmdNonNuBuy;
    private Command cmdNonNuView;
    private Command cmdAoNamBuy;
    private Command cmdAoNamView;
    private Command cmdAoNuBuy;
    private Command cmdAoNuView;
    private Command cmdGangTayNamBuy;
    private Command cmdGangTayNamView;
    private Command cmdGangTayNuBuy;
    private Command cmdGangTayNuView;
    private static int pa;
    public static bool trans;
    public static int xstart;
    public static int ystart;
    public static int popupW;
    public static int popupH;
    public static int cmySK;
    public static int cmtoYSK;
    public static int cmdySK;
    public static int cmvySK;
    public static int cmyLimSK;
    public static int columns;
    public static int rows;
    private int totalRowInfo;
    private int ypaintKill;
    private int ylimUp;
    private int ylimDow;
    private int yPaint;
    private int[] color = new int[18]
    {
        0,
        0,
        0,
        0,
        600841,
        600841,
        667658,
        667658,
        3346944,
        3346688,
        4199680,
        5052928,
        3276851,
        3932211,
        4587571,
        5046280,
        6684682,
        3359744
    };
    private int[][] colorBorder = new int[5][]
    {
        new int[6]
        {
            18687,
            16869,
            15052,
            13235,
            11161,
            9344
        },
        new int[6]
        {
            45824,
            39168,
            32768,
            26112,
            19712,
            13056
        },
        new int[6]
        {
            16744192,
            15037184,
            13395456,
            11753728,
            10046464,
            8404992
        },
        new int[6]
        {
            13500671,
            12058853,
            10682572,
            9371827,
            7995545,
            6684800
        },
        new int[6]
        {
            16711705,
            15007767,
            13369364,
            11730962,
            10027023,
            8388621
        }
    };
    private int[] size = new int[6]
    {
        2,
        1,
        1,
        1,
        1,
        1
    };
    public static string Name_store;
    public static MyVector vecShop;
    public static int indexEff;
    public static EffectCharPaint effUpok;
    private int yhelp;
    public bool isPaintInfoHelpNotIphone;
    public static int inforX;
    public static int inforY;
    public static int inforW;
    public static int inforH;
    public Command cmdDead = new Command(mResources.DIES[0], 11038);
    protected Command cmdBagUseItem;
    protected Command cmdBagSortItem;
    private Command cmdItemInfoClose;
    protected Command cmdBagThrowItem;
    protected Command cmdBagSplitItem;
    private Command cmdBagViewItemInfo;
    private Command cmdBagSelectItem;
    public static MyVector ItemAutoUse = new MyVector();
    public static string svTitle;
    public static long lastimeAutouse = 0;
    public static string svAction;
    public static int INFO;
    public static int STORE;
    public static int ZONE;
    public static int UPGRADE;
    private int Hitem = 30;
    private int maxSizeRow = 5;
    private int isTranKyNang;
    private bool isTran;
    private int cmY_Old;
    private int cmX_Old;
    private int step;
    private Input2Dlg ipError;
    private string strErrCard = string.Empty;
    private long timeLastBuff;
    public static long lastAutoMove;
    public static int index;
    public static int[] fardistandsX;
    public static int[] fardistandsY;
    public static bool isItemNearMe;
    public static int pointCenterX;
    public static int pointCenterY;
    public static int rangeSearch;
    public short timeLucky_Draw;
    public short percentWin1Lucky_Draw;
    public short numPlayerLucky_Draw;
    public string timeC;
    public string totalMoneyLucky_Draw;
    public string percentWin2Lucky_Draw;
    public string winnerInfoLucky_Draw;
    public string myMoneyLucky_Draw;
    public long timeStart;
    public bool isRefresh;
    public static sbyte typeLucky_Draw;
    private mFont fnt = mFont.tahoma_7b_yellow;
    private sbyte countCaptcha;
    private sbyte[] arrowCaptcha = new sbyte[6]
    {
        -1,
        -1,
        -1,
        -1,
        -1,
        -1
    };
    public sbyte indexThanThu;
    public MyVector clan_thanthu = new MyVector();
    private int xThanThu;
    private int yThanThu;
    private int wThanThu;
    private int hThanThu;
    private int xExpThanThu;
    private int yExpThanThu;
    private int wExpThanThu;
    private int hExpThanThu;
    private int xSelectThanThu;
    private int ySelectThanThu;
    private int wSelectThanThu;
    private int xRecThanThu;
    private int yRecThanThu;
    private int wRecThanThu;
    private int hRecThanThu;
    private int countSendRequests;
    private string strTime = string.Empty;
    public bool hideInterface;
    private int[][] arrNgocKhamEXP = new int[11][]
    {
        new int[2],
        new int[2]
        {
            200,
            10
        },
        new int[2]
        {
            500,
            20
        },
        new int[2]
        {
            1000,
            50
        },
        new int[2]
        {
            2000,
            100
        },
        new int[2]
        {
            5000,
            200
        },
        new int[2]
        {
            10000,
            500
        },
        new int[2]
        {
            20000,
            1000
        },
        new int[2]
        {
            50000,
            2000
        },
        new int[2]
        {
            100000,
            5000
        },
        new int[2]
        {
            100000,
            10000
        }
    };
    private const int CMD_BIJUU = 500;
    private const sbyte INDEXMENU_BIJUU = 6;
    private const sbyte INDEXMENU_BIJUU_NOPOINT = 0;
    private const sbyte INDEXMENU_BIJUU_POINT = 1;
    private const sbyte INDEXMENU_BIJUU_SKILL = 2;
    private static int indexBijuuMenu;
    public GameScr()
    {
        if (GameCanvas.w == 128 || GameCanvas.h <= 208)
        {
            indexSize = 20;
        }

        initCommand();
        left = menu;
        right = cmdFocus;
    }

    static GameScr()
    {
        vMobSoul = new MyVector();
        vClan = new MyVector();
        vParty = new MyVector();
        vPtMap = new MyVector();
        vFriend = new MyVector();
        vList = new MyVector();
        vFriendWait = new MyVector();
        vEnemies = new MyVector();
        vCharInMap = new MyVector();
        vItemMap = new MyVector();
        vMobAttack = new MyVector();
        vSet = new MyVector();
        vMob = new MyVector();
        vNpc = new MyVector();
        vBuNhin = new MyVector();
        vLanterns = new MyVector();
        indexSize = 28;
        indexTitle = 0;
        indexSelect = 0;
        indexRow = -1;
        indexMenu = 0;
        indexCard = -1;
        scrInfo = new Scroll();
        scrMain = new Scroll();
        gamePad = new GamePad();
        vItemUpGrade = new MyVector();
        isViewClanMemOnline = false;
        isSortClanByPointWeek = false;
        isViewClanInvite = true;
        isMessageMenu = false;
        titleInputText = string.Empty;
        isPaintRankedList = false;
        isPaintAuctionSale = false;
        isPaintAlert = false;
        isPaintTask = false;
        isPaintTeam = false;
        isPaintFindTeam = false;
        isPaintFriend = false;
        isPaintList = false;
        isPaintEnemies = false;
        isPaintItemInfo = false;
        isPaintSelectSkill = false;
        isPaintInfoMe = false;
        isPaintStore = false;
        isPaintEliteShop = false;
        isPaintNonNam = false;
        isPaintNonNu = false;
        isPaintAoNam = false;
        isPaintAoNu = false;
        isPaintGangTayNam = false;
        isPaintGangTayNu = false;
        isPaintQuanNam = false;
        isPaintQuanNu = false;
        isPaintGiayNam = false;
        isPaintGiayNu = false;
        isPaintLien = false;
        isPaintNhan = false;
        isPaintNgocBoi = false;
        isPaintPhu = false;
        isPaintWeapon = false;
        isPaintStack = false;
        isPaintStackLock = false;
        isPaintGrocery = false;
        isPaintGroceryLock = false;
        isPaintUpGrade = false;
        isPaintConvert = false;
        isPaintUpGradeGold = false;
        isPaintUpPearl = false;
        isPaintBox = false;
        isPaintItemDell = false;
        isPaintSplit = false;
        isPaintCharInMap = false;
        isPaintTrade = false;
        isPaintZone = false;
        isPaintAuto = false;
        isPaintMessage = false;
        isPaintClan = false;
        isRequestMember = false;
        isPaintLuckySpin = false;
        isPaintAuctionBuy = false;
        isPaintLucky_Draw = false;
        isPaintGiaoDo = false;
        coinGotngoc = new int[11]
        {
            0,
            5000,
            40000,
            135000,
            320000,
            625000,
            1080000,
            1715000,
            2560000,
            3645000,
            5000000
        };
        typeViewInfo = 0;
        typeActive = 0;
        vItemTreeFront = new MyVector();
        vItemTreeBehind = new MyVector();
        vItemTreeBetwen = new MyVector();
        tgamePad = new GamePad();
        isTouchKey = true;
        isUseitemAuto = false;
        isAllmap = false;
        keySkill = new Skill[3];
        onScreenSkill = new Skill[5];
        count = 0;
        deltaY = 20;
        isManualFocus = false;
        pa = 0;
        trans = false;
        popupW = 140;
        popupH = 160;
        columns = 6;
        Name_store = "Shop Online";
        vecShop = new MyVector();
        indexEff = 0;
        svTitle = string.Empty;
        svAction = string.Empty;
        INFO = 0;
        STORE = 1;
        ZONE = 2;
        UPGRADE = 3;
        isItemNearMe = false;
        typeLucky_Draw = 0;
        indexBijuuMenu = 0;
        flyTextX = new int[5];
        flyTextY = new int[5];
        flyTextDx = new int[5];
        flyTextDy = new int[5];
        flyTextState = new int[5];
        flyTextString = new string[5];
        flyTextColor = new int[8];
        for (int i = 0; i < 5; i++)
        {
            flyTextState[i] = -1;
        }
    }

    public static long getMaxExp(int level)
    {
        long num = 0L;
        for (int i = 0; i <= level; i++)
        {
            num += exps[i];
        }

        return num;
    }

    private Member curMemberSelected()
    {
        return (Member)vClan.elementAt(indexRow);
    }

    public void createConfirm(string[] menu, Npc npc)
    {
        resetButton();
        isLockKey = true;
        left = new Command(menu[0], 130011, npc);
        right = new Command(menu[1], 130012, npc);
    }

    public static long[] getLevelExp(long exp)
    {
        long num = exp;
        int num2 = 0;
        for (num2 = 0; num2 < exps.Length && num >= exps[num2]; num2++)
        {
            num -= exps[num2];
        }

        return new long[2]
        {
            num2,
            num
        };
    }

    public static void setLevel_Exp(long exp, bool value)
    {
        long[] levelExp = getLevelExp(exp);
        if (value)
        {
            Char.getMyChar().clevel = (int)levelExp[0];
        }

        Char.getMyChar().cExpR = levelExp[1];
    }

    public static GameScr gI()
    {
        if (instance == null)
        {
            instance = new GameScr();
        }

        return instance;
    }

    public void refreshFindTeam()
    {
        if (!isPaintFindTeam)
        {
            return;
        }

        left = (center = null);
        left = new Command(mResources.MENU, 11045);
        if (vPtMap.size() > 0 && indexRow >= 0 && indexRow < vPtMap.size())
        {
            Party party = (Party)vPtMap.elementAt(indexRow);
            if (party != null && !Char.getMyChar().cName.Equals(party.name))
            {
                center = new Command(mResources.SELECT, 11046);
            }
        }
    }

    public void refreshTeam()
    {
        if (!isPaintTeam)
        {
            return;
        }

        left = (center = null);
        indexRow = 0;
        if (vParty.size() == 0)
        {
            center = null;
            left = new Command(mResources.MENU, 11047);
            return;
        }

        Party party = (Party)vParty.firstElement();
        if (party.charId == Char.getMyChar().charID)
        {
            left = new Command(mResources.TEAMLEADER, 11070, party);
        }
        else
        {
            left = new Command(mResources.OUT_TEAM, 11071);
        }
    }

    public override void switchToMe()
    {
        MainFuctions.time_ktg = mSystem.currentTimeMillis();
        Info.addInfo("Chào mừng bạn tới thế giới Ninja School Online", 80, mFont.tahoma_7b_yellow);
        TileMap.loadMusicBackground();
        Session_ME.isCancel = false;
        Session_ME.huyLock();
        base.switchToMe();
        Handle.SwitchToMe();
    }

    public void openUITrade()
    {
        try
        {
            resetButton();
            tradeItemName = string.Empty;
            typeTrade = 0;
            typeTrade = (typeTradeOrder = (coinTrade = (coinTradeOrder = 0)));
            isPaintTrade = true;
            arrItemTradeMe = new Item[12];
            arrItemTradeOrder = new Item[12];
            indexMenu = 0;
            int num = ((!Main.isPC) ? indexSize : 0);
            setPopupSize(175, 200 - num);
            right = cmdCloseAll;
        }
        catch (Exception ex)
        {
            Out.println(ex.ToString() + " loi tai ham openUITrade");
        }
    }

    public bool testAct()
    {
        for (sbyte b = 2; b < 9; b = (sbyte)(b + 2))
        {
            if (GameCanvas.keyHold[b])
            {
                return false;
            }
        }

        return true;
    }

    public bool checkExistFriend(string strName)
    {
        for (int i = 0; i < vFriend.size(); i++)
        {
            Friend friend = (Friend)vFriend.elementAt(i);
            if (friend.friendName.Equals(strName))
            {
                return false;
            }
        }

        return true;
    }

    // Offline client addition: double-click on a world target = Enter.
    private object lastClickTarget;
    private long lastClickTime;
    /// <summary>
    /// Called when a click focuses a world target. A second click on the same target within
    /// NsoGameControl.DoubleClickMs presses the fire key (keyPressedz[5], the Enter key),
    /// which updateKey handles in the same frame: attack monster/player, talk to NPC, pick item.
    /// </summary>
    private void onTargetClicked(object target)
    {
        long now = mSystem.getCurrentTimeMillis();
        int window = NsoGameControl.DoubleClickMs;
        if (window > 0 && target == lastClickTarget && now - lastClickTime <= window)
        {
            GameCanvas.keyPressedz[5] = true;
            lastClickTarget = null; // a third click starts a new pair
            return;
        }

        lastClickTarget = target;
        lastClickTime = now;
    }

    private void showCharList()
    {
        if (GameCanvas.isTouch && !Main.isPC)
        {
            if (GameCanvas.isPointerDown && !GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(xTG, yTG, 34, 34) && !isPaintCharInMap && GameCanvas.isPointerClick && GameCanvas.isHoldPress())
            {
                doShowCharInMap();
            }
        }
        else if (Main.isPC && GameCanvas.keyHold[13] && !isPaintCharInMap && GameCanvas.isHoldPress())
        {
            doShowCharInMap();
        }
    }

    private void checkAuto()
    {
        // Item-driven automation yields to menus, dialogs and chat; manual pickup is unchanged.
        if (isUseitemAuto && (GameCanvas.menu.showMenu || GameCanvas.currentDialog != null || ChatTextField.gI().isShow || isPaintPopup() || isPaintUI()))
            return;
        if (TileMap.mapID == 130 || isMobSameParty())
        {
            return;
        }

        long currentTimeMillis = mSystem.getCurrentTimeMillis();
        if ((GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[14] || GameCanvas.keyPressedz[15] || GameCanvas.keyPressedz[22] || GameCanvas.keyPressedz[23] || GameCanvas.keyPressedz[24]) && !ChatTextField.gI().isShow)
        {
            auto = 0;
            if (lockAutoMove)
            {
                Char.getMyChar().isLockMove = false;
                lockAutoMove = false;
            }
        }

        if (GameCanvas.keyPressedz[5] && !isPaintPopup())
        {
            if (auto == 0 && !isPaintPopup())
            {
                if (currentTimeMillis - lastFire < 800 && checkSkillValid2() && Char.getMyChar().mobFocus != null)
                {
                    auto = 10;
                    GameCanvas.keyPressedz[5] = false;
                }
            }
            else
            {
                if (!lockAutoMove && Char.getMyChar().statusMe != 14)
                {
                    lockAutoMove = !lockAutoMove;
                    Char.getMyChar().isLockMove = !Char.getMyChar().isLockMove;
                    lastFire = currentTimeMillis;
                    return;
                }

                auto = 0;
                if (lockAutoMove)
                {
                    Char.getMyChar().isLockMove = false;
                    lockAutoMove = false;
                }

                GameCanvas.keyPressedz[Key.NUM4] = (GameCanvas.keyPressedz[Key.NUM6] = false);
            }

            lastFire = currentTimeMillis;
        }

        if (GameCanvas.gameTick % 10 == 0 && auto > 0 && (Char.getMyChar().mobFocus != null || Char.getMyChar().itemFocus != null))
        {
            doFire(false);
        }

        if (auto > 1)
        {
            auto--;
        }
    }

    private bool isMobSameParty()
    {
        if (Char.getMyChar().mobFocus != null)
        {
            if ((Char.getMyChar().mobFocus.getTemplate().mobTemplateId == 142 && Char.getMyChar().cTypePk == Char.PK_PHE1) || (Char.getMyChar().mobFocus.getTemplate().mobTemplateId == 143 && Char.getMyChar().cTypePk == Char.PK_PHE2) || (Char.getMyChar().mobFocus.getTemplate().mobTemplateId == 143 && Char.getMyChar().cTypePk == Char.PK_PHE3))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    public void sortList(int type)
    {
        MyVector myVector = ((type != 0) ? vEnemies : vFriend);
        for (int i = 0; i < myVector.size() - 1; i++)
        {
            Friend friend = (Friend)myVector.elementAt(i);
            for (int j = i + 1; j < myVector.size(); j++)
            {
                Friend friend2 = (Friend)myVector.elementAt(j);
                if (friend2.type > friend.type)
                {
                    Friend friend3 = friend2;
                    friend2 = friend;
                    friend = friend3;
                    myVector.setElementAt(friend, i);
                    myVector.setElementAt(friend2, j);
                }
                else if (friend2.type == friend.type && friend.friendName.CompareTo(friend2.friendName) > 0)
                {
                    Friend friend4 = friend2;
                    friend2 = friend;
                    friend = friend4;
                    myVector.setElementAt(friend, i);
                    myVector.setElementAt(friend2, j);
                }
            }
        }
    }

    public void sortClan()
    {
        for (int i = 0; i < vClan.size() - 1; i++)
        {
            Member member = (Member)vClan.elementAt(i);
            for (int j = i + 1; j < vClan.size(); j++)
            {
                Member member2 = (Member)vClan.elementAt(j);
                if (isViewClanMemOnline && !isSortClanByPointWeek)
                {
                    if (member2.isOnline && !member.isOnline)
                    {
                        Member member3 = member2;
                        member2 = member;
                        member = member3;
                        vClan.setElementAt(member, i);
                        vClan.setElementAt(member2, j);
                    }
                    else
                    {
                        if (!member2.isOnline || !member.isOnline)
                        {
                            continue;
                        }

                        if (member2.type > member.type)
                        {
                            Member member4 = member2;
                            member2 = member;
                            member = member4;
                            vClan.setElementAt(member, i);
                            vClan.setElementAt(member2, j);
                        }
                        else
                        {
                            if (member2.type != member.type)
                            {
                                continue;
                            }

                            if (member2.pointClan > member.pointClan)
                            {
                                Member member5 = member2;
                                member2 = member;
                                member = member5;
                                vClan.setElementAt(member, i);
                                vClan.setElementAt(member2, j);
                            }
                            else
                            {
                                if (member.pointClan != member2.pointClan)
                                {
                                    continue;
                                }

                                if (member2.pointClanWeek > member.pointClanWeek)
                                {
                                    Member member6 = member2;
                                    member2 = member;
                                    member = member6;
                                    vClan.setElementAt(member, i);
                                    vClan.setElementAt(member2, j);
                                }
                                else if (member.pointClanWeek == member2.pointClanWeek)
                                {
                                    if (member2.level > member.level)
                                    {
                                        Member member7 = member2;
                                        member2 = member;
                                        member = member7;
                                        vClan.setElementAt(member, i);
                                        vClan.setElementAt(member2, j);
                                    }
                                    else if (member.level == member2.level && member.name.CompareTo(member2.name) > 0)
                                    {
                                        Member member8 = member2;
                                        member2 = member;
                                        member = member8;
                                        vClan.setElementAt(member, i);
                                        vClan.setElementAt(member2, j);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (isSortClanByPointWeek)
                {
                    if (isViewClanMemOnline)
                    {
                        if (member2.isOnline && !member.isOnline)
                        {
                            Member member9 = member2;
                            member2 = member;
                            member = member9;
                            vClan.setElementAt(member, i);
                            vClan.setElementAt(member2, j);
                        }
                        else
                        {
                            if (!member2.isOnline || !member.isOnline)
                            {
                                continue;
                            }

                            if (member2.pointClanWeek > member.pointClanWeek)
                            {
                                Member member10 = member2;
                                member2 = member;
                                member = member10;
                                vClan.setElementAt(member, i);
                                vClan.setElementAt(member2, j);
                            }
                            else
                            {
                                if (member.pointClanWeek != member2.pointClanWeek)
                                {
                                    continue;
                                }

                                if (member2.pointClan > member.pointClan)
                                {
                                    Member member11 = member2;
                                    member2 = member;
                                    member = member11;
                                    vClan.setElementAt(member, i);
                                    vClan.setElementAt(member2, j);
                                }
                                else if (member.pointClan == member2.pointClan)
                                {
                                    if (member2.type > member.type)
                                    {
                                        Member member12 = member2;
                                        member2 = member;
                                        member = member12;
                                        vClan.setElementAt(member, i);
                                        vClan.setElementAt(member2, j);
                                    }
                                    else if (member2.type == member.type && member.level == member2.level && member.name.CompareTo(member2.name) > 0)
                                    {
                                        Member member13 = member2;
                                        member2 = member;
                                        member = member13;
                                        vClan.setElementAt(member, i);
                                        vClan.setElementAt(member2, j);
                                    }
                                }
                            }
                        }
                    }
                    else if (member2.pointClanWeek > member.pointClanWeek)
                    {
                        Member member14 = member2;
                        member2 = member;
                        member = member14;
                        vClan.setElementAt(member, i);
                        vClan.setElementAt(member2, j);
                    }
                    else
                    {
                        if (member.pointClanWeek != member2.pointClanWeek)
                        {
                            continue;
                        }

                        if (member2.pointClan > member.pointClan)
                        {
                            Member member15 = member2;
                            member2 = member;
                            member = member15;
                            vClan.setElementAt(member, i);
                            vClan.setElementAt(member2, j);
                        }
                        else if (member.pointClan == member2.pointClan)
                        {
                            if (member2.type > member.type)
                            {
                                Member member16 = member2;
                                member2 = member;
                                member = member16;
                                vClan.setElementAt(member, i);
                                vClan.setElementAt(member2, j);
                            }
                            else if (member2.type == member.type && member.level == member2.level && member.name.CompareTo(member2.name) > 0)
                            {
                                Member member17 = member2;
                                member2 = member;
                                member = member17;
                                vClan.setElementAt(member, i);
                                vClan.setElementAt(member2, j);
                            }
                        }
                    }
                }
                else if (member2.type > member.type)
                {
                    Member member18 = member2;
                    member2 = member;
                    member = member18;
                    vClan.setElementAt(member, i);
                    vClan.setElementAt(member2, j);
                }
                else
                {
                    if (member2.type != member.type)
                    {
                        continue;
                    }

                    if (member2.pointClan > member.pointClan)
                    {
                        Member member19 = member2;
                        member2 = member;
                        member = member19;
                        vClan.setElementAt(member, i);
                        vClan.setElementAt(member2, j);
                    }
                    else
                    {
                        if (member.pointClan != member2.pointClan)
                        {
                            continue;
                        }

                        if (member2.pointClanWeek > member.pointClanWeek)
                        {
                            Member member20 = member2;
                            member2 = member;
                            member = member20;
                            vClan.setElementAt(member, i);
                            vClan.setElementAt(member2, j);
                        }
                        else if (member.pointClanWeek == member2.pointClanWeek)
                        {
                            if (member2.level > member.level)
                            {
                                Member member21 = member2;
                                member2 = member;
                                member = member21;
                                vClan.setElementAt(member, i);
                                vClan.setElementAt(member2, j);
                            }
                            else if (member.level == member2.level && member.name.CompareTo(member2.name) > 0)
                            {
                                Member member22 = member2;
                                member2 = member;
                                member = member22;
                                vClan.setElementAt(member, i);
                                vClan.setElementAt(member2, j);
                            }
                        }
                    }
                }
            }
        }
    }

    private void autoFocus()
    {
        if (cLastFocusID >= 0 && vCharInMap.size() > 0)
        {
            int indexChar = Char.getIndexChar(cLastFocusID);
            if (indexChar >= 0 && indexChar < vCharInMap.size())
            {
                Char @char = (Char)vCharInMap.elementAt(indexChar);
                if (@char != null && Char.isCharInScreen(@char) && !@char.isNhanbanz())
                {
                    Char.getMyChar().mobFocus = null;
                    Char.getMyChar().deFocusNPC();
                    Char.getMyChar().itemFocus = null;
                    Char.isManualFocus = true;
                    Char.getMyChar().charFocus = @char;
                }
            }
            else
            {
                cLastFocusID = -1;
                Char.getMyChar().charFocus = null;
            }
        }
        else
        {
            cLastFocusID = -1;
        }
    }

    public static long thoigian;
    public static void HutVP()
    {
        if (!HutVatPham)
        {
            return;
        }

        try
        {
            if (mSystem.currentTimeMillis() - thoigian > 1000)
            {
                for (int i = 0; i < vItemMap.size(); i++)
                {
                    ItemMap itemMap = (ItemMap)vItemMap.elementAt(i);
                    Service.gI().pickItem(itemMap.itemMapID);
                    thoigian = mSystem.currentTimeMillis();
                }
            }
        }
        catch (Exception)
        {
        }
    }

    public static void TuDanhNguoi()
    {
        try
        {
            MyVector myVector = new MyVector();
            myVector.addElement(Char.getMyChar().charFocus);
            Service.gI().sendPlayerAttack(new MyVector(), myVector, 2);
        }
        catch
        {
        }
    }

    public static void TuDanhQuai()
    {
        try
        {
            MyVector myVector = new MyVector();
            myVector.addElement(Char.getMyChar().mobFocus);
            Service.gI().sendPlayerAttack(myVector, new MyVector(), 1);
        }
        catch
        {
        }
    }

    public static long TimeAk;
    //public static bool isTudanh;
    public static void TuDanh()
    {
        Skill myskill = Char.getMyChar().myskill;
        if (mSystem.currentTimeMillis() - TimeAk > Char.getMyChar().myskill.coolDown + 100)
        {
            if ((double)Math.abs(Char.getMyChar().mobFocus.x - Char.getMyChar().cx) < (double)Char.getMyChar().myskill.dx * 1.5)
            {
                TuDanhQuai();
                myskill.Paint();
                TimeAk = mSystem.currentTimeMillis();
            }
            else if (Char.getMyChar().charFocus != null && Char.getMyChar().isMeCanAttackOtherPlayer(Char.getMyChar().charFocus) && (double)Math.abs(Char.getMyChar().charFocus.charID - Char.getMyChar().cx) < (double)Char.getMyChar().myskill.dx * 1.5)
            {
                TuDanhNguoi();
                myskill.Paint();
                TimeAk = mSystem.currentTimeMillis();
            }
        }
    }

    public static class CalendarHelper
    {
        public static DateTime GetCurrentTimeInTimeZone()
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
            return currentTime;
        }
    }

    /// <summary>Offline addition: row step of the auto panel in pixels (rows are drawn 30 px apart).</summary>
    private const int AutoRowStep = 30;
    /// <summary>Offline addition: number of rows paintUIAuto draws (keep in sync with its paintCheckBox calls).</summary>
    private const int AutoRowCount = 8;
    /// <summary>
    /// Offline addition: scrolls the auto panel by whole rows so the selected row is fully visible
    /// (replaces moveTo, which centred the row with the wrong item size).
    /// </summary>
    private void AutoScrollToRow(int row)
    {
        int visible = System.Math.Max(1, scrMain.height / AutoRowStep);
        int first = scrMain.cmtoY / AutoRowStep;
        if (row < first)
            first = row;
        else if (row >= first + visible)
            first = row - visible + 1;
        first = System.Math.Min(first, System.Math.Max(0, AutoRowCount - visible)); // never past the last row
        scrMain.cmtoY = System.Math.Max(0, first * AutoRowStep);
    }

    public bool isOpeningUI()
    {
        return false;
    }

    public bool isPcLevelSmall()
    {
        if (Main.isPC)
        {
            return true;
        }

        if (Char.getMyChar().ctaskId > 1)
        {
            return true;
        }

        return false;
    }

    public static void startFlyText(string flyString, int x, int y, int dx, int dy, int color)
    {
        int num = -1;
        for (int i = 0; i < 5; i++)
        {
            if (flyTextState[i] == -1)
            {
                num = i;
                break;
            }
        }

        if (num != -1)
        {
            flyTextColor[num] = color;
            flyTextString[num] = flyString;
            flyTextX[num] = x;
            flyTextY[num] = y;
            flyTextDx[num] = dx;
            flyTextDy[num] = dy;
            flyTextState[num] = 0;
            flyPaintX[num] = x;
            flyPaintY[num] = y; // reset recycled text slots
        }
    }

    public static void startLanterns(int x, int y)
    {
        vLanterns.addElement(new Lanterns(x, y));
    }

    public static bool startSplash(int x, int y, int dir)
    {
        int num = ((splashState[0] != -1) ? 1 : 0);
        if (splashState[num] != -1)
        {
            return false;
        }

        splashState[num] = 0;
        splashDir[num] = dir;
        splashX[num] = x;
        splashY[num] = y;
        splashPaintX[num] = x;
        splashPaintY[num] = y;
        return true;
    }

    private void setClanComand()
    {
        if (isPaintItemInfo)
        {
            return;
        }

        left = (center = null);
        if (indexMenu == 0)
        {
            if (Char.getMyChar().ctypeClan == 4)
            {
                left = new Command(mResources.CLANS[0], 14004);
            }

            if (Char.getMyChar().ctypeClan == 3)
            {
                left = new Command(mResources.CLANS[1], 14004);
            }

            if (Char.getMyChar().ctypeClan == 2)
            {
                left = new Command(mResources.CLANS[2], 14004);
            }

            if (!Char.getMyChar().cClanName.Equals(string.Empty))
            {
                if (indexTitle == 1)
                {
                    center = new Command(mResources.VIEW, 140101);
                }
                else
                {
                    center = new Command(mResources.CLAN_MENU[3], 14010);
                }
            }
        }
        else if (indexMenu == 1)
        {
            if (vClan.size() <= 0 || indexRow < 0 || indexRow >= vClan.size())
            {
                return;
            }

            Member member = curMemberSelected();
            if (member != null)
            {
                if (Char.getMyChar().ctypeClan == 4)
                {
                    left = new Command(mResources.CLANS[0], 14005);
                }

                if (Char.getMyChar().ctypeClan == 3)
                {
                    left = new Command(mResources.CLANS[1], 14005);
                }

                if (Char.getMyChar().ctypeClan != 4 && Char.getMyChar().ctypeClan != 3)
                {
                    left = new Command(mResources.CLANS[4], 14005);
                }

                if (!member.name.Equals(Char.getMyChar().cName) && (!isViewClanMemOnline || indexMember != 0))
                {
                    center = new Command(mResources.SELECT, 14006, member.name);
                }
            }
        }
        else if (indexMenu == 2 && indexTitle == 1 && Char.clan != null && Char.clan.items != null)
        {
            left = new Command(mResources.SELECT, 1508);
            if (!GameCanvas.isTouchControlLargeScreen)
            {
                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 1509);
            }
        }
    }

    /// <summary>Offline addition (user request): leaves a tab's items for the tab title row (all tabs), hiding the
    /// item info, so left/right switch tabs again (same state as pressing up from the first row).</summary>
    private void TrangBiBackToTitle()
    {
        indexTitle = 0;
        indexSelect = 0;
        isPaintItemInfo = false;
        left = (center = null);
        setLCR();
    }

    private bool InfoArrowsNavigateSelection
    {
        get
        {
            return indexMenu == 0 || indexMenu == 4 || indexMenu == 5 || indexMenu == 6;
        }
    }

    private static bool TryGridDirection(out TabGridNavigation.Direction direction)
    {
        direction = TabGridNavigation.Direction.Left;
        if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
            return true;
        direction = TabGridNavigation.Direction.Right;
        if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            return true;
        direction = TabGridNavigation.Direction.Up;
        if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            return true;
        direction = TabGridNavigation.Direction.Down;
        return GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25];
    }

    private void setCharFocusCommand()
    {
        if (indexRow >= 0 && vCharInMap.size() > 0)
        {
            if (Char.getIndexChar(cLastFocusID) == indexRow)
            {
                left = new Command(mResources.UNSELECT, 14002);
                return;
            }

            left = new Command(mResources.SELECT, 14003);
            center = new Command(string.Empty, 14003);
        }
        else
        {
            left = (center = null);
        }
    }

    private void setRankedListCommand()
    {
        if (vList.size() > 0 && indexRow >= 0 && indexRowMax > 0)
        {
            center = new Command(mResources.RANKED_MATCH, 14024);
            left = new Command(mResources.PLAYER_INFO, 14025);
        }
        else
        {
            center = null;
            left = null;
        }
    }

    private void setDunListCommand()
    {
        if (vList.size() > 0 && indexRow >= 0 && indexRowMax > 0)
        {
            center = new Command(mResources.SELECT, 14021);
        }
        else
        {
            center = null;
        }
    }

    private void setEnemiesCommand()
    {
        if (vEnemies.size() > 0 && indexRow >= 0 && indexRowMax > 0)
        {
            center = new Command(mResources.SELECT, 11078);
        }
        else
        {
            center = null;
        }
    }

    private void setFriendCommand()
    {
        if (vFriend.size() > 0 && indexRow >= 0 && indexRowMax > 0 && indexRow < vFriend.size())
        {
            Friend friend = (Friend)vFriend.elementAt(indexRow);
            center = new Command(mResources.SELECT, 11079);
        }
        else
        {
            center = null;
        }
    }

    private void setPartyCommand()
    {
        center = null;
        if (indexRow == -1)
        {
            return;
        }

        if (((Party)vParty.elementAt(0)).charId == Char.getMyChar().charID)
        {
            Party party = (Party)vParty.elementAt(indexRow);
            if (party.charId != Char.getMyChar().charID)
            {
                center = new Command(mResources.SELECT, 11080, party.name);
            }
        }
        else
        {
            Party party2 = (Party)vParty.elementAt(indexRow);
            if (party2.charId != Char.getMyChar().charID)
            {
                center = new Command(mResources.SELECT, 12009, party2.name);
            }
        }
    }

    public bool isOpenUI()
    {
        if (isPaintAuto || isPaintItemInfo || isPaintLuckySpin || isPaintInfoMe || isPaintStore || isPaintEliteShop || isPaintWeapon || isPaintNonNam || isPaintNonNu || isPaintAoNam || isPaintAoNu || isPaintGangTayNam || isPaintGangTayNu || isPaintQuanNam || isPaintQuanNu || isPaintGiayNam || isPaintGiayNu || isPaintLien || isPaintNhan || isPaintNgocBoi || isPaintPhu || isPaintStack || isPaintStackLock || isPaintGrocery || isPaintGroceryLock || isPaintUpGrade || isPaintAuctionBuy || isPaintAuctionSale || isPaintConvert || isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintUpPearl || isPaintLuyenThach || isPaintBox || isPaintTrade || isPaintLuyenNgoc || isPaintKhamNgoc || isPaintGiaoDo || isPaintGotNgoc || isPaintThaoNgoc || isPaintItemDell)
        {
            return true;
        }

        return false;
    }

    public static bool isSmallUI()
    {
        if (isPaintRankedList || (isPaintInfoMe && indexMenu == 6 && indexBijuuMenu != 0) || (isPaintInfoMe && indexMenu > 0 && indexMenu < 5) || isPaintAuto || isPaintZone || (isPaintClan && (indexMenu == 0 || indexMenu == 1 || indexMenu == 3 || indexMenu == 4)) || isPaintCharInMap || isPaintTeam || isPaintFindTeam || isPaintFriend || isPaintList || isPaintEnemies || isPaintTask || isPaintMessage || isPaintAlert || isPaintLucky_Draw)
        {
            return true;
        }

        return false;
    }

    /// <summary>Offline addition: true while a grid-style shop UI is shown (same list as updateKeyBuyItemUITouch).</summary>
    private bool IsGridShop
    {
        get
        {
            return isPaintEliteShop || isPaintStore || isPaintBox || isPaintGrocery || isPaintGroceryLock || isPaintStack || isPaintStackLock || isPaintNonNam || isPaintNonNu || isPaintAoNam || isPaintAoNu || isPaintGangTayNam || isPaintGangTayNu || isPaintQuanNam || isPaintQuanNu || isPaintGiayNam || isPaintGiayNu || isPaintWeapon || isPaintLien || isPaintNhan || isPaintNgocBoi || isPaintPhu || isPaintItemDell;
        }
    }

    private bool isCanLuyenThach()
    {
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        int num4 = 0;
        for (int i = 0; i < arrItemUpPeal.Length; i++)
        {
            Item item = arrItemUpPeal[i];
            if (item != null)
            {
                if (item.template.id == 455)
                {
                    num++;
                }
                else if (item.template.id == 456)
                {
                    num2++;
                }
                else if (item.template.type == 26)
                {
                    num3++;
                    num4 = item.template.id;
                }
            }
        }

        if (num >= 9 || num2 >= 9 || (num4 >= 10 && num >= 3 && num3 == 1) || (num4 >= 11 && num2 >= 3 && num3 == 1))
        {
            return true;
        }

        return false;
    }

    public static void setPopupSize(int w, int h)
    {
        popupW = w;
        popupH = h;
        popupX = gW2 - w / 2;
        popupY = gH2 - h / 2;
        if (GameCanvas.h <= 250)
        {
            popupY -= 10;
        }

        if (GameCanvas.isTouch && !isSmallUI() && !GameCanvas.isTouchControlSmallScreen && GameCanvas.currentScreen is GameScr)
        {
            popupW = 310;
            popupX = gW / 2 - popupW / 2;
        }

        if (popupY < -10)
        {
            popupY = -10;
        }

        if (GameCanvas.h > 208 && popupY < 0)
        {
            popupY = 0;
        }

        if (GameCanvas.h == 208 && popupY < 10)
        {
            popupY = 10;
        }

        if (GameCanvas.w == 128 || GameCanvas.h <= 208)
        {
            w = 126;
            h = 160;
        }
    }

    public static object objKhu;
    public static bool isLockKhu;
    public void upPearl()
    {
        int num = 0;
        int num2 = 0;
        bool flag = false;
        bool flag2 = false;
        for (int i = 0; i < arrItemUpPeal.Length; i++)
        {
            if (arrItemUpPeal[i] != null)
            {
                num++;
                num2 += crystals[arrItemUpPeal[i].template.id];
                if (arrItemUpPeal[i].template.id == 11)
                {
                    GameCanvas.msgdlg.setInfo(mResources.UP_MAX, null, new Command(mResources.CLOSE, 1), null);
                    GameCanvas.msgdlg.show();
                    return;
                }
            }

            if (arrItemUpPeal[i] != null && arrItemUpPeal[i].isLock)
            {
                flag = true;
            }

            if (arrItemUpPeal[i] != null && !arrItemUpPeal[i].isLock)
            {
                flag2 = true;
            }
        }

        if (num <= 1)
        {
            GameCanvas.msgdlg.setInfo(mResources.NOT_ENOUGH_UPPEARL, null, new Command(mResources.CLOSE, 1), null);
            GameCanvas.msgdlg.show();
            return;
        }

        int num3 = 0;
        num3 = crystals.Length - 1;
        while (num3 >= 0 && num2 <= crystals[num3])
        {
            num3--;
        }

        if (num3 >= crystals.Length - 1)
        {
            num3 = crystals.Length - 2;
        }

        if (isTypeXu)
        {
            if (coinUpCrystals[num3 + 1] > Char.getMyChar().xu)
            {
                GameCanvas.msgdlg.setInfo(mResources.NOT_ENOUGH_COIN_UPPEARL, null, new Command(mResources.CLOSE, 1), null);
                GameCanvas.msgdlg.show();
            }
            else if (flag)
            {
                GameCanvas.startYesNoDlg(mResources.CONFIRMUPPEARL, 88813, arrItemUpPeal, 8882, null);
            }
            else
            {
                Service.gI().crystalCollect(arrItemUpPeal);
            }
        }
        else if (coinUpCrystals[num3 + 1] > Char.getMyChar().xu + Char.getMyChar().yen)
        {
            GameCanvas.msgdlg.setInfo(mResources.NOT_ENOUGH_COIN_UPPEARL, null, new Command(mResources.CLOSE, 1), null);
            GameCanvas.msgdlg.show();
        }
        else if (flag2)
        {
            GameCanvas.startYesNoDlg(mResources.CONFIRMUPPEARL, 88814, arrItemUpPeal, 8882, null);
        }
        else
        {
            Service.gI().crystalCollectLock(arrItemUpPeal);
        }
    }

    public void showAlert(string title, string str, bool withMenuShow)
    {
        InfoDlg.hide();
        isPaintAlert = true;
        isLockKey = true;
        indexRow = 0;
        setPopupSize(200, (indexMenu == 4) ? 200 : 176);
        if (withMenuShow)
        {
            popupH -= 60;
        }

        right = new Command(mResources.CLOSE, 3);
        left = (center = null);
        textsTitle = title;
        texts = mFont.tahoma_7.splitFontVector(str, popupW - 30);
    }

    public void scrollDownAlert()
    {
        indexRowMax = texts.size();
        scrMain.setStyle(indexRowMax, 12, popupX, ystart + 12, popupW, popupH - 42 - ((textsTitle != null) ? 10 : 0), true, 1);
        indexRow = texts.size() - 1;
        scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
    }

    private void setInfoFrameForLargeScreen()
    {
        if (!GameCanvas.isTouchControlLargeScreen || (isPaintClan && indexMenu == 0))
        {
            return;
        }

        inforX = popupX + 175;
        inforW = popupW - 179;
        inforY = popupY + 33;
        inforH = 138;
        if (isPaintTrade && indexMenu == 0)
        {
            inforX = popupX + 6 + 3 * indexSize;
            inforW = popupW - (11 + 6 * indexSize);
        }

        if (isPaintInfoMe)
        {
            if (indexMenu == 4)
            {
                inforX = popupX + 33;
                inforY = popupY + 87;
                inforW = popupW - 67;
                inforH = 75;
            }
            else if (indexMenu == 5)
            {
                inforH = 161;
            }
        }
    }

    public void setLCR()
    {
        center = null;
        if (indexTitle == 0 && (indexMenu == 1 || indexMenu == 3 || indexMenu == 4))
        {
            left = null;
            return;
        }

        switch (indexMenu)
        {
            case 0:
            {
                if (indexTitle != 1)
                {
                    break;
                }

                Item item = getItemFocus(3);
                if (item != null)
                {
                    left = cmdBagSelectItem;
                    if ((GameCanvas.isTouch && GameCanvas.w < 320) || !GameCanvas.isTouch)
                    {
                        center = cmdBagViewItemInfo;
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                    left = cmdBagSortItem;
                }

                break;
            }

            case 1:
            {
                if (indexTitle != 1)
                {
                    break;
                }

                left = null;
                if (indexSelect < 0)
                {
                    break;
                }

                SkillTemplate skillTemplate3 = Char.getMyChar().nClass.skillTemplates[indexSelect];
                Skill skill3 = Char.getMyChar().getSkill(skillTemplate3);
                if (skill3 != null)
                {
                    if (skill3.point < skillTemplate3.maxPoint)
                    {
                        left = new Command(mResources.UPPOINT, 14001);
                    }

                    if (skill3.template.type == 1 || skill3.template.type == 4 || skill3.template.type == 2 || skill3.template.type == 3)
                    {
                        center = new Command(mResources.SHORCUT, 11081);
                    }
                    else
                    {
                        center = null;
                    }
                }

                break;
            }

            case 2:
                if (indexTitle >= 1)
                {
                    left = new Command(mResources.UPPOINT, 11084);
                    center = new Command(string.Empty, 11084);
                }

                break;
            case 3:
                left = null;
                center = new Command(mResources.CHANGE, 110854);
                break;
            case 4:
            {
                if (indexTitle != 1)
                {
                    break;
                }

                left = null;
                Item item2 = getItemFocus(5);
                if (item2 != null)
                {
                    if (currentCharViewInfo.charID == Char.getMyChar().charID)
                    {
                        left = new Command(mResources.SELECT, 11082);
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(5, item2);
                        }
                        else
                        {
                            center = new Command(mResources.VIEW, 11083);
                        }
                    }
                    else if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(5, item2);
                    }
                    else
                    {
                        center = new Command(mResources.VIEW, 11083);
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                    if (!GameCanvas.isTouch && indextabTrangbi > 0)
                    {
                        left = new Command(mResources.tbi1, 2003);
                    }
                }

                break;
            }

            case 5:
                left = null;
                if (indexTitle == 1 && indexSelect >= 0 && currentCharViewInfo.arrItemMounts[indexSelect] != null)
                {
                    if (Char.getMyChar().charID == currentCharViewInfo.charID)
                    {
                        left = new Command(mResources.MOVEOUT, 1516);
                    }

                    center = new Command((!GameCanvas.isTouchControlLargeScreen) ? mResources.VIEW : string.Empty, 1515);
                    // Offline change (user request, same as Trang bị): on large screens the focused item's
                    // info is shown automatically; Enter / Circle then opens the "Tháo ra" menu (see updateKey).
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(41, currentCharViewInfo.arrItemMounts[indexSelect]);
                        center = null;
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                }

                break;
            case 6:
            {
                if (indexBijuuMenu == 0)
                {
                    left = null;
                    if (indexTitle == 1 && indexSelect >= 0 && currentCharViewInfo.arrItemViThu[indexSelect] != null)
                    {
                        if (Char.getMyChar().charID == currentCharViewInfo.charID)
                        {
                            left = new Command(mResources.MOVEOUT, 508);
                        }

                        center = new Command((!GameCanvas.isTouchControlLargeScreen) ? mResources.VIEW : string.Empty, 503);
                        // Offline change (user request, same as Thú cưỡi): info shown automatically on large screens.
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(51, currentCharViewInfo.arrItemViThu[indexSelect]);
                            center = null;
                        }
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }

                    break;
                }

                if (indexBijuuMenu == 1)
                {
                    if (indexTitle >= 1)
                    {
                        left = new Command(mResources.UPPOINT, 504);
                    }

                    break;
                }

                if (indexTitle == 1)
                {
                    left = null;
                    if (indexSelect >= 0)
                    {
                        SkillTemplate skillTemplate = Char.getMyChar().nClass.skillTemplates[indexSelect];
                        Skill skill = Char.getMyChar().getSkill(skillTemplate);
                        if (skill != null)
                        {
                            if (skill.point < skillTemplate.maxPoint)
                            {
                                left = new Command(mResources.UPPOINT, 14001);
                            }

                            if (skill.template.type == 1 || skill.template.type == 4 || skill.template.type == 2 || skill.template.type == 3)
                            {
                                center = new Command(mResources.SHORCUT, 11081);
                            }
                            else
                            {
                                center = null;
                            }
                        }
                    }
                }

                if (indexTitle != 1)
                {
                    break;
                }

                left = null;
                if (indexSelect < 0)
                {
                    break;
                }

                SkillTemplate skillTemplate2 = Char.getMyChar().nClass.skillTemplates[indexSelect];
                Skill skill2 = Char.getMyChar().getSkill(skillTemplate2);
                if (skill2 != null)
                {
                    if (skill2.point < skillTemplate2.maxPoint)
                    {
                        left = new Command(mResources.UPPOINT, 505);
                    }

                    center = null;
                }

                break;
            }
        }

        if (indexMenu == 6)
        {
            right = cmdCloseAll;
        }
        else if (currentCharViewInfo.charID == Char.getMyChar().charID)
        {
            right = new Command(mResources.BACK, 11086);
        }
        else
        {
            right = cmdCloseAll;
        }
    }

    public void toimuonchet()
    {
        while (Char.getMyChar().cHP > 0)
        {
            MyVector myVector = new MyVector();
            myVector.addElement(Char.getMyChar());
            Service.gI().sendPlayerAttack(new MyVector(), myVector, -1);
            Thread.Sleep(1000);
        }
    }

    private void viewMemberInfo()
    {
        Party party = (Party)vParty.elementAt(indexRow);
        if (party.c != null && party.c != Char.getMyChar())
        {
            Service.gI().viewInfo(party.c.cName);
            isPaintTeam = false;
            gI().resetButton();
        }
    }

    private void Convert()
    {
        GameCanvas.endDlg();
        Service.gI().doConvertUpgrade(arrItemConvert[0].indexUI, arrItemConvert[1].indexUI, arrItemConvert[2].indexUI);
    }

    public void convert(int index)
    {
        if (arrItemConvert[index] == null)
        {
            arrItemConvert[index] = Char.getMyChar().arrItemBag[indexSelect];
            Char.getMyChar().arrItemBag[indexSelect] = null;
        }
        else
        {
            Item item = Char.getMyChar().arrItemBag[indexSelect];
            Char.getMyChar().arrItemBag[indexSelect] = null;
            Char.getMyChar().arrItemBag[arrItemConvert[index].indexUI] = arrItemConvert[index];
            arrItemConvert[index] = item;
        }

        left = (center = null);
        updateCommandForUI();
    }

    private void viewMemberClanInfo()
    {
        if (indexRow >= 0 && indexRow < vClan.size())
        {
            Member member = curMemberSelected();
            if (member != null && !member.name.Equals(string.Empty))
            {
                Service.gI().viewInfo(member.name);
                isPaintClan = false;
                gI().resetButton();
            }
        }
    }

    private bool isHaveVanBienLenh()
    {
        for (int i = 0; i < Char.getMyChar().arrItemBag.Length; i++)
        {
            Item item = Char.getMyChar().arrItemBag[i];
            if (item != null && item.template.id == 279 && item.template.level <= Char.getMyChar().clevel)
            {
                return true;
            }
        }

        return false;
    }

    public static void StartAutoUseThread(Item item)
    {
        new Thread(() => Handle.atcAutoUse(item)).Start();
    }

    public static bool checkAutouse(int var0)
    {
        return ItemAutoUse.contains(var0);
    }

    public static void addAutoUse(int var0)
    {
        if (!ItemAutoUse.contains(var0))
        {
            ItemAutoUse.addElement(var0);
        }
    }

    public void showViewInfo()
    {
        setLCR();
        indexMenu = 3;
        isPaintInfoMe = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
    }

    private static long nextItemAutoPickup;
    public static int ItemAutoPickupRadius = 120;
    public static int ItemAutoPickupIntervalMs = 350;
    public static int findnearMe(int[] list)
    {
        int num = 0;
        int num2 = 0;
        for (int i = 0; i < list.Length; i++)
        {
            if (i != num2 && num > list[i])
            {
                num = list[i];
            }
        }

        return num;
    }

    public static int findlocat(int[] list)
    {
        int num = 0;
        int num2 = 0;
        for (int i = 0; i < list.Length; i++)
        {
            if (i != num2 && num > list[i])
            {
                num = list[i];
                num2 = i;
            }
        }

        return num2;
    }

    public static string getDotNumber(long m)
    {
        string text = string.Empty + m;
        int num = text.Length / 3;
        if (text.Length % 3 == 0)
        {
            num--;
        }

        for (int i = 0; i < num; i++)
        {
            int num2 = text.Length - (i + 1) * 3 - i;
            text = text.Substring(0, num2) + "." + text.Substring(num2, text.Length);
        }

        return text;
    }

    public static string getTimeCountDown(long timeStart, int secondCount)
    {
        string empty = string.Empty;
        long num = (timeStart + secondCount * 1000 - mSystem.currentTimeMillis()) / 1000;
        if (num <= 0)
        {
            return string.Empty;
        }

        long num2 = num / 60;
        long num3 = num;
        if (num2 > 0)
        {
            if (num2 < 10)
            {
                if (num % 60 >= 0 && num % 60 < 10)
                {
                    return "0" + num2 + ":0" + num % 60;
                }

                return "0" + num2 + ":" + num % 60;
            }

            if (num % 60 >= 0 && num % 60 < 10)
            {
                return num2 + ":0" + num % 60;
            }

            return num2 + ":" + num % 60;
        }

        return (num3 >= 10) ? (num3 + "s") : ("0" + num3 + "s");
    }
}
