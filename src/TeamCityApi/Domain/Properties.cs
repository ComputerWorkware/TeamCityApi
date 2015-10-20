using System.Linq;

namespace TeamCityApi.Domain
{
    public class Properties
    {
        public string Count { get; set; }
        
        public PropertyList Property { get; set; }

        public Property this[string name] => Property[name];

        public override string ToString()
        {
            return string.Join(", ", Property);
        }
    }
}
