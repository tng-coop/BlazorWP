using Microsoft.EntityFrameworkCore;

namespace BlazorWP;

public class SqlDemoDbContext : DbContext
{
    public DbSet<DemoRecord> Records => Set<DemoRecord>();

    public SqlDemoDbContext(DbContextOptions<SqlDemoDbContext> options)
        : base(options)
    {
    }
}

public class DemoRecord
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Value { get; set; }
}
