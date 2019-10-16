using System;
using System.Collections.Generic;
using TeamCityApi.Locators;

namespace TeamCityApi.Fields
{
    public class FileFields
    {
        private readonly List<string> _fields = new List<string>();

        public FileFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public FileFields WithBeforeRevision()
        {
            _fields.Add("before-revision");
            return this;
        }

        public FileFields WithAfterRevision()
        {
            _fields.Add("after-revision");
            return this;
        }

        public FileFields WithChangeType()
        {
            _fields.Add("changeType");
            return this;
        }

        public FileFields WithFile()
        {
            _fields.Add("file");
            return this;
        }

        public FileFields WithRelativeFile()
        {
            _fields.Add("relative-file");
            return this;
        }

        public override string ToString()
        {
            return "file(" + String.Join(",", _fields) + ")";
        }
    }
}