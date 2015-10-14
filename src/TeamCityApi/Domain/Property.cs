namespace TeamCityApi.Domain
{
    public class Property
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Own { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Value: {1}, Own: {2}", Name, Value, Own);
        }
    }
}