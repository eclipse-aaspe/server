
namespace AasxServerBlazor.Interfaces
{
    public interface IUserService
    {
        bool ValidateCredentials(string username, string password);
    }
}

