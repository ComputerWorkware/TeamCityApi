using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Funq;
using NLog;
using TeamCityApi;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;

namespace TeamCityConsole
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            var container = SetupContainer();

            DisplayAssemblyInfo();

            Type[] optionTypes = GetOptionsInThisAssembly();

            ParserResult<object> result = Parser.Default.ParseArguments(args, optionTypes);

            if (result.Errors.OfType<HelpRequestedError>().Any())
            {
                return;
            }

            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                {
                    Console.Out.WriteLine(error);
                }
                return;
            }

            dynamic options = result.Value;

            dynamic verb = GetVerb(options);

            ICommand command = container.ResolveNamed<ICommand>(verb);
#if !DEBUG
            if (verb != Verbs.SelfUpdate)
            {
                ICommand updateCommand = container.ResolveNamed<ICommand>(Verbs.SelfUpdate);
                updateCommand.Execute(null).Wait();
            }
#endif

            try
            {
                Task task = command.Execute(options);
                task.Wait();
            }
            catch (AggregateException e)
            {
                foreach (Exception innerException in e.Flatten().InnerExceptions)
                {
                    Log.Fatal(innerException);
                }
            }
        }

        private static string GetVerb(object options)
        {
            return options.GetType().GetCustomAttribute<VerbAttribute>().Name;
        }

        private static void DisplayAssemblyInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            Console.Out.WriteLine("Product: {0}", fvi.ProductName);
            Console.Out.WriteLine("Company: {0}", fvi.CompanyName);
            Console.Out.WriteLine("Assembly version: {0}", assembly.GetName().Version);
            Console.Out.WriteLine("File version: {0}", fvi.FileVersion);
        }

        private static Container SetupContainer()
        {
            var container = new Container();

            Settings settings = new Settings();
            settings.Load();

            AssemblyMetada assemblyMetada = new AssemblyMetada();

            container.Register<IHttpClientWrapper>(new HttpClientWrapper(settings.TeamCityUri, settings.Username,
                settings.Password));

            container.Register<ITeamCityClient>(x => new TeamCityClient(x.Resolve<IHttpClientWrapper>()));

            container.Register<IFileDownloader>(x => new FileDownloader(x.Resolve<IHttpClientWrapper>()));

            container.Register<IDownloadDataFlow>(x => new DownloadDataFlow(x.Resolve<IFileDownloader>()));

            container.Register<IFileSystem>(new FileSystem());

            container.Register<ICommand>(Verbs.GetDependencies,
                x => new ResolveDependencyCommand(x.Resolve<ITeamCityClient>(), x.Resolve<IFileSystem>(), x.Resolve<IDownloadDataFlow>()));

            container.Register<ICommand>(Verbs.GetArtifacts, x => new DownloadArtifactCommand(x.Resolve<IFileSystem>()));

            container.Register<ICommand>(Verbs.SelfUpdate,
                x => new UpdateCommand(x.Resolve<ITeamCityClient>(), x.Resolve<IFileSystem>(), x.Resolve<IFileDownloader>(), assemblyMetada, settings));

            container.Register<ICommand>(Verbs.SetConfig,
                x => new SetConfigCommand(settings));

            return container;
        }

        private static Type[] GetOptionsInThisAssembly()
        {
            IEnumerable<Type> types = from x in typeof(Program).Assembly.GetTypes()
                where x.GetCustomAttribute<VerbAttribute>() != null
                select x;

            return types.ToArray();
        }
    }
}
