using System.Threading.Tasks;
using TeamCityApi.Clients;
using TeamCityApi.Domain;

namespace TeamCityConsole.Commands
{
    public interface ICommand
    {
        Task Execute(object options);
    }

    public class BuildInfo
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public string BuildTypeId { get; set; }

        public static BuildInfo FromBuild(Build build)
        {
            return new BuildInfo
            {
                Id = build.Id,
                Number = build.Number,
                BuildTypeId = build.BuildTypeId
            };
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Number: {1}, BuildTypeId: {2}", Id, Number, BuildTypeId);
        }
    }
}