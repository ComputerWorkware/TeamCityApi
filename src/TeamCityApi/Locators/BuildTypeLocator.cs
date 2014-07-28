namespace TeamCityApi.Locators
{
    public class BuildTypeLocator
    {
        private readonly CriteriaBuilder _criteriaBuilder = new CriteriaBuilder();

        public BuildTypeLocator With(string dimension, string value)
        {
            _criteriaBuilder.Add(dimension, value);
            return this;
        }

        public BuildTypeLocator WithId(string buildTypeId)
        {
            return With("id", buildTypeId);
        }

        public BuildTypeLocator WithName(string name)
        {
            return With("name", name);
        }

        public override string ToString()
        {
            return _criteriaBuilder.ToString();
        }
    }
}