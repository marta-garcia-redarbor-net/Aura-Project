using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aura.Infrastructure.Adapters.Persistence;

internal sealed class AuraDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuraDbContext>
{
    public AuraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuraDbContext>();
        optionsBuilder.UseSqlite("Data Source=aura-ef-design-time.db");
        return new AuraDbContext(optionsBuilder.Options);
    }
}
