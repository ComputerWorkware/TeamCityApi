namespace TeamCityApi.Domain
{
    public class AgentSummary
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string TypeId { get; set; }
        public string Href { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, TypeId: {2}, Href: {3}", Id, Name, TypeId, Href);
        }
    }
}