using System.Configuration;

namespace TeamCityConsole
{
    public class Settings
    {
        public string TeamCityUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SelfUpdateBuildConfigId { get; set; }

        public static Settings CreateFromConfig()
        {
            return new Settings
            {
                TeamCityUri = ConfigurationManager.AppSettings["teamcityuri"],
                Username = ConfigurationManager.AppSettings["username"],
                Password = ConfigurationManager.AppSettings["password"],
                SelfUpdateBuildConfigId = ConfigurationManager.AppSettings["selfUpdateBuildConfigId"],
            };
        }
    }
}