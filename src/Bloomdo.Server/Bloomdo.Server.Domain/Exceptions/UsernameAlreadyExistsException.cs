namespace Bloomdo.Server.Domain.Exceptions;

public class UsernameAlreadyExistsException : DomainException
{
    public UsernameAlreadyExistsException(string username)
        : base($"Username '@{username}' is already taken")
    {
        Username = username;
    }

    public string Username { get; }
}
