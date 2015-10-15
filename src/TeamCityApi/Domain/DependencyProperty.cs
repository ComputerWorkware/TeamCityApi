namespace TeamCityApi.Domain
{
    public class DependencyProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Value: {1}", Name, Value);
        }
    }
}