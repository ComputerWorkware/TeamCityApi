using System.Threading.Tasks;
using TeamCityApi.Clients;

namespace TeamCityApi.Domain
{
    public class BuildSummary
    {
        private IBuildClient _buildClient;

        public string Id { get; set; }
        public string BuildTypeId { get; set; }
        public string Href { get; set; }
        public string Number { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public string WebUrl { get; set; }

        internal void SetBuildClient(IBuildClient buildClient)
        {
            _buildClient = buildClient;
        }

        public async Task<Build> GetDetails()
        {
            return await _buildClient.ById(Id);
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Number: {1}, BuildTypeId: {2}, State: {3}, Status: {4}", Id, Number, BuildTypeId, State, Status);
        }
    }
}