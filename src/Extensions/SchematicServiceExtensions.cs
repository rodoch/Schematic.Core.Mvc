using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public static class SchematicServiceExtensions
    {
        public static IServiceCollection AddSchematic(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SchematicSettings>(configuration.GetSection("Schematic"));

            services.AddScoped<IPasswordValidator, PasswordValidator>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IEmailValidator, EmailValidator>();
            
            return services;
        }
    }
}