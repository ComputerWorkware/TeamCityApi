using System.Linq;
using TeamCityApi.Domain;

namespace TeamCityApi.Model
{
    public class BuildInfo
    {
        public long Id { get; set; }
        public string Number { get; set; }
        public string BuildConfigId { get; set; }
        public string CommitHash { get; set; }

        public static BuildInfo FromBuild(Build build)
        {
            return new BuildInfo
            {
                Id = build.Id,
                Number = build.Number,
                BuildConfigId = build.BuildTypeId,
                CommitHash = build.Revisions.Any() ? build.Revisions[0].Version : string.Empty
            };
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Number: {1}, BuildConfigId: {2}", Id, Number, BuildConfigId);
        }

        protected bool Equals(BuildInfo other)
        {
            return string.Equals(Id, other.Id) && string.Equals(Number, other.Number) && string.Equals(BuildConfigId, other.BuildConfigId) && string.Equals(CommitHash, other.CommitHash);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BuildInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Id.GetHashCode();
                hashCode = (hashCode * 397) ^ (Number != null ? Number.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BuildConfigId != null ? BuildConfigId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CommitHash != null ? CommitHash.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}