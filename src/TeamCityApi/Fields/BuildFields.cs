using System;
using System.Collections.Generic;
using TeamCityApi.Locators;

namespace TeamCityApi.Fields
{
    public class BuildFields
    {
        private readonly List<string> _fields = new List<string>();

        public BuildFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public BuildFields WithLong()
        {
            _fields.Add("$long");
            return this;
        }

        public BuildFields WithId()
        {
            _fields.Add("id");
            return this;
        }

        public BuildFields WithNumber()
        {
            _fields.Add("number");
            return this;
        }

        public BuildFields WithStatus()
        {
            _fields.Add("status");
            return this;
        }

        public BuildFields WithStartDate()
        {
            _fields.Add("startDate");
            return this;
        }

        public BuildFields WithFinishDate()
        {
            _fields.Add("finishDate");
            return this;
        }

        public BuildFields WithChangesFields(Action<ChangesFields> fieldsAction)
        {
            var fields = new ChangesFields();
            fieldsAction(fields);
            _fields.Add(fields.ToString());
            return this;
        }

        public override string ToString()
        {
            return "build(" + String.Join(",", _fields) + ")";
        }
    }
}