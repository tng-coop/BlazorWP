using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWP;

public static class SqlDemoSetup
{
    public static IServiceCollection AddSqlDemo(this IServiceCollection services)
    {
        var connection = new SqliteConnection("Data Source=sql-demo;Mode=Memory;Cache=Shared");
        connection.Open();

        services.AddSingleton(connection);
        services.AddDbContextFactory<SqlDemoDbContext>(options => options.UseSqlite(connection));

        using var context = new SqlDemoDbContext(new DbContextOptionsBuilder<SqlDemoDbContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();

        return services;
    }
}
