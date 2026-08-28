using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testing.Application.Abstractions.Data;
using Testing.Infrastructure.Persistence;
using Testing.Infrastructure.Persistence.Zam;

namespace Testing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ZemogDB")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ZemogDB'.");

        services.AddDbContextFactory<ZemogContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        return services;
    }
}
