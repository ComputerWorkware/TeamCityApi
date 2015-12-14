using System;
using Newtonsoft.Json;

namespace TeamCityApi.Tests.Helpers
{
    public static class CloneObject
    {
        public static T CloneViaJson<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }

    }
}