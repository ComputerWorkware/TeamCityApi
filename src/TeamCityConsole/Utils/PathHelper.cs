using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TeamCityConsole.Utils
{
    public class PathHelper
    {
        public static string PathRelativeTo(string path, string root)
        {
            var pathParts = GetPathParts(path);
            var rootParts = GetPathParts(root);

            var length = pathParts.Count > rootParts.Count ? rootParts.Count : pathParts.Count;
            for (int i = 0; i < length; i++)
            {
                if (pathParts.First() == rootParts.First())
                {
                    pathParts.RemoveAt(0);
                    rootParts.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < rootParts.Count; i++)
            {
                pathParts.Insert(0, "..");
            }

            return pathParts.Count > 0 ? Path.Combine(pathParts.ToArray()) : string.Empty;
        }

        public static IList<string> GetPathParts(string path)
        {
            return path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();
        } 
    }
}