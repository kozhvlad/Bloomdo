using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Infrastructure.DatabaseContexts;

public class LocalDatabaseContext : DbContext
{
    public LocalDatabaseContext(DbContextOptions<LocalDatabaseContext> options) : base(options)
    {
    }

}