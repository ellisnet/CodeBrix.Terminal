using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Reflection;

namespace RemoteTerminal.Server.Helpers;

public static class WebApplicationExtensions
{
    private const string HubHostingSection = "HubHosting";
    private const string HubHostingUrlSetting = "BaseHostingUrl";
    private const string HubHostingTcpPortSetting = "HostingTcpPort";

    private static readonly MethodInfo MapHubMethod =
        typeof(HubEndpointRouteBuilderExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub)
                        && m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType == typeof(string));

    public static void MapAllHubs(this WebApplication app, Type assemblyType)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assemblyType);

        var hubTypes = assemblyType.Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsSubclassOf(typeof(Hub)));

        foreach (var hubType in hubTypes)
        {
            var name = hubType.Name;

            if (name.EndsWith("Hub", StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^3];
            }

            var pattern = $"/{name.ToLowerInvariant()}";

            MapHubMethod.MakeGenericMethod(hubType).Invoke(null, [app, pattern]);
        }
    }

    public static void AddHostingUrl(this WebApplication app, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(config);

        var baseUrl = config.GetValue<string>($"{HubHostingSection}:{HubHostingUrlSetting}");
        var port = config.GetValue<int>($"{HubHostingSection}:{HubHostingTcpPortSetting}");
        app.Urls.Add($"{baseUrl}:{port}");
    }
}
