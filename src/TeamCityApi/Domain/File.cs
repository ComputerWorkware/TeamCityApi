using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeamCityApi.Domain
{
    public class File
    {
        private IHttpClientWrapper _http;

        public int Size { get; set; }
        public DateTime ModificationTime { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }

        public string ContentHref { get; set; }

        public string ChildrenHref { get; set; }

        public bool HasChildren
        {
            get { return string.IsNullOrWhiteSpace(ChildrenHref) == false; }
        }

        public bool HasContent
        {
            get { return ContentHref != null; }
        }

        public async Task<List<File>> GetChildren()
        {
            if (ChildrenHref == null)
            {
                return new List<File>();
            }

            List<File> files = await _http.Get<List<File>>(ChildrenHref);

            files.ForEach(file => file.Initialize(_http));

            return files;
        }

        internal void Initialize(IHttpClientWrapper http)
        {
            _http = http;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}", Name);
        }
    }
}