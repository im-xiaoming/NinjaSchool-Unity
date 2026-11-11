namespace NsoServer.Net
{
    // Subset of com.nsoz.constants.CMD ported as needed per phase. Values are identical to the Java server.
    public static class Cmd
    {
        // top-level
        public const sbyte PLAYER_DIE = 0;
        public const sbyte NPC_ATTACK_PLAYER = -2;
        public const sbyte NPC_ATTACK_ME = -3;
        public const sbyte ME_DIE = -11;
        public const sbyte ME_LIVE = -10;
        public const sbyte ME_BACK = -9;
        public const sbyte ME_DIE_EXP_DOWN = 72;
        public const sbyte RETURN_POINT_MAP = 88;
        public const sbyte ME_LOAD_INFO = -123;
        public const sbyte PLAYER_MOVE = 1;
        public const sbyte ZONE_CHANGE = 28;
        public const sbyte OPEN_UI_ZONE = 36;
        public const sbyte RESET_POINT = 52;
        public const sbyte MAP_CHANGE = -17;
        public const sbyte MAP_CLEAR = -16;
        public const sbyte OPEN_UI_BOX = 31;
        public const sbyte ALERT_MESSAGE = 53;
        public const sbyte MAP_ITEM = 117;
        public const sbyte MAP_INFO = -18;
        public const sbyte FULL_SIZE = -32;
        public const sbyte SUB_COMMAND = -30;
        public const sbyte NOT_LOGIN = -29;
        public const sbyte NOT_MAP = -28;
        public const sbyte GET_SESSION_ID = -27;
        public const sbyte SERVER_DIALOG = -26;

        // under NOT_LOGIN
        public const sbyte LOGIN = -127;
        public const sbyte CLIENT_INFO = -125;

        // under SUB_COMMAND
        public const sbyte ME_LOAD_ALL = -127;
        public const sbyte ME_LOAD_THU_NUOI = -69;
        public const sbyte LOAD_RMS = -65;

        // under NOT_MAP
        public const sbyte ME_LOAD_ACTIVE = -106;
        public const sbyte SELECT_PLAYER = -126;
        public const sbyte CREATE_PLAYER = -125;
        public const sbyte UPDATE_VERSION = -123;
        public const sbyte UPDATE_DATA = -122;
        public const sbyte UPDATE_MAP = -121;
        public const sbyte UPDATE_SKILL = -120;
        public const sbyte UPDATE_ITEM = -119;
        public const sbyte CLIENT_OK = -101;
    }
}
