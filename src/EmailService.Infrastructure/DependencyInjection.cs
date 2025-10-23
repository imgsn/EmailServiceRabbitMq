using EmailService.Application.Common.Interfaces;
using EmailService.Infrastructure.Messaging;
using EmailService.Infrastructure.Persistence;
using EmailService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        // Register DbContext as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Services - Register TenantContext as Scoped
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();

        return services;
    }
}