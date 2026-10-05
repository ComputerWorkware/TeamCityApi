using System.Configuration;
using System.Reflection;

namespace TeamCityConsole
{
    public interface ISettings
    {
        string TeamCityUri { get; set; }
        string TeamCityUsername { get; set; }
        string TeamCityPassword { get; set; }
        string SelfUpdateBuildConfigId { get; set; }
        string GitLabUri { get; set; }
        string GitLabToken { get; set; }
        void Save();
        void Load();
    }

    public class Settings : ISettings
    {
        public string TeamCityUri { get; set; }
        public string TeamCityUsername { get; set; }
        public string TeamCityPassword { get; set; }
        public string GitLabUri { get; set; }
        public string GitLabToken { get; set; }
        public string SelfUpdateBuildConfigId { get; set; }

        public void Save()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string configFilename = executingAssembly.Location + ".config";

            var configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFilename;
            System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

            config.AppSettings.Settings["teamcityusername"].Value = TeamCityUsername;
            config.AppSettings.Settings["teamcitypassword"].Value = TeamCityPassword;
            config.AppSettings.Settings["teamcityuri"].Value = TeamCityUri;
            config.AppSettings.Settings["gitlaburi"].Value = GitLabUri;
            // Existing deployed configs won't have this key yet.
            config.AppSettings.Settings.Remove("gitlabtoken");
            config.AppSettings.Settings.Add("gitlabtoken", GitLabToken);
            config.AppSettings.Settings["selfUpdateBuildConfigId"].Value = SelfUpdateBuildConfigId;

            config.Save();
        }

        public void Load()
        {
            TeamCityUri = ConfigurationManager.AppSettings["teamcityuri"];
            TeamCityUsername = ConfigurationManager.AppSettings["teamcityusername"];
            TeamCityPassword = ConfigurationManager.AppSettings["teamcitypassword"];
            GitLabUri = ConfigurationManager.AppSettings["gitlaburi"];
            GitLabToken = ConfigurationManager.AppSettings["gitlabtoken"];
            SelfUpdateBuildConfigId = ConfigurationManager.AppSettings["selfUpdateBuildConfigId"];
        }
    }
}