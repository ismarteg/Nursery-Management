using Microsoft.EntityFrameworkCore;

namespace NurseryManagement.Api.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration.GetValue<string>("Database:ConnectionString")
            ?? "Data Source=nursery.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        return services;
    }
}
