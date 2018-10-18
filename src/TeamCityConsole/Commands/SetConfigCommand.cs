using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TeamCityConsole.Options;

namespace TeamCityConsole.Commands
{
    public class SetConfigCommand : ICommand
    {
        private readonly ISettings _settings;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public SetConfigCommand(ISettings settings)
        {
            _settings = settings;
        }

        public async Task Execute(object options)
        {
            var configOptions = options as SetConfigOptions;
            if (configOptions == null) throw new ArgumentNullException("configOptions");

            if (configOptions.UserName != null)
            {
                _settings.TeamCityUsername = configOptions.UserName;
            }

            if (configOptions.Password!= null)
            {
                _settings.TeamCityPassword = configOptions.Password;
            }

            if (configOptions.TeamCityUri!= null)
            {
                _settings.TeamCityUri= configOptions.TeamCityUri;
            }

            if (configOptions.SelfUpdateBuildConfigId != null)
            {
                _settings.SelfUpdateBuildConfigId = configOptions.SelfUpdateBuildConfigId;
            }

            _settings.Save();

            await Task.FromResult(0); //just to hide build warning
        }
    }
}
