using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class Trigger
    {
        public string Id { get; set; }
        public string Type { get; set; }

        public List<Property> Properties { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Type: {1}", Id, Type);
        }
    }
}