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
            var resolvedValuesHavePlaceholders = false;

            var replaced = re.Replace(tokenizedString,
                match =>
                {
                    var resolvedValue = this[match.Groups[1].Value].Value;
                    if (!string.IsNullOrEmpty(resolvedValue))
                    {
                        if (resolvedValue.Contains("%"))
                        {
                            resolvedValuesHavePlaceholders = true;
                        }
                        return resolvedValue;
                    }
                    else
                    {
                        //if could not resolve a placeholder then do not replace it.
                        return match.Value;
                    }
                });

            if (resolvedValuesHavePlaceholders)
            {
                replaced = ReplaceInString(replaced);
            }

            return replaced;
        }
    }
}