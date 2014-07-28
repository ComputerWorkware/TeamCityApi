using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeamCityApi.Domain
{
    public class ArtifactsReference
    {
        private IHttpClientWrapper _http;

        public string Href { get; set; }

        public async Task<List<File>> GetFiles()
        {
            List<File> files = await _http.Get<List<File>>(Href);

            files.ForEach(x => x.Initialize(_http));

            return files;
        }

        internal void Initialize(IHttpClientWrapper http)
        {
            _http = http;
        }

        public override string ToString()
        {
            return string.Format("Href: {0}", Href);
        }
    }
}