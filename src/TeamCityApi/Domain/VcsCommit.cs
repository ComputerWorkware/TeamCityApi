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
        public string BranchName { get; set; }
        public GitAuthenticationType AuthenticationType { get; set; }
        public string VcsRootId { get; set; }

        public VcsCommit(VcsRoot vcsRoot, string commitHash)
        {
            CommitSha = commitHash;
            AuthenticationType = GitAuthenticationType.Http;
            VcsRootId = vcsRoot.VcsRootId;

            Property property = vcsRoot.Properties.Property.FirstOrDefault(x => x.Name == "url");
            if (property != null)
            {
                RepositoryLocation = property.Value;
            }

            property = vcsRoot.Properties.Property.FirstOrDefault(x => x.Name == "authMethod");
            if (property != null)
            {
                AuthenticationType = property.Value == "PASSWORD" ? GitAuthenticationType.Http : GitAuthenticationType.Ssh;
            }

            property = vcsRoot.Properties.Property.FirstOrDefault(x => x.Name == "branch");
            if (property != null)
            {
                BranchName = property.Value;
            }

        }
    }
}