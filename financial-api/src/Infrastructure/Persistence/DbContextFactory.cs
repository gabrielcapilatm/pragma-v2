namespace FinancialApi.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FinancialApi.Application.Common.Interfaces;

public class DbContextFactory
{
    private readonly ConnectionStringResolver _connectionStringResolver;
    private readonly ITenantResolver _tenantResolver;

    public DbContextFactory(
        ConnectionStringResolver connectionStringResolver,
        ITenantResolver tenantResolver)
    {
        _connectionStringResolver = connectionStringResolver;
        _tenantResolver = tenantResolver;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var tenant = _tenantResolver.GetCurrentTenant();
        var connectionString = _connectionStringResolver.GetConnectionString(tenant);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
