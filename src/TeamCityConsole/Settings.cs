using System.Configuration;
using System.Reflection;

namespace TeamCityConsole
{
    public interface ISettings
    {
        string TeamCityUri { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string SelfUpdateBuildConfigId { get; set; }
        void Save();
        void Load();
    }

    public class Settings : ISettings
    {
        public string TeamCityUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SelfUpdateBuildConfigId { get; set; }

        public void Save()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string configFilename = executingAssembly.Location + ".config";

            var configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFilename;
            System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

            config.AppSettings.Settings["username"].Value = Username;
            config.AppSettings.Settings["password"].Value = Password;
            config.AppSettings.Settings["teamcityuri"].Value = TeamCityUri;
            config.AppSettings.Settings["selfUpdateBuildConfigId"].Value = SelfUpdateBuildConfigId;

            config.Save();
        }

        public void Load()
        {
            TeamCityUri = ConfigurationManager.AppSettings["teamcityuri"];
            Username = ConfigurationManager.AppSettings["username"];
            Password = ConfigurationManager.AppSettings["password"];
            SelfUpdateBuildConfigId = ConfigurationManager.AppSettings["selfUpdateBuildConfigId"];
        }
    }
}