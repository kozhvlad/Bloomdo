namespace Bloomdo.Client.Core.Interfaces;

public interface IAccessTokenManager
{
    string? AuthToken { get; }
    bool IsAuthenticated { get; }

    Task<bool> Login(string email, string password);
    void Logout();
    Task TryLoadTokenFromStorage();
}