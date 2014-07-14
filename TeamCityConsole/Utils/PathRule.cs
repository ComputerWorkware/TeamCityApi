﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TeamCityApi.Domain;

namespace TeamCityConsole.Utils
{
    public class PathRule
    {
        public string Source { get; set; }
        public string Dest { get; set; }

        public static List<PathRule> Parse(string rulesString)
        {
            IEnumerable<string> lines = ParseLines(rulesString);

            return lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => ParseLine(line)).ToList();
        }

        public File GetFile(string basePath)
        {
            File file = ParseSource(basePath+Source);

            return file;
        }

        private static IEnumerable<string> ParseLines(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return Enumerable.Empty<string>();
            }

            string[] lines = str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            return lines;
        }

        private static PathRule ParseLine(string line)
        {
            string[] pathParts = line.Split(new []{"=>"}, StringSplitOptions.None);

            if (pathParts.Length == 2)
            {
                return new PathRule {Source = pathParts[0].Trim(), Dest = pathParts[1].Trim()};
            }

            return new PathRule {Source = pathParts[0].Trim()};
        }

        public static File ParseSource(string sourceStr)
        {
            string file = sourceStr.Replace('/', '\\');
            if (file.Contains("!"))
            {
                string[] strings = sourceStr.Split('!');
                string left = strings[0];
                file = strings[1];
            }

            file = GetLastPart(file);

            var result = new File
            {
                Name = file
            };

            if (IsFile(file))
            {
                result.ContentHref = sourceStr;
            }
            else
            {
                result.ChildrenHref = sourceStr;
            }

            return result;
        }

        private static string GetLastPart(string path)
        {
            string[] parts = path.Split('\\', '/');
            return parts.Last();
        }

        private static bool IsFile(string path)
        {
            return Regex.IsMatch(path, @".*\.[a-zA-Z0-9]{1,3}");
        }
    }
}