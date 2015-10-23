using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TeamCityApi.Helpers
{
    public static class SshKeyHelper
    {
        public const string ResourcePrefix = ".ssh.";
        public static string GetSSHKeyFolder()
        {
            string folderName = Path.Combine(Path.GetTempPath(), "CISERVERSSHKEY");
            if (!Directory.Exists(folderName))
            {
                string sshFolder = Path.Combine(folderName, ".ssh");
                Directory.CreateDirectory(sshFolder);

                var assem = Assembly.GetExecutingAssembly();
                var resourceNames = assem.GetManifestResourceNames().Where(rn => rn.Contains(ResourcePrefix));

                foreach (string resourceName in resourceNames)
                {
                    if (string.IsNullOrEmpty(resourceName))
                        continue;

                    int extractPosition = resourceName.IndexOf(ResourcePrefix, StringComparison.Ordinal);
                    var fileName = resourceName.Substring(extractPosition + ResourcePrefix.Length);
                    string fileFullName = Path.Combine(sshFolder, fileName);

                    if (File.Exists(fileFullName))
                        continue;

                    using (var stream = assem.GetManifestResourceStream(resourceName))
                    {
                        Byte[] assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        File.WriteAllBytes(fileFullName, assemblyData);
                    }
                }
            }

            return folderName;
        }
    }
}
