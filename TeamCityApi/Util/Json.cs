using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TeamCityApi.Util
{
    public static class Json
    {
        private static readonly IsoDateTimeConverter DateTimeConverter = new IsoDateTimeConverter() { DateTimeFormat = "yyyyMMdd'T'HHmmsszzzz" };

        private static readonly CollectionWrapperConverter WrapperConverter = new CollectionWrapperConverter();

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, DateTimeConverter, WrapperConverter, new FileConverter());
        }

        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}