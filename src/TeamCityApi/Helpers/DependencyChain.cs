using System;
using System.Linq;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.Locators;

namespace TeamCityApi.Helpers
{
    public class DependencyChain : Graph<CombinedDependency>
    {
        private readonly ITeamCityClient _client;
        private readonly string _buildChainId;

        public DependencyChain(ITeamCityClient client, BuildConfig rootBuildConfig)
        {
            _client = client;
            _buildChainId = rootBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
            InitGraph(rootBuildConfig);
        }

        private void InitGraph(BuildConfig rootBuildConfig)
        {
            AddBuildConfigWithDependents(new GraphNode<CombinedDependency>(new CombinedDependency(rootBuildConfig)));
        }

        private void AddBuildConfigWithDependents(GraphNode<CombinedDependency> node)
        {
            AddNode(node);

            if (node.Value.BuildConfig.ArtifactDependencies != null)
            {
                foreach (var artifactDependency in node.Value.BuildConfig.ArtifactDependencies)
                {
                    var child = ConstructCombinedDependency(artifactDependency, node.Value.Build);
                    var childNode = new GraphNode<CombinedDependency>(child);

                    if (!this.Contains(child))
                    {
                        AddBuildConfigWithDependents(childNode);
                    }

                    AddDirectedEdge(node, childNode, 0);
                }
            }
        }

        private CombinedDependency ConstructCombinedDependency(DependencyDefinition artifactDependency, Build parentBuild)
        {
            BuildConfig dependencyBuildConfig = _client.BuildConfigs.GetByConfigurationId(artifactDependency.SourceBuildConfig.Id).Result;

            Build dependencyBuild = null;
            switch (artifactDependency.Properties["revisionName"].Value)
            {
                case "buildNumber":
                    var dependsOnBuildNumber = artifactDependency.Properties["revisionValue"].Value;
                    dependencyBuild = _client.Builds.ByNumber(dependsOnBuildNumber, dependencyBuildConfig.Id).Result;
                    break;
                case "sameChainOrLastFinished":
                    if (parentBuild != null)
                    {
                        var buildDependencySummary = parentBuild.ArtifactDependencies.FirstOrDefault(d => d.BuildTypeId == dependencyBuildConfig.Id);
                        if (buildDependencySummary != null)
                        {
                            dependencyBuild = _client.Builds.ById(buildDependencySummary.Id).Result;
                        }
                    }
                    break;
            }

            var isCloned = _buildChainId == dependencyBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value;

            return new CombinedDependency()
            {
                BuildConfig = dependencyBuildConfig,
                Build = dependencyBuild,
                IsCloned = isCloned
            };
        }

        public override string ToString()
        {
            return SketchGraph();
        }

        public string SketchGraph(GraphNode<CombinedDependency> node = null, int level = 0)
        {
            if (level == 0 && Count == 0)
                return "Empty chain";

            if (node == null)
                node = (GraphNode<CombinedDependency>)Nodes.First();
            
            var buildConfigId = node.Value.BuildConfig.Id;
            var buildNumber = (node.Value.Build != null) ? " | Build #" + node.Value.Build.Number : " | Same chain";
            var cloned = level > 0 && node.Value.IsCloned ? " | Cloned" : " | Original";

            var sketch = new string((char)0x2014, level) +
                buildConfigId +
                cloned +
                buildNumber +
                Environment.NewLine;

            foreach (var child in node.Neighbors)
            {
                sketch += SketchGraph((GraphNode<CombinedDependency>)child, level + 1);
            }

            return sketch;
        }

        internal bool Contains(BuildConfig buildConfig)
        {
            return this.Any(d => d.BuildConfig.Equals(buildConfig));
        }
    }

    public class CombinedDependency
    {
        public Build Build { get; set; }
        public BuildConfig BuildConfig { get; set; }
        public bool IsCloned { get; set; }

        public CombinedDependency(BuildConfig buildConfig)
        {
            BuildConfig = buildConfig;
        }

        public CombinedDependency()
        {
        }

        public override string ToString()
        {
            return string.Format("BuildConfigId: {0}, Build Number: {1}", BuildConfig.Id, Build?.Number);
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Build return false.
            CombinedDependency bc = obj as CombinedDependency;
            if (bc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (BuildConfig.Id == bc.BuildConfig.Id && Build?.Id == bc.Build?.Id);
        }

        public bool Equals(CombinedDependency bc)
        {
            // If parameter is null return false:
            if (bc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (BuildConfig.Id == bc.BuildConfig.Id && Build?.Id == bc.Build?.Id);
        }

        public override int GetHashCode()
        {
            var hash = BuildConfig.Id.GetHashCode();
            if (Build != null)
            {
                hash ^= Build.Id.GetHashCode();
            }
            return hash;
        }
    }
}