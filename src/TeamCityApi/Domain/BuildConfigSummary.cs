namespace TeamCityApi.Domain
{
    public class BuildConfigSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, ProjectName: {2}, ProjectId: {3}, Href: {4}, WebUrl: {5}", Id, Name, ProjectName, ProjectId, Href, WebUrl);
        }
    }
}