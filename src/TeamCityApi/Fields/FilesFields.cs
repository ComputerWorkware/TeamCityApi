using System;
using System.Collections.Generic;
using TeamCityApi.Locators;

namespace TeamCityApi.Fields
{
    public class FilesFields
    {
        private readonly List<string> _fields = new List<string>();

        public FilesFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public FilesFields WithLong()
        {
            _fields.Add("$long");
            return this;
        }

        public FilesFields WithFileFields(Action<FileFields> fieldsAction)
        {
            var fields = new FileFields();
            fieldsAction(fields);
            _fields.Add(fields.ToString());
            return this;
        }

        public override string ToString()
        {
            return "files(" + String.Join(",", _fields) + ")";
        }

    }
}