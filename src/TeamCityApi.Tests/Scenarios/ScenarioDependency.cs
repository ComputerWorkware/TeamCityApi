using System;
using TeamCityApi.Domain;
using TeamCityApi.Tests.Helpers;

namespace TeamCityApi.Tests.Scenarios
{
	public enum DependencyType
	{
		ArtifactSameChain,
		ArtifactFixedBuild
	}
	public class ScenarioDependency
	{
		public SingleBuildScenario SingleBuildScenario { get; set; }

		public DependencyType DependencyType { get; set; }

		public ScenarioDependency(SingleBuildScenario singleBuildScenario, DependencyType dependencyType)
		{
			SingleBuildScenario = singleBuildScenario;
			DependencyType = dependencyType;
		}

		public Dependency AsDependency()
		{
			return BuildDependencyGenerator.Artifact(SingleBuildScenario.Build);
		}

		public DependencyDefinition AsDependencyDefinition()
		{
			switch (DependencyType)
			{
				case DependencyType.ArtifactSameChain:
					return BuildConfigDependencyGenerator.ArtifactSameChain(SingleBuildScenario.BuildConfig);
				case DependencyType.ArtifactFixedBuild:
					return BuildConfigDependencyGenerator.ArtifactFixedBuild(SingleBuildScenario.Build);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}