namespace TeamCityApi.Domain
{
    public class Dependency
    {
        public long Id { get; set; }
        public string BuildTypeId { get; set; }
        public string Number { get; set; }
        public BuildStatus Status { get; set; }
        public string State { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, BuildTypeId: {1}, Number: {2}, Status: {3}, State: {4}, Href: {5}, WebUrl: {6}", Id, BuildTypeId, Number, Status, State, Href, WebUrl);
        }
    }
}