using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Graphs;

namespace TeamCityApi.Helpers
{
    public class BuildChain : Graph<Build>
    {
        private readonly IBuildClient _buildClient;

        public BuildChain(IBuildClient buildClient, Build rootBuild)
        {
            _buildClient = buildClient;

            InitGraph(rootBuild);
        }

        private void InitGraph(Build rootBuild)
        {
            AddBuildWithDependencies(new GraphNode<Build>(rootBuild));
        }

        private void AddBuildWithDependencies(GraphNode<Build> node)
        {
            AddNode(node);

            if (node.Value.ArtifactDependencies != null)
            {
                foreach (var artifactDependency in node.Value.ArtifactDependencies)
                {
                    var dependencyBuild = _buildClient.ById(artifactDependency.Id.ToString()).Result;
                    var dependencyBuildNode = new GraphNode<Build>(dependencyBuild);

                    if (!this.Contains(dependencyBuild))
                    {
                        AddBuildWithDependencies(dependencyBuildNode);
                    }

                    AddDirectedEdge(node, dependencyBuildNode, 0);
                }
            }
        }
    }
}