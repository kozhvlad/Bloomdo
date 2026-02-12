using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Data.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.AccountRoles)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.AccountRoles)
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => a.Email == email, cancellationToken);
    }
}