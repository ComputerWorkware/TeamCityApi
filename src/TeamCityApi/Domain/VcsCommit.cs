using System.Linq;

namespace TeamCityApi.Domain
{
    public enum GitAuthenticationType
    {
        Ssh = 1,
        Http = 2
    }

    public class VcsCommit
    {
        public string CommitSha { get; set; }
        public string RepositoryLocation { get; set; }
        public string RepositoryNameWithNamespace { get; set; }
        public string BranchName { get; set; }
        public GitAuthenticationType AuthenticationType { get; set; }
        public string VcsRootId { get; set; }

        /// <summary>
        /// Encapsulate Repo location, Branch and Commit.
        /// Since VcsRoot contains parameter placeholders, build parameters needed to generate end values
        /// </summary>
        /// <param name="vcsRoot"></param>
        /// <param name="parameters"></param>
        /// <param name="commitHash"></param>
        public VcsCommit(VcsRoot vcsRoot, PropertyList parameters, string commitHash)
        {
            CommitSha = commitHash;
            AuthenticationType = GitAuthenticationType.Http;
            VcsRootId = vcsRoot.VcsRootId;

            Property property = vcsRoot.Properties.Property.FirstOrDefault(x => x.Name == "url");
            if (property != null)
            {
                RepositoryLocation = parameters.ReplaceInString(property.Value);
            }

            property = parameters.FirstOrDefault(x => x.Name == "git.repo.path");
            if (property != null)
            {
                RepositoryNameWithNamespace = parameters.ReplaceInString(property.Value);
            }

            property = vcsRoot.Properties.Property.FirstOrDefault(x => x.Name == "authMethod");
            if (property != null)
            {
                AuthenticationType = property.Value == "PASSWORD" ? GitAuthenticationType.Http : GitAuthenticationType.Ssh;
            }

            property = vcsRoot.Properties.Property.FirstOrDefault(x => x.Name == "branch");
            if (property != null)
            {
                BranchName = parameters.ReplaceInString(property.Value);
            }

        }
    }
}