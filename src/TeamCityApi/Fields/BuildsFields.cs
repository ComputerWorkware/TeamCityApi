using System;
using System.Collections.Generic;
using System.Linq;
using TeamCityApi.Locators;

namespace TeamCityApi.Fields
{
    public class BuildsFields
    {
        private readonly List<string> _fields = new List<string>();

        public BuildsFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public BuildsFields WithLong()
        {
            _fields.Add("$long");
            return this;
        }

        public BuildsFields WithBuildFields(Action<BuildFields> fieldsAction)
        {
            var fields = new BuildFields();
            fieldsAction(fields);
            _fields.Add(fields.ToString());
            return this;
        }

        public override string ToString()
        {
            return String.Join(",", _fields);
        }

    }
}