using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Funq;
using NLog;
using TeamCityApi;
using TeamCityApi.Clients;
using TeamCityApi.Helpers;
using TeamCityApi.Helpers.Git;
using TeamCityApi.UseCases;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;
using TeamCityConsole.Utils;
using File = System.IO.File;


namespace TeamCityConsole
{
    class Program
    {
        private static Logger Log;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            Log = LogManager.GetCurrentClassLogger();

            ExtractResources();

            var container = SetupContainer();
            DisplayAssemblyInfo();
            DisplayExecutedCommand(args);
            
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

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Log.Info("Exiting TeamCityConsole Process");
        }

        private static void DisplayExecutedCommand(string[] args)
        {
            Console.Out.WriteLine(Assembly.GetExecutingAssembly().Location + " " + String.Join(" ", args));
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

            container.Register<IHttpClientWrapper>(new HttpClientWrapper(settings.TeamCityUri, settings.TeamCityUsername,
                settings.TeamCityPassword));

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

            container.Register<IBuildConfigXmlClient>(x => new BuildConfigXmlClient(x.Resolve<ITeamCityClient>(), x.Resolve<IGitRepositoryFactory>()));

            container.Register(x => new CloneRootBuildConfigUseCase(x.Resolve<ITeamCityClient>(), x.Resolve<IBuildConfigXmlClient>(), x.Resolve<IVcsRootHelper>()));

            container.Register(x => new CloneChildBuildConfigUseCase(x.Resolve<ITeamCityClient>(), x.Resolve<IVcsRootHelper>(), x.Resolve<IBuildConfigXmlClient>()));
            
            container.Register(x => new DeepCloneBuildConfigUseCase(x.Resolve<ITeamCityClient>(), x.Resolve<IVcsRootHelper>(), x.Resolve<IBuildConfigXmlClient>()));

            container.Register(x => new DeleteClonedBuildChainUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new DeleteGitBranchesInBuildChainUseCase(x.Resolve<ITeamCityClient>(), x.Resolve<IGitLabClientFactory>()));

            container.Register(x => new ShowBuildChainUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new CompareBuildsUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new PropagateVersionUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new ShowVersionsUseCase(x.Resolve<ITeamCityClient>()));

            container.Register(x => new GenerateEscrowUseCase(x.Resolve<ITeamCityClient>()));

            container.Register<ICommand>(Verbs.CloneRootBuildConfig, x => new CloneRootBuildConfigCommand(x.Resolve<CloneRootBuildConfigUseCase>()));

            container.Register<ICommand>(Verbs.CloneChildBuildConfig, x => new CloneChildBuildConfigCommand(x.Resolve<CloneChildBuildConfigUseCase>()));

            container.Register<ICommand>(Verbs.DeepCloneBuildConfig, x => new DeepCloneBuildConfigCommand(x.Resolve<DeepCloneBuildConfigUseCase>()));

            container.Register<ICommand>(Verbs.DeleteClonedBuildChain, x => new DeleteClonedBuildChainCommand(x.Resolve<DeleteClonedBuildChainUseCase>()));

            container.Register<ICommand>(Verbs.DeleteGitBranchesInBuildChain, x => new DeleteGitBranchInBuildChainCommand(x.Resolve<DeleteGitBranchesInBuildChainUseCase>()));

            container.Register<ICommand>(Verbs.ShowBuildChain, x => new ShowBuildChainCommand(x.Resolve<ShowBuildChainUseCase>()));

            container.Register<ICommand>(Verbs.CompareBuilds, x => new CompareBuildsCommand(x.Resolve<CompareBuildsUseCase>()));

            container.Register<ICommand>(Verbs.PropagateVersion, x => new PropagateVersionCommand(x.Resolve<PropagateVersionUseCase>()));

            container.Register<ICommand>(Verbs.ShowVersions, x => new ShowVersionsCommand(x.Resolve<ShowVersionsUseCase>()));

            container.Register<ICommand>(Verbs.GenerateEscrow, x => new GenerateEscrowCommand(x.Resolve<ITeamCityClient>(),
                x.Resolve<GenerateEscrowUseCase>(), 
                x.ResolveNamed<ICommand>(Verbs.GetArtifacts), 
                x.Resolve<IFileSystem>(),
                x.Resolve<IFileDownloader>() ));

            container.Register<List<GitCredential>>(x=>new List<GitCredential>
            {
                new GitCredential
                {
                    HostName = "*",
                    UserName = settings.TeamCityUsername,
                    Password = settings.TeamCityPassword
                }
            });

            container.Register<GitLabSettings>(x=> new GitLabSettings()
                {
                    GitLabUri = settings.GitLabUri,
                    GitLabUsername = settings.GitLabUsername,
                    GitLabPassword = settings.GitLabPassword
                }
            );

            container.Register<IGitRepositoryFactory>(x => new GitRepositoryFactory(x.Resolve<List<GitCredential>>()));
            container.Register<IGitLabClientFactory>(x => new GitLabClientFactory(x.Resolve<GitLabSettings>()));
            container.Register<IVcsRootHelper>(x => new VcsRootHelper(x.Resolve<ITeamCityClient>(), x.Resolve<IGitRepositoryFactory>(), x.Resolve<IGitLabClientFactory>()));

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
