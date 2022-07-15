namespace StatsReporter
{
    public static class Strings
    {
        public const string BotName = "Pokemon Statistics Reporter";
        public static readonly string BotVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public const string BasePath = "../bin/";

        public const string ConfigFileName = "config.json";
        public const string ConfigFilePath = BasePath + ConfigFileName;

        public const string StaticFolder = "static";
        public static readonly string LocaleFolder = StaticFolder + Path.DirectorySeparatorChar + "locales";
        public static readonly string DataFolder = BasePath + StaticFolder + Path.DirectorySeparatorChar + "data";
    }
}