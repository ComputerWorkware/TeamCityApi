namespace TeamCityApi.Domain
{
    public class ProjectSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentProjectId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, ParentProjectId: {1}, Href: {2}, WebUrl: {3}", Id, ParentProjectId, Href, WebUrl);
        }
    }
}