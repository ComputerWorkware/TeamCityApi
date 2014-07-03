using Newtonsoft.Json;

namespace TeamCityApi.Domain
{
    public class FileRevision
    {
        [JsonProperty("before-revision")]
        public string BeforeRevision { get; set; }

        [JsonProperty("after-revision")]
        public string AfterRevision { get; set; }

        public string File { get; set; }

        [JsonProperty("relative-file")]
        public string RelativeFile { get; set; }

        public override string ToString()
        {
            return string.Format("File: {0}, RelativeFile: {1}, BeforeRevision: {2}, AfterRevision: {3}", File, RelativeFile, BeforeRevision, AfterRevision);
        }
    }
}