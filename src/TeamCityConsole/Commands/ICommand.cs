using System.Threading.Tasks;
using TeamCityApi.Clients;

namespace TeamCityConsole.Commands
{
    public interface ICommand
    {
        Task Execute(object options);
    }
}