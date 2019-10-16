using System;
using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Username: {1}, Name: {2}", Id, Username, Name);
        }
    }
}