using System.Configuration;

namespace TeamCityConsole
{
    public class Settings
    {
        public static string TeamCityUri
        {
            get { return ConfigurationManager.AppSettings["teamcityuri"]; }
        }

        public static string Username
        {
            get { return ConfigurationManager.AppSettings["username"]; }
        }

        public static string Password
        {
            get { return ConfigurationManager.AppSettings["password"]; }
        }
    }
}