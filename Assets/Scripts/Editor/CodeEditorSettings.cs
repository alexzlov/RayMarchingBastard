namespace CodeEdit
{
    public static class EditorColor
    {
        public static string Background  = Solarized.Base03;
        public static string Color       = "#ffffff";
        public static string Type        = Solarized.Yellow;
        public static string Keyword     = Solarized.Green;
        public static string Symbol      = Solarized.Base1;
        public static string Digit       = Solarized.Violet;
        public static string String      = Solarized.Violet;
        public static string Comment     = Solarized.Base01;
        public static string CgProgram   = Solarized.Blue;
        public static string Raymarching = Solarized.Cyan;
        public static string Entrypoint  = Solarized.Orange;
        public static string Unity       = Solarized.Magenta;
    }

    public static class EditorSettings
    {
        public static string Font      = "Fonts/mononoki-Regular";
        public static int    FontSize  = 12;
        public static bool   WordWrap  = false;
        public static int    MinHeight = 200;
    }
}