using System.Threading.Tasks;
using TeamCityApi.Domain;

namespace TeamCityApi.Clients
{
    public interface IVcsRootInstanceClient
    {
        Task<VcsRootInstance> ById(string projectId);
    }

    public class VcsRootInstanceClient : IVcsRootInstanceClient
    {
        private readonly IHttpClientWrapper _http;
        public VcsRootInstanceClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<VcsRootInstance> ById(string id)
        {
            string requestUri = $"/app/rest/vcs-root-instances/id:{id}";

            return await _http.Get<VcsRootInstance>(requestUri);
        }
    }
}