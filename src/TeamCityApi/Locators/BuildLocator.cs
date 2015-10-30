using System;

namespace TeamCityApi.Locators
{
    public class BuildLocator
    {
        private readonly CriteriaBuilder _criteriaBuilder = new CriteriaBuilder();

        public long? Id { get; private set; }
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

        public BuildLocator WithBuildConfiguration(Action<BuildTypeLocator> locatorAction)
        {
            var locator = new BuildTypeLocator();
            locatorAction(locator);
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

        public BuildLocator WithNumber(string number, string buildConfigId)
        {
            return With("number", string.Format("{0}", number))
                .WithBuildConfiguration(bcLocator => bcLocator.WithId(buildConfigId));
        }

        public BuildLocator WithSnapshotDependencyTo(long id, bool includeInitial = true, bool recursive = true)
        {
            return With("snapshotDependency", string.Format("(to:(id:{0}),includeInitial:{1},recursive:{2})", id, includeInitial.ToString().ToLower(), recursive.ToString().ToLower()));
        }

        public BuildLocator WithSnapshotDependencyFrom(long id, bool includeInitial = true, bool recursive = true)
        {
            return With("snapshotDependency", string.Format("(from:(id:{0}),includeInitial:{1},recursive:{2})", id, includeInitial.ToString().ToLower(), recursive.ToString().ToLower()));
        }

        public override string ToString()
        {
            if (Id != null)
            {
                return "id:" + Id;
            }

            return _criteriaBuilder.ToString();
        }
    }
}
