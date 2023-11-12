using WiimoteGun;

namespace WiimoteLib
{
    class Log
    {
        public static void Info(string s)
        {
            SimpleLogger.Instance.Info(s);
        }

        public static void Debug(string s)
        {
            SimpleLogger.Instance.Debug(s);
        }

        public static void Warning(string s)
        {
            SimpleLogger.Instance.Warning(s);
        }

        public static void Error(string s)
        {
            SimpleLogger.Instance.Error(s);
        }

        public static void Error(System.Exception ex)
        {
            SimpleLogger.Instance.Error(ex.Message);
        }
    }
}
