using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Client.Infrastructure.DatabaseContexts;

public class LocalDatabaseContext : DbContext
{
    public LocalDatabaseContext(DbContextOptions<LocalDatabaseContext> options) : base(options)
    {
    }

}