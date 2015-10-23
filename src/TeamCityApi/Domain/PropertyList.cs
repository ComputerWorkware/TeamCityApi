using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TeamCityApi.Clients;
using TeamCityApi.Logging;

namespace TeamCityApi.Domain
{
    public class PropertyList : List<Property>
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(PropertyList));

        public Property this[string name]
        {
            get
            {
                var property = this.FirstOrDefault(p => p.Name == name) ?? new Property()
                {
                    Name = string.Empty,
                    Value = string.Empty
                };

                return property;
            }
        }
    }
}