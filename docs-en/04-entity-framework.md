# Entity Framework

## Introduction

Entity Framework (EF) Core is an ORM (Object-Relational Mapper) used to map domain objects to relational database structures. It allows the application to interact with the database using C# code, abstracting the need to write raw SQL in most cases.

> **On LATAM Platform V2**, EF Core is the **official standard** for relational data access, aligned with the Clean Architecture and DDD-based architecture.

---

## How Entity Framework Core Works

### DbContext

`DbContext` is the main database access class.

**Responsibilities:**
- Manage the database connection
- Track entity state (change tracking)
- Execute queries
- Persist changes

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

### DbSet

Represents a collection of entities in the database. Examples:
- `Transactions`
- `Users`

```csharp
// Accessing a DbSet
var activeTransactions = await _context.Transactions
    .Where(t => t.Status == TransactionStatus.Active)
    .ToListAsync();
```

### Entities

Entities are classes that represent domain data.

**Important rules:**
- Must not depend on EF Core directly
- Must represent the domain, not the database structure

```csharp
// Domain/Entities/Transaction.cs
public class Transaction
{
    public Guid Id { get; private set; }
    public string TransactionNumber { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { } // For EF

    public static Transaction Create(string transactionNumber, decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = transactionNumber,
            Amount = amount,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be approved");

        Status = TransactionStatus.Approved;
    }
}
```

### Change Tracking

EF Core tracks entity changes automatically, enabling:
- Automatic updates on `SaveChangesAsync()`
- State management (Added, Modified, Deleted)

```csharp
// EF tracks changes automatically
var transaction = await _context.Transactions.FindAsync(id);
transaction.Approve(); // Changes state
await _context.SaveChangesAsync(); // Persists the change
```

**Disable tracking for read-only queries (better performance):**

```csharp
var transactions = await _context.Transactions
    .AsNoTracking()
    .Where(t => t.CreatedAt > DateTime.UtcNow.AddDays(-30))
    .ToListAsync();
```

### LINQ

LINQ queries written in C# are translated to SQL by EF Core.

```csharp
var result = await _context.Users
    .Where(u => u.Country == "BR" && u.IsActive)
    .OrderByDescending(u => u.CreatedAt)
    .Select(u => new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email
    })
    .ToListAsync();

// EF generates:
// SELECT u.Id, u.Name, u.Email
// FROM Users u
// WHERE u.Country = 'BR' AND u.IsActive = 1
// ORDER BY u.CreatedAt DESC
```

---

## How We Use EF Core in the Project

| Aspect | Implementation |
|--------|---------------|
| Purpose | Standard relational persistence |
| Architecture | Respects layered separation (DDD + Clean Architecture) |
| Location | Isolated in the **Infrastructure** layer |
| Domain boundary | EF Core must not leak into Domain or API layers |

### Layer structure

```
FinancialApi.Domain
├── Entities/
│   └── Transaction.cs
└── Repositories/
    └── ITransactionRepository.cs

FinancialApi.Application
└── Services/
    ├── ITransactionService.cs
    └── TransactionService.cs

FinancialApi.Infrastructure
└── Persistence/
    └── Repositories/
        └── TransactionRepository.cs  (implements ITransactionRepository)
```

---

## How to Implement

### 1. Create the DbContext

The `DbContext` represents the database connection.

**Best practices:**
- Keep it simple
- No business logic
- Configuration and access only

```csharp
// Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

### 2. Define Entities

Entities must:
- Represent the domain
- Have a clear identity
- Not depend on EF Core

**Avoid:**
- Infrastructure attributes on domain classes
- Coupling to database structure

### 3. Configure Mappings

Mappings must be **separated from the entity** using `IEntityTypeConfiguration<T>` classes.

```csharp
// Infrastructure/Persistence/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.Property(u => u.Country).IsRequired().HasMaxLength(2);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => new { u.Country, u.IsActive });
    }
}
```

**Mapping with relationships:**

```csharp
// Infrastructure/Persistence/Configurations/TransactionConfiguration.cs
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionNumber).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Status).HasConversion<string>();

        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TransactionNumber).IsUnique();
    }
}
```

### 4. Dependency Injection

Register `DbContext` via DI to enable lifecycle control, testability, and decoupling.

```csharp
// Registration (Infrastructure/DependencyInjection.cs)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null
        )
    )
);

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
```

**Usage in a service:**

```csharp
public class TransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Transaction> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

---

## Migrations

Migrations are used to **version schema changes**.

### How They Work

- Each change to the model generates a migration
- EF Core generates the SQL scripts automatically
- Enables incremental schema evolution

