namespace TeamCityApi.Domain
{
    public class BuildSummary
    {
        public string Id { get; set; }
        public string BuildTypeId { get; set; }
        public string Href { get; set; }
        public string Number { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public string WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Number: {1}, BuildTypeId: {2}, State: {3}, Status: {4}", Id, Number, BuildTypeId, State, Status);
        }
    }
}