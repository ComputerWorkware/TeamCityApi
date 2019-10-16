using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamCityApi.Clients;

namespace TeamCityApi.Domain
{
    public class BuildSummary
    {
        private IBuildClient _buildClient;

        public long Id { get; set; }
        public string BuildTypeId { get; set; }
        public string Href { get; set; }
        public string Number { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public string WebUrl { get; set; }
        public List<ChangeSummary> Changes { get; set; }

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

        public static explicit operator BuildSummary(Build b)
        {
            return new BuildSummary()
            {
                
                Id = b.Id,
                BuildTypeId = b.BuildTypeId,
                Number = b.Number,
                Status = b.Status,
                State = b.State,
                Href = b.Href,
                WebUrl = b.WebUrl
            };
        }
    }
}