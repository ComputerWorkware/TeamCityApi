using System.Collections.Generic;
using System.Threading.Tasks;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Clients
{
    public interface IChangeClient
    {
        Task<List<ChangeSummary>> GetAll();
        Task<Change> GetById(string changeId);
    }

    public class ChangeClient : IChangeClient
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ChangeClient));
        
        private readonly IHttpClientWrapper _http;

        private const string _baseUri = "/app/rest/changes";

        public ChangeClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<List<ChangeSummary>> GetAll()
        {
            Log.TraceFormat("API Change.GetAll().");

            var changes = await _http.Get<List<ChangeSummary>>(_baseUri);

            return changes;
        }

        public async Task<Change> GetById(string changeId)
        {
            Log.TraceFormat("API Change.GetById(). changeId: {0}", changeId);

            string requestUri = string.Format("{0}/id:{1}",_baseUri,  changeId);

            var change = await _http.Get<Change>(requestUri);

            return change;
        }
    }
}