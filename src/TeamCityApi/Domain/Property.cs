namespace TeamCityApi.Domain
{
    public class Property
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Own { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Value);
        }
    }
}