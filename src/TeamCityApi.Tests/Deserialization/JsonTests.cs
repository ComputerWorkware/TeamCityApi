using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using TeamCityApi.Domain;
using TeamCityApi.Util;
using Xunit;

namespace TeamCityApi.Tests.Deserialization
{
    public class JsonTests
    {
        [Fact]
        public void FactMethodName()
        {
            Type type = typeof(List<string>);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                
            }
            Type type1 = typeof(List<>);
            Type listType = typeof(IList);
            bool isAssignableFrom = type1.IsAssignableFrom(type);
            bool isAssignableFrom1 = listType.IsAssignableFrom(type);
            List<string> list = Activator.CreateInstance(type) as List<string>;
        }

        [Fact]
        public void WrappedCollectionTest()
        {
            string json = @"
                    {
                        ""files"": [
                            {
                                ""size"": 5120,
                                ""modificationTime"": ""20140511T122234-0400"",
                                ""content"": {
                                    ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/content/foo.core.dll""
                                },
                                ""name"": ""foo.core.dll"",
                                ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/metadata/foo.core.dll""
                            },
                            {
                                ""size"": 11776,
                                ""modificationTime"": ""20140511T122234-0400"",
                                ""content"": {
                                    ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/content/foo.core.pdb""
                                },
                                ""name"": ""foo.core.pdb"",
                                ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/metadata/foo.core.pdb""
                            }
                        ]
                    }";
            List<File> files = JsonConvert.DeserializeObject<List<File>>(json, new CollectionWrapperConverter(),
                new IsoDateTimeConverter() {DateTimeFormat = "yyyyMMdd'T'HHmmsszzzz"});

            string serialize = Json.Serialize(files);
            //List<File> files = Json.Deserialize<List<File>>(json);
        }

        [Fact]
        public void RegularCollectionTest()
        {
            string json = @"
                    [
                        
                            {
                                ""size"": 5120,
                                ""modificationTime"": ""20140511T122234-0400"",
                                ""content"": {
                                    ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/content/foo.core.dll""
                                },
                                ""name"": ""foo.core.dll"",
                                ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/metadata/foo.core.dll""
                            },
                            {
                                ""size"": 11776,
                                ""modificationTime"": ""20140511T122234-0400"",
                                ""content"": {
                                    ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/content/foo.core.pdb""
                                },
                                ""name"": ""foo.core.pdb"",
                                ""href"": ""/httpAuth/app/rest/builds/id:22/artifacts/metadata/foo.core.pdb""
                            }
                        
                    ]";

            List<File> files = JsonConvert.DeserializeObject<List<File>>(json, new CollectionWrapperConverter(),
                new IsoDateTimeConverter() { DateTimeFormat = "yyyyMMdd'T'HHmmsszzzz" });
        }
    }

    public class ObjectToArrayConverter<T> : CustomCreationConverter<List<T>> where T : new()
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<T> target = new List<T>();

            try
            {
                JObject jObject = JObject.Load(reader);

                JToken token = jObject.First.First;

                //JToken token = jObject["files"];
                
                serializer.Populate(token.CreateReader(), target);
            }
            catch (JsonReaderException)
            {
            }

            return target;
        }

        public override List<T> Create(Type objectType)
        {
            return new List<T>();
        }
    }
}