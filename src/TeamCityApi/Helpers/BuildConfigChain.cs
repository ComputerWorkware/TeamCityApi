using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Graphs;

namespace TeamCityApi.Helpers
{
    public class BuildConfigChain : Graph<BuildConfig>
    {
        private readonly IBuildConfigClient _buildConfigClient;

        public BuildConfigChain(IBuildConfigClient buildConfigClient, BuildConfig rootBuildConfig)
        {
            _buildConfigClient = buildConfigClient;

            InitGraph(rootBuildConfig);
        }

        private void InitGraph(BuildConfig rootBuildConfig)
        {
            AddBuildConfigWithDependencies(new GraphNode<BuildConfig>(rootBuildConfig));
        }

        private void AddBuildConfigWithDependencies(GraphNode<BuildConfig> parentNode)
        {
            AddNode(parentNode);

            if (parentNode.Value.ArtifactDependencies != null)
            {
                foreach (var artifactDependency in parentNode.Value.ArtifactDependencies)
                {
                    var dependencyBuildConfig = _buildConfigClient.GetByConfigurationId(artifactDependency.SourceBuildConfig.Id).Result;
                    var childNode = new GraphNode<BuildConfig>(dependencyBuildConfig);

                    if (!this.Contains(dependencyBuildConfig))
                    {
                        AddBuildConfigWithDependencies(childNode);
                    }

                    AddDirectedEdge(parentNode, childNode, 0);
                }
            }
        }
    }
}