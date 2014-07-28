using System;
using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class Change
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Username { get; set; }
        public DateTime Date { get; set; }
        public string Href { get; set; }
        public string WebLink { get; set; }
        public string Comment { get; set; }

        public List<FileRevision> Files { get; set; }

        public VcsRootInstance VcsRootInstance { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Username: {1}, Date: {2}, Version: {3}, Comment: {4}", Id, Username, Date, Version, Comment);
        }
    }
}