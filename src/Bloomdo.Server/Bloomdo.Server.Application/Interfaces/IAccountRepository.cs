using Bloomdo.Server.Domain.Entities;

namespace Bloomdo.Server.Application.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeAccountId = null, CancellationToken cancellationToken = default);
}
