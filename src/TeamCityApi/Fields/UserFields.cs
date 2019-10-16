using System;
using System.Collections.Generic;
using TeamCityApi.Locators;

namespace TeamCityApi.Fields
{
    public class UserFields
    {
        private readonly List<string> _fields = new List<string>();

        public UserFields With(string field)
        {
            _fields.Add(field);
            return this;
        }

        public UserFields WithUsername()
        {
            _fields.Add("username");
            return this;
        }

        public UserFields WithName()
        {
            _fields.Add("name");
            return this;
        }

        public UserFields WithId()
        {
            _fields.Add("id");
            return this;
        }

        public UserFields WithHref()
        {
            _fields.Add("href");
            return this;
        }

        public override string ToString()
        {
            return "user(" + String.Join(",", _fields) + ")";
        }
    }
}