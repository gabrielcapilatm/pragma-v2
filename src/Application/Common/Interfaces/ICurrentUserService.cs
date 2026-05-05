namespace FinancialApi.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    string? TenantCode { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
