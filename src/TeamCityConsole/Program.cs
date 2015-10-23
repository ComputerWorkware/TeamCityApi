using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Funq;
using NLog;
using TeamCityApi;
using TeamCityApi.UseCases;
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
            ExtractResources();

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

            ExecuteAsync(command, options).GetAwaiter().GetResult();
        }

        private const string ExtractPrefix = ".extract.";
        private static void ExtractResources()
        {
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var assem = Assembly.GetExecutingAssembly();

            string resourceName = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.Contains(ExtractPrefix));

            int extractPosition = resourceName.IndexOf(ExtractPrefix, StringComparison.Ordinal);

            var dllName = resourceName.Substring(extractPosition+ExtractPrefix.Length);
            
            string assemblyFullName = Path.Combine(assemblyLocation,dllName);

            if (File.Exists(assemblyFullName))
                return;

            if (resourceName == null) return; 

            using (var stream = assem.GetManifestResourceStream(resourceName))
            {
                Byte[] assemblyData = new Byte[stream.Length];

                stream.Read(assemblyData, 0, assemblyData.Length);

                File.WriteAllBytes(assemblyFullName,assemblyData);

            }

        }

        private static async Task ExecuteAsync(ICommand command, dynamic options)
        {
            try
            {
                await AsyncStackTraceExtensions.Log(command.Execute(options));
            }
            catch (AggregateException e)
            {
                foreach (Exception innerException in e.Flatten().InnerExceptions)
                {
                    Log.Fatal(innerException);
                }
            }
            catch (Exception e)
            {
                string message = e.Message + Environment.NewLine + e.StackTraceEx();
                Log.Fatal(message);
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

            container.Register<IFileDownloader>(x => new FileDownloader(x.Resolve<IHttpClientWrapper>(), x.Resolve<IFileSystem>()));

            container.Register<IDownloadDataFlow>(x => new DownloadDataFlow(x.Resolve<IFileDownloader>()));

            container.Register<IFileSystem>(new FileSystem());

            container.Register<ICommand>(Verbs.GetDependencies,
                x => new ResolveDependencyCommand(x.Resolve<ITeamCityClient>(), x.Resolve<IFileSystem>(), x.Resolve<IDownloadDataFlow>()));

            container.Register<ICommand>(Verbs.GetArtifacts, x => new DownloadArtifactCommand(x.Resolve<IFileSystem>()));

            container.Register<ICommand>(Verbs.SelfUpdate,
                x => new UpdateCommand(x.Resolve<ITeamCityClient>(), x.Resolve<IFileSystem>(), x.Resolve<IFileDownloader>(), assemblyMetada, settings));

            container.Register<ICommand>(Verbs.SetConfig, x => new SetConfigCommand(settings));

            container.Register(x => new CloneRootBuildConfigUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new CloneChildBuildConfigUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new DeleteClonedBuildChainUseCase(x.Resolve<ITeamCityClient>()));

            container.Register<ICommand>(Verbs.CloneRootBuildConfig, x => new CloneRootBuildConfigCommand(x.Resolve<CloneRootBuildConfigUseCase>()));

            container.Register<ICommand>(Verbs.CloneChildBuildConfig, x => new CloneChildBuildConfigCommand(x.Resolve<CloneChildBuildConfigUseCase>()));

            container.Register<ICommand>(Verbs.DeleteClonedBuildChain, x => new DeleteClonedBuildChainCommand(x.Resolve<DeleteClonedBuildChainUseCase>()));

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
