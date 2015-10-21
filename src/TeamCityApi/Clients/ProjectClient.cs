using System.Collections.Generic;
using System.Threading.Tasks;
using TeamCityApi.Domain;
using TeamCityApi.Logging;

namespace TeamCityApi.Clients
{
    public interface IProjectClient
    {
        Task<List<ProjectSummary>> GetAll();
        Task<Project> GetById(string projectId);
        Task<Project> GetByName(string name);
    }

    public class ProjectClient : IProjectClient
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ProjectClient));

        private readonly IHttpClientWrapper _http;

        private const string _baseUri = "/app/rest/projects";

        public ProjectClient(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<List<ProjectSummary>> GetAll()
        {
            Log.TraceFormat("API Project.GetAll()");

            var projects = await _http.Get<List<ProjectSummary>>(_baseUri);

            return projects;
        }

        public async Task<Project> GetById(string projectId)
        {
            Log.TraceFormat("API Project.GetById(projectId: {0})", projectId);

            string requestUri = string.Format("{0}/id:{1}", _baseUri, projectId);

            var project = await _http.Get<Project>(requestUri);

            return project;
        }

        public async Task<Project> GetByName(string name)
        {
            Log.TraceFormat("API Project.GetByName(name: {0})", name);

            string requestUri = string.Format("{0}/name:{1}", _baseUri, name);

            var project = await _http.Get<Project>(requestUri);

            return project;
        }
    }
}