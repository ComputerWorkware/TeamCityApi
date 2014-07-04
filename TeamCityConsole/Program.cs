﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using TeamCityConsole.Commands;
using TeamCityConsole.Options;

namespace TeamCityConsole
{
    class Program
    {
        private static readonly Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>
        {
            {Verbs.GetArtifacts, new DownloadArtifactCommand()},
            {Verbs.GetDependencies, new ResolveDependencyCommand()},
        };

        static void Main(string[] args)
        {
            ParserResult<object> result = Parser.Default.ParseArguments(args, typeof(GetArtifactOptions), typeof(GetDependenciesOptions));

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

            ICommand command = Commands[GetVerb(options)];

            Task displayTask = Task.Run(async () =>
            {
                Console.Out.WriteLine("Downloading");
                while (true)
                {
                    Console.Out.Write(".");
                    await Task.Delay(400);
                }
            });

            Task downloadTask = command.Execute(options);

            try
            {
                Task.WaitAny(displayTask, downloadTask);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            
        }

        private static string GetVerb(object options)
        {
            return options.GetType().GetCustomAttribute<VerbAttribute>().Name;
        }
    }
}