using System.Diagnostics;
using System.Reflection;

namespace TeamCityConsole.Utils
{
    public interface IAssemblyMetada
    {
        string FileVersion { get; }
        string Location { get; }
    }

    public class AssemblyMetada : IAssemblyMetada
    {
        private readonly Assembly _assembly = typeof (AssemblyMetada).Assembly;

        public string FileVersion
        {
            get
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(_assembly.Location);
                return fvi.FileVersion;
            }
        }

        public string Location
        {
            get { return _assembly.Location; }
        }
    }
}