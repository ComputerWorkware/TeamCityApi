using System;

namespace TeamCityApi.Domain
{
    public class ChangeSummary
    {
        public long Id { get; set; }
        public string Version { get; set; }
        public string Username { get; set; }
        public DateTime Date { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Version: {1}, Date: {2}, Username: {3}, Href: {4}, WebUrl: {5}", Id, Version, Date, Username, Href, WebUrl);
        }
    }
}