### Project Rules

| Rule | Description |
|------|-------------|
| Every structural change must become a migration | Never modify the database manually |
| Migrations must be in the repository | Version control is mandatory |
| Do not modify applied migrations | Create a new migration to fix mistakes |
| Use descriptive names | e.g., `AddTransactionStatusColumn` |

### Migration Workflow

**1. Update the model:**
```csharp
public class Transaction
{
    // ... existing properties
    public string Description { get; private set; } // NEW
}
```

**2. Generate the migration:**
```bash
dotnet ef migrations add AddDescriptionToTransaction \
  --project Infrastructure \
  --startup-project Api
```

**3. Review the generated migration:**
```csharp
public partial class AddDescriptionToTransaction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Transactions",
            type: "varchar(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Description",
            table: "Transactions");
    }
}
```

**4. Apply the migration:**
```bash
# Development
dotnet ef database update --project Infrastructure --startup-project Api

# Production (via CI/CD using migration bundles)
dotnet ef migrations bundle --output ./efbundle
./efbundle --connection "Server=prod;Database=LatamDB;..."
```

---

## Queries

EF Core is the default query mechanism.

### Recommended for

- Simple and medium complexity queries
- Transactional operations
- Common filters and joins

**Repository example:**

```csharp
public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Transaction>> GetByCountryAsync(string country)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.User.Country == country)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }
}
```

### When to Use Raw SQL

- Very complex queries
- Performance-critical paths where EF-generated SQL is suboptimal
- EF Core limitations

```csharp
/// <summary>
/// Fetches active transactions from the last month grouped by country.
/// Performance: ~200ms for 10k records.
/// Required index: IX_Transactions_CreatedAt_Status
/// </summary>
public async Task<IEnumerable<TransactionSummary>> GetMonthlySummaryAsync(int year, int month)
{
    var sql = @"
        SELECT CAST(CreatedAt AS DATE) as Date,
               COUNT(*) as TotalTransactions,
               SUM(Amount) as TotalAmount
        FROM Transactions
        WHERE YEAR(CreatedAt) = {0} AND MONTH(CreatedAt) = {1}
        GROUP BY CAST(CreatedAt AS DATE)
        ORDER BY Date";

    return await _context.Database
        .SqlQueryRaw<TransactionSummary>(sql, year, month)
        .ToListAsync();
}
```

---

## Performance

EF Core abstracts SQL but does not eliminate performance problems.

### Best Practices

| Practice | Implementation |
|----------|---------------|
| Avoid loading unnecessary data | Use `Select()` for projections |
| Prefer projections over full entities | Map to DTOs, not domain objects |
| Use optimized reads | `AsNoTracking()` when not updating |
| Avoid multiple queries | Use `Include()` for eager loading |

### Common Problem: N+1 Queries

**Bad (N+1 — one query per user):**
```csharp
var users = await _context.Users.ToListAsync();
foreach (var user in users)
{
    var transactions = await _context.Transactions
        .Where(t => t.UserId == user.Id)
        .ToListAsync();
}
```

**Better (1 query with Include):**
```csharp
var users = await _context.Users
    .Include(u => u.Transactions)
    .ToListAsync();
```

**Best (projection — only the data you need):**
```csharp
var users = await _context.Users
    .Select(u => new UserWithTransactionsDto
    {
        UserId = u.Id,
        UserName = u.Name,
        TransactionCount = u.Transactions.Count,
        TotalAmount = u.Transactions.Sum(t => t.Amount)
    })
    .ToListAsync();
```

---

## Multi-tenant Implications

The platform uses per-country database isolation.

**EF Core implications:**
- The `DbContext` must connect to the correct database for the current tenant
- No operation may occur without a resolved tenant context
- The connection string must be resolved dynamically per request

```csharp
// Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var tenant = _currentUser.Tenant;
            var connectionString = _configuration.GetConnectionString(tenant);
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}
```

---

## Best Practices Summary

- Use EF Core as the standard for relational data access
- Keep `DbContext` simple and focused
- Separate entity configuration into `IEntityTypeConfiguration<T>` classes
- Always use migrations for structural changes
- Avoid business logic in the persistence layer
- Keep queries explicit and readable
- Use `AsNoTracking()` for read queries
- Apply indexes on frequently filtered columns
- Document complex queries with their performance characteristics

## What to Avoid

- Exposing entities directly in the API
- Mixing domain and infrastructure
- Using EF Core inconsistently across projects
- Creating generic repositories without a clear need
- Using Raw SQL without justification
- Ignoring performance impact
- Lazy loading inside loops
- Modifying already-applied migrations
- Hardcoded connection strings
