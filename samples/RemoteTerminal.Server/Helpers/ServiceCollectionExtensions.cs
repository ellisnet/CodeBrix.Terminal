using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using RemoteTerminal.Server.Auth;
using System;

namespace RemoteTerminal.Server.Helpers;

public static class ServiceCollectionExtensions
{
    public static void AddAuthenticationPolicies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddAuthentication(SharedKeyAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, SharedKeyAuthHandler>(
                SharedKeyAuthHandler.SchemeName, null);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthSettings.AuthorizedEntityPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(SharedKeyAuthHandler.SchemeName);
                policy.RequireAuthenticatedUser();
            });
        });
    }
}
