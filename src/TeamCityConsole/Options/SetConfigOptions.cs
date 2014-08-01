using CommandLine;

namespace TeamCityConsole.Options
{
    [Verb(Verbs.SetConfig, HelpText="Update the authentication information in the configuration file.")]
    public class SetConfigOptions
    {
        [Option('t', "teamCityUri", Required = false, HelpText="The Uri to connect to Team City")]
        public string TeamCityUri { get; set; }

        [Option('u', "userName", Required = false,HelpText = "User name for connecting to team city.")]
        public string UserName { get; set; }

        [Option('p', "password", Required = false,HelpText = "Password for connecting to team city.")]
        public string Password { get; set; }

        [Option('s', "selfUpdateConfig", Required = false, HelpText = "Self update bulid configuration Id.")]
        public string SelfUpdateBuildConfigId { get; set; }

    }

}
