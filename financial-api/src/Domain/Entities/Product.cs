namespace FinancialApi.Domain.Entities;

using FinancialApi.Domain.Common;

public class Product : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Category { get; private set; } = string.Empty;

    private Product() { }

    public static Product Create(string name, decimal price, string category)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Category = category
        };
    }
}
