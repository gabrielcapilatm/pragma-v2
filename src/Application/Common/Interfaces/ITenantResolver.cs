namespace FinancialApi.Application.Common.Interfaces;

using FinancialApi.Domain.ValueObjects;

public interface ITenantResolver
{
    Tenant GetCurrentTenant();
}
