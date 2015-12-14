﻿using System.Collections.Generic;
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

        private void AddBuildWithDependencies(GraphNode<Build> parentNode)
        {
            AddNode(parentNode);

            if (parentNode.Value.ArtifactDependencies != null)
            {
                foreach (var artifactDependency in parentNode.Value.ArtifactDependencies)
                {
                    var dependencyBuild = _buildClient.ById(artifactDependency.Id).Result;
                    var childNode = new GraphNode<Build>(dependencyBuild);

                    if (!this.Contains(dependencyBuild))
                    {
                        AddBuildWithDependencies(childNode);
                    }

                    AddDirectedEdge(parentNode, childNode, 0);
                }
            }
        }
    }
}