namespace TeamCityApi.Locators
{
    public class ProjectLocator
    {
        private readonly CriteriaBuilder _criteriaBuilder = new CriteriaBuilder();

        public ProjectLocator With(string dimension, string value)
        {
            _criteriaBuilder.Add(dimension, value);
            return this;
        }

        public ProjectLocator WithId(string buildTypeId)
        {
            return With("id", buildTypeId);
        }

        public ProjectLocator WithName(string name)
        {
            return With("name", name);
        }

        public override string ToString()
        {
            return _criteriaBuilder.ToString();
        }
    }
}