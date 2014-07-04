using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using TeamCityApi.Domain;

namespace TeamCityApi.Util
{
    public class FileConverter : CustomCreationConverter<File>
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            File file = new File();

            JObject jObject = JObject.Load(reader);

            JToken contentToken = jObject["content"];
            JToken childrenToken = jObject["children"];

            serializer.Populate(jObject.CreateReader(), file);

            if (contentToken != null)
            {
                file.ContentHref = contentToken["href"].Value<string>();
            }

            if (childrenToken != null)
            {
                file.ChildrenHref = childrenToken["href"].Value<string>();
            }

            return file;
        }

        public override File Create(Type objectType)
        {
            return new File();
        }
    }

    public class CollectionWrapperConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var target = Activator.CreateInstance(objectType);

            JsonToken tokenType = reader.TokenType;

            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject jObject = JObject.Load(reader);

                JToken wrappingToken = jObject.Children().FirstOrDefault(x => x.Type == JTokenType.Property && x.First != null && x.First.Type == JTokenType.Array);

                if (wrappingToken == null)
                {
                    return target;
                }

                JToken arrayToken = wrappingToken.First;

                serializer.Populate(arrayToken.CreateReader(), target);                
            }
            else
            {
                serializer.Populate(reader, target);
            }

            return target;
        }

        public override bool CanConvert(Type objectType)
        {
            TypeInfo typeInfo = objectType.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(List<>))
            {
                return true;
            }
            return false;
        }

        public override bool CanWrite
        {
            get { return false; }
        }
    }
}