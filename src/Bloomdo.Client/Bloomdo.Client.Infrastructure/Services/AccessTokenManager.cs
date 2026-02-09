using Bloomdo.Client.Core.Interfaces;
using Microsoft.Maui.Storage;

namespace Bloomdo.Client.Infrastructure.Services;

public class AccessTokenManager : IAccessTokenManager
{
    public string? AuthToken { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AuthToken);

    public async Task<bool> Login(string email, string password)
    {
        string receivedToken = "jwt_token";

        if (string.IsNullOrEmpty(receivedToken))
        {
            return false;
        }

        AuthToken = receivedToken;

        try
        {
            await SecureStorage.Default.SetAsync("auth_token", AuthToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to save auth token: " + ex.Message);
        }

        return true;
    }

    public async Task TryLoadTokenFromStorage()
    {
        try
        {
            AuthToken = await SecureStorage.Default.GetAsync("auth_token");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load auth token: " + ex.Message);
            AuthToken = null;
        }
    }

    public void Logout()
    {
        AuthToken = null;

        SecureStorage.Default.Remove("auth_token");
    }
}