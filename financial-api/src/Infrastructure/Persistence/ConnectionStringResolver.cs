namespace FinancialApi.Infrastructure.Persistence;

using Microsoft.Extensions.Configuration;
using FinancialApi.Domain.ValueObjects;

public class ConnectionStringResolver
{
    private readonly IConfiguration _configuration;

    public ConnectionStringResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(Tenant tenant)
    {
        var connectionString = _configuration.GetConnectionString(tenant.Code);

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                $"Connection string not found for tenant: {tenant.Code}");

        return connectionString;
    }
}
