using System;

namespace TeamCityApi.Locators
{
    public class BuildLocator
    {
        private readonly CriteriaBuilder _criteriaBuilder = new CriteriaBuilder();

        public long? Id { get; private set; }
        public string Number { get; private set; }
        public string Branch { get; private set; }

        public BuildLocator With(string dimension, string value)
        {
            _criteriaBuilder.Add(dimension, value);
            return this;
        }

        public BuildLocator IncludePersonal(bool value = true)
        {
            return With("personal", value.ToString());
        }

        public BuildLocator IncludeCanceled(bool value = true)
        {
            return With("canceled", value.ToString());
        }

        public BuildLocator IncludePinned(bool value = true)
        {
            return With("pinned", value.ToString());
        }

        public BuildLocator IncludeRunning(bool value = true)
        {
            return With("running", value.ToString());
        }

        public BuildLocator WithTags(params string[] tags)
        {
            string formattedTags = string.Format("({0})", string.Join(",", tags));
            return With("tags", formattedTags);
        }

        public BuildLocator WithBuildConfiguration(BuildTypeLocator locator)
        {
            return With("buildType", string.Format("({0})", locator));
        }

        public BuildLocator WithUser(UserLocator locator)
        {
            return With("user", string.Format("({0})", locator));
        }

        public BuildLocator WithAgentName(string agentName)
        {
            return With("agentName", agentName);
        }

        public BuildLocator WithBuildStatus(BuildStatus buildStatus)
        {
            return With("status", buildStatus.ToString());
        }

        public BuildLocator WithMaxResults(int maxResults)
        {
            return With("count", maxResults.ToString());
        }

        public BuildLocator SinceBuild(BuildLocator locator)
        {
            return With("sinceBuild", string.Format("({0})", locator));
        }

        public BuildLocator WithStartIndex(int startIndex)
        {
            return With("start", startIndex.ToString());
        }

        public BuildLocator SinceDate(DateTime date)
        {
            return With("sinceDate", date.ToString("yyyyMMdd'T'HHmmsszzzz").Replace(":", "").Replace("+", "-"));
        }

        public BuildLocator WithBranch(BranchLocator locator)
        {
            return With("branch", string.Format("({0})", locator));
        }

        public override string ToString()
        {
            if (Id != null)
            {
                return "id:" + Id;
            }

            if (Number != null)
            {
                return "number:" + Number;
            }

            return _criteriaBuilder.ToString();
        }
    }
}
