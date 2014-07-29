using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamCityConsole.Model
{
    public class DependencyConfig
    {
        public string BuildConfigId { get; set; }

        public List<BuildInfo> BuildInfos { get; set; }

        public DependencyConfig()
        {
            BuildInfos = new List<BuildInfo>();
        }

        protected bool Equals(DependencyConfig other)
        {
            return string.Equals(BuildConfigId, other.BuildConfigId)
                   && BuildInfos.Count == other.BuildInfos.Count
                   && BuildInfos.Except(other.BuildInfos).Any() == false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DependencyConfig) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (BuildConfigId != null ? BuildConfigId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (BuildInfos != null ? BuildInfos.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}