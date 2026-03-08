using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RemoteTerminal.Server.Helpers;

namespace RemoteTerminal.Server;

internal class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSignalR();

        builder.Services.AddAuthenticationPolicies();

        var app = builder.Build();

        app.AddHostingUrl(app.Configuration);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAllHubs(assemblyType: typeof(Program));

        app.Run();
    }
}
