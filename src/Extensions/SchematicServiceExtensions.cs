using System;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Schematic.Identity;

namespace Schematic.Core.Mvc
{
    public static class SchematicServiceExtensions
    {
        public static IServiceCollection AddSchematic(this IServiceCollection services)
        {
            services.AddScoped<ISchematicSettings, SchematicSettings>();
            services.AddScoped<IPasswordValidator, PasswordValidator>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IEmailValidator, EmailValidator>();
            services.AddScoped<EmailSettings>();
            
            return services;
        }
    }
}