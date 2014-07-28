namespace TeamCityApi.Domain
{
    public class TemplateSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool TemplateFlag { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public string Href { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, ProjectId: {2}, ProjectName: {3}, TemplateFlag: {4}, Href: {5}", Id, Name, ProjectId, ProjectName, TemplateFlag, Href);
        }
    }
}