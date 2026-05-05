namespace FinancialApi.Domain.ValueObjects;

public sealed class Tenant
{
    public string Code { get; }

    private Tenant(string code) => Code = code;

    public static Tenant Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tenant code cannot be empty.", nameof(code));

        var upperCode = code.ToUpperInvariant();

        if (!upperCode.All(char.IsLetter))
            throw new ArgumentException($"Tenant code must contain only letters: '{code}'.", nameof(code));

        return new Tenant(upperCode);
    }

    public override string ToString() => Code;
    public override bool Equals(object? obj) => obj is Tenant other && Code == other.Code;
    public override int GetHashCode() => Code.GetHashCode();
}
