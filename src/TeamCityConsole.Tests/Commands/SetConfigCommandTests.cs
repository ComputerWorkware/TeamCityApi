using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;
using TeamCityConsole.Tests.Helpers;
using Xunit;
using Xunit.Extensions;

namespace TeamCityConsole.Tests.Commands
{
    public class SetConfigCommandTests
    {
        public class Execute
        {

            [Theory]
            [AutoNSubstituteData]
            public void should_save_settings(TestSetConfigCommand setConfigCommand, SetConfigOptions setConfigOptions)
            {
                setConfigCommand.Execute(setConfigOptions).Wait();

                Assert.Equal(setConfigCommand.Settings.TeamCityUsername, setConfigOptions.UserName);
                Assert.Equal(setConfigCommand.Settings.TeamCityPassword, setConfigOptions.Password);
                Assert.Equal(setConfigCommand.Settings.TeamCityUri, setConfigOptions.TeamCityUri);
                Assert.Equal(setConfigCommand.Settings.SelfUpdateBuildConfigId, setConfigOptions.SelfUpdateBuildConfigId);

                setConfigCommand.Settings.Received().Save();
            }

            [Theory]
            [AutoNSubstituteData]
            public void null_options_should_not_update_settings(TestSetConfigCommand setConfigCommand, SetConfigOptions setConfigOptions)
            {
                setConfigOptions.UserName = null;
                setConfigCommand.Execute(setConfigOptions).Wait();

                Assert.NotNull(setConfigCommand.Settings.TeamCityUsername);

            }


        }

        public class TestSetConfigCommand : SetConfigCommand
        {
            public ISettings Settings { get; private set; }

            public TestSetConfigCommand(ISettings settings) : base(settings)
            {
                Settings = settings;
            }
        }
    }
}
