using System;
using System.Linq;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.Logging;
using TeamCityApi.Util;

namespace TeamCityApi.Helpers
{
    public class BuildConfigChain : Graph<BuildConfig>
    {
        private readonly IBuildConfigClient _buildConfigClient;
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public BuildConfigChain(IBuildConfigClient buildConfigClient, BuildConfig rootBuildConfig)
        {
            _buildConfigClient = buildConfigClient;

            InitGraph(rootBuildConfig);
        }

        private void InitGraph(BuildConfig rootBuildConfig)
        {
            AddBuildConfigWithDependents(new GraphNode<BuildConfig>(rootBuildConfig));
        }

        private void AddBuildConfigWithDependents(GraphNode<BuildConfig> node)
        {
            AddNode(node);

            if (node.Value.ArtifactDependencies != null)
            {
                foreach (var artifactDependency in node.Value.ArtifactDependencies)
                {
                    BuildConfig dependencyBuildConfig = null;
                    try
                    {
                        dependencyBuildConfig = _buildConfigClient.GetByConfigurationId(artifactDependency.SourceBuildConfig.Id).Result;
                    }
                    catch (AggregateException e)
                    {
                        Log.Warn("Could not load BuildConfigId " + artifactDependency.SourceBuildConfig.Id + ". It could have been deleted or renamed.");
                        Log.Warn(e.Message);
                    }

                    if (dependencyBuildConfig != null)
                    {
                        var childNode = new GraphNode<BuildConfig>(dependencyBuildConfig);

                        if (!this.Contains(dependencyBuildConfig))
                        {
                            AddBuildConfigWithDependents(childNode);
                        }

                        AddDirectedEdge(node, childNode, 0);
                    }
                }
            }
        }

        public override string ToString()
        {
            return SketchGraph();
        }

        public string SketchGraph(GraphNode<BuildConfig> node = null, int level = 0)
        {
            if (level == 0 && Count == 0)
                return "Empty chain";

            if (node == null)
                node = (GraphNode<BuildConfig>)Nodes.First();

            var sketch = new string('-', level) + node.Value.Id + Environment.NewLine;

            foreach (var child in node.Neighbors)
            {
                sketch += SketchGraph((GraphNode<BuildConfig>)child, level + 1);
            }

            return sketch;
        }
    }
}