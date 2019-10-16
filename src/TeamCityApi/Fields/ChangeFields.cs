using System;
using System.Collections.Generic;
using TeamCityApi.Locators;

namespace TeamCityApi.Fields
{
    public class ChangeFields
    {
        private readonly List<string> _fields = new List<string>();

        public ChangeFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public ChangeFields WithLong()
        {
            _fields.Add("$long");
            return this;
        }

        public ChangeFields WithId()
        {
            _fields.Add("id");
            return this;
        }

        public ChangeFields WithVersion()
        {
            _fields.Add("version");
            return this;
        }

        public ChangeFields WithUsername()
        {
            _fields.Add("username");
            return this;
        }

        public ChangeFields WithDate()
        {
            _fields.Add("date");
            return this;
        }

        public ChangeFields WithComment()
        {
            _fields.Add("comment");
            return this;
        }

        public ChangeFields WithHref()
        {
            _fields.Add("href");
            return this;
        }

        public ChangeFields WithWebUrl()
        {
            _fields.Add("webUrl");
            return this;
        }

        public ChangeFields WithUserFields(Action<UserFields> fieldsAction)
        {
            var fields = new UserFields();
            fieldsAction(fields);
            _fields.Add(fields.ToString());
            return this;
        }

        public ChangeFields WithFilesFields(Action<FilesFields> fieldsAction)
        {
            var fields = new FilesFields();
            fieldsAction(fields);
            _fields.Add(fields.ToString());
            return this;
        }

        public override string ToString()
        {
            return "change(" + String.Join(",", _fields) + ")";
        }
    }
}