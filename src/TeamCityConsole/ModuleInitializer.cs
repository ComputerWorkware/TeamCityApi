// This file can be modified in any way, with two exceptions. 1) The name of
// this class must be "ModuleInitializer". 2) This class must have a public or
// internal parameterless "Run" method that returns void. In addition to these
// modifications, this file may also be moved to any location, as long as it
// remains a part of its current project.

using System;
using System.Linq;
using System.Reflection;

namespace TeamCityConsole
{
    internal static class ModuleInitializer
    {
        internal static void Run()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;
        }

        private static Assembly ResolveEventHandler(Object sender, ResolveEventArgs args)
        {

            String dllName = new AssemblyName(args.Name).Name + ".dll";

            var assem = Assembly.GetExecutingAssembly();

            String resourceName = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.EndsWith(dllName));

            if (resourceName == null) return null; // Not found, maybe another handler will find it

            using (var stream = assem.GetManifestResourceStream(resourceName))
            {

                Byte[] assemblyData = new Byte[stream.Length];

                stream.Read(assemblyData, 0, assemblyData.Length);

                return Assembly.Load(assemblyData);
            }
        }
    }
}