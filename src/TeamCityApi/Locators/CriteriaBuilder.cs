using System.Collections.Generic;
using System.Linq;

namespace TeamCityApi.Locators
{
    public class CriteriaBuilder
    {
        private readonly Dictionary<string,string> _criteriaDictionary = new Dictionary<string, string>();

        public void Add(string dimension, string value)
        {
            _criteriaDictionary[dimension] = value;
        }

        public override string ToString()
        {
            var criterias = _criteriaDictionary.Select(x => string.Format("{0}:{1}", x.Key, x.Value));
            return string.Join(",", criterias);
        }
    }
}