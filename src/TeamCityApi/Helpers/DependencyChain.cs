using System;
using System.Collections.Generic;
using System.Linq;
using TeamCityApi.Clients;
using TeamCityApi.Domain;
using TeamCityApi.Helpers.Graphs;
using TeamCityApi.Locators;
using TeamCityApi.UseCases;

namespace TeamCityApi.Helpers
{
    public class DependencyChain : Graph<DependencyNode>
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
            AddDependencies(new GraphNode<DependencyNode>(new DependencyNode(rootBuildConfig)));
        }

        private void AddDependencies(GraphNode<DependencyNode> node)
        {
            AddNode(node);

            if (node.Value.CurrentBuildConfig != null)
            {
                if (node.Value.CurrentBuildConfig.ArtifactDependencies != null)
                {
                    foreach (var artifactDependency in node.Value.CurrentBuildConfig.ArtifactDependencies)
                    {
                        AddDependency(
                            node,
                            artifactDependency.Properties["revisionName"].Value,
                            artifactDependency.Properties["revisionValue"].Value,
                            artifactDependency.SourceBuildConfig.Id);
                    }
                }
            }

            if (node.Value.HistoricBuild != null)
            {
                if (node.Value.HistoricBuild.ArtifactDependencies != null)
                {
                    foreach (var artifactDependency in node.Value.HistoricBuild.ArtifactDependencies)
                    {
                        AddDependency(
                            node,
                            "buildNumber",
                            artifactDependency.Number,
                            artifactDependency.BuildTypeId);
                    }
                }
            }
        }

        private void AddDependency(GraphNode<DependencyNode> parentNode, string revisionName, string revisionValue, string sourceBuildConfigId)
        {
            BuildConfig currentBuildConfig = null;
            Build historicBuild = null;
            bool isCloned = false;

            switch (revisionName)
            {
                case "buildNumber":
                    var dependsOnBuildNumber = revisionValue;
                    historicBuild = _client.Builds.ByNumber(dependsOnBuildNumber, sourceBuildConfigId).Result;
                    break;
                case "sameChainOrLastFinished":
                    currentBuildConfig = _client.BuildConfigs.GetByConfigurationId(sourceBuildConfigId).Result;
                    isCloned = _buildChainId == currentBuildConfig.Parameters[ParameterName.BuildConfigChainId].Value;
                    break;
            }

            var childDependency = new DependencyNode()
            {
                CurrentBuildConfig = currentBuildConfig,
                HistoricBuild = historicBuild,
                IsCloned = isCloned
            };

            var childDependencyGraphNode = new GraphNode<DependencyNode>(childDependency);

            AddDependencies(childDependencyGraphNode);

            AddDirectedEdge(parentNode, childDependencyGraphNode, 0);
        }

        /// <summary>
        /// Returns Build Configs, which are included to Build Chain with multiple versions.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IGrouping<BuildConfig, Build>> GetNonUniqueDependencies()
        {
            var results = from d in Nodes
                          group d.Value.HistoricBuild by d.Value.CurrentBuildConfig into g
                          where g.Count() > 1
                          select g;
            return results;
        }

        public override string ToString()
        {
            var dependencies = Nodes.Select(n => n.Value.ToString()).ToList();
            dependencies.Sort();
            return string.Join(Environment.NewLine, dependencies);
        }
        public string ToString(ShowBuildChainUseCase.BuildChainFilter filter)
        {
            var dependencies = new List<string>();
            switch (filter)
            {
                case ShowBuildChainUseCase.BuildChainFilter.Cloned:
                    dependencies.AddRange(from node in Nodes where node.Value.IsCloned select node.Value.ToString());
                    break;
                case ShowBuildChainUseCase.BuildChainFilter.Original:
                    dependencies.AddRange(from node in Nodes where !node.Value.IsCloned select node.Value.ToString());
                    break;
                default:
                    dependencies = Nodes.Select(n => n.Value.ToString()).ToList();
                    break;
            }
            dependencies.Sort();
            return string.Join(Environment.NewLine, dependencies);
        }


        public string SketchGraph(GraphNode<DependencyNode> node = null, int level = 0)
        {
            if (level == 0 && Count == 0)
                return "Empty chain";

            if (node == null)
                node = (GraphNode<DependencyNode>)Nodes.First();

            var sketch = new string(' ', level * 2) + " - " +
                node.Value +
                Environment.NewLine;

            foreach (var child in node.Neighbors)
            {
                sketch += SketchGraph((GraphNode<DependencyNode>)child, level + 1);
            }

            return sketch;
        }

        internal bool Contains(string buildConfigId)
        {
            return this.Any(d => d.CurrentBuildConfig?.Id == buildConfigId || d.HistoricBuild?.BuildTypeId == buildConfigId);
        }

        internal DependencyNode FindByBuildConfigId(string buildConfigId)
        {
            return this.First(d => d.CurrentBuildConfig?.Id == buildConfigId || d.HistoricBuild?.BuildTypeId == buildConfigId);
        }

        public IEnumerable<DependencyNode> GetParents(string childBuildConfigId)
        {
            return from GraphNode<DependencyNode> node in Nodes
                   where node.Neighbors.Any(n => n.Value.CurrentBuildConfig?.Id == childBuildConfigId || n.Value.HistoricBuild?.BuildTypeId == childBuildConfigId)
                   select node.Value;
        }

        public HashSet<DependencyNode> FindAllParents(string childBuildConfigId)
        {
            var allParents = new HashSet<DependencyNode>();

            var directParents = GetParents(childBuildConfigId).ToArray();

            allParents.UnionWith(directParents);

            if (directParents.Any())
            {
                foreach (var directParent in directParents)
                {
                    allParents.UnionWith(FindAllParents(directParent.HistoricBuild?.BuildTypeId ?? directParent.CurrentBuildConfig.Id));
                }
            }

            return allParents;
        }
    }

    /// <summary>
    /// Contains either a Build or BuildConfig. Depending if parent node references it as a:
    ///  * "fixed build number" dependency => Build
    ///  * "latest or same chain" dependency => BuildConfig
    /// </summary>
    public class DependencyNode
    {
        public Build HistoricBuild { get; set; }
        public BuildConfig CurrentBuildConfig { get; set; }
        public bool IsCloned { get; set; }

        public DependencyNode(BuildConfig buildConfig)
        {
            CurrentBuildConfig = buildConfig;
        }

        public DependencyNode()
        {
        }

        public override string ToString()
        {
            var buildConfigId = CurrentBuildConfig?.Id ?? HistoricBuild.BuildTypeId;
            var buildNumber = (HistoricBuild != null) ? " | Build #" + HistoricBuild.Number : " | Same chain";
            var cloned = IsCloned ? " | Cloned" : " | Original";

            return buildConfigId + buildNumber + cloned;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Build return false.
            DependencyNode bc = obj as DependencyNode;
            if (bc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (CurrentBuildConfig?.Id == bc.CurrentBuildConfig?.Id && HistoricBuild?.Id == bc.HistoricBuild?.Id);
        }

        public bool Equals(DependencyNode bc)
        {
            // If parameter is null return false:
            if (bc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (CurrentBuildConfig?.Id == bc.CurrentBuildConfig?.Id && HistoricBuild?.Id == bc.HistoricBuild?.Id);
        }

        public override int GetHashCode()
        {
            var hash = 37;
            if (CurrentBuildConfig != null)
            {
                hash ^= CurrentBuildConfig.Id.GetHashCode();
            }
            if (HistoricBuild != null)
            {
                hash ^= HistoricBuild.Id.GetHashCode();
            }
            return hash;
        }
    }
}