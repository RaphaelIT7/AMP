﻿namespace AMP.Data {
    public class Defines {

        public static string MOD_DEV_STATE    = "Alpha";
        public static string MOD_VERSION      = MOD_DEV_STATE + " 0.8.2";
        public static string MOD_SUFFIX       = "";
        public static string FULL_MOD_VERSION = MOD_VERSION + MOD_SUFFIX;
        public static string MOD_NAME         = "AMP " + FULL_MOD_VERSION;

        public const string AMP           = "AMP";
        public const string SERVER        = "Server";
        public const string CLIENT        = "Client";
        public const string WEB_INTERFACE = "Web";
        public const string DISCORD_SDK   = "DiscordSDK";

        public const uint   STEAM_APPID          = 629730;
        public const uint   STEAM_APPID_SPACEWAR = 480;

        public const int    MAX_PLAYERS   = 10;

    }
}
