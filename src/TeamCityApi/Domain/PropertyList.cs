using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public string ReplaceInString(string tokenizedString)
        {
            var re = new Regex(@"\%([\w\.]+)\%", RegexOptions.Compiled);

            return re.Replace(tokenizedString, match => this[match.Groups[1].Value].Value);
        }
    }
}