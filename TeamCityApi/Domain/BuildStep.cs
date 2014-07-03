using System.Collections.Generic;

namespace TeamCityApi.Domain
{
    public class BuildStep
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Disabled { get; set; }

        public List<Property> Properties { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, Type: {2}, Disabled: {3}", Id, Name, Type, Disabled);
        }
    }
}