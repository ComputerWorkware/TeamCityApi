using System;
using System.Collections.Generic;

namespace TeamCityApi.Fields
{
    public class ChangesFields
    {
        private readonly List<string> _fields = new List<string>();

        public ChangesFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public ChangesFields WithLong()
        {
            _fields.Add("$long");
            return this;
        }

        public ChangesFields WithChangeFields(Action<ChangeFields> fieldsAction)
        {
            var fields = new ChangeFields();
            fieldsAction(fields);
            _fields.Add(fields.ToString());
            return this;
        }

        public override string ToString()
        {
            return "changes(" + String.Join(",", _fields) + ")";
        }

    }
}