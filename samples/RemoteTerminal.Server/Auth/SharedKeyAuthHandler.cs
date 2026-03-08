using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace RemoteTerminal.Server.Auth;

public class SharedKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = nameof(AuthKey);

    //Authorized AuthKey.AuthorizedEntity values for the AuthorizedEntityPolicy
    public readonly string[] AuthorizedEntityPolicyEntities =
    [
        "RemoteTerminalClient"
    ];

    public SharedKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // For SignalR WebSocket connections, the token is sent as a query string parameter
        string authToken = null;

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                authToken = headerValue["Bearer ".Length..].Trim();
            }
        }

        // Fall back to query string (used by SignalR WebSocket transport)
        if (string.IsNullOrWhiteSpace(authToken))
        {
            authToken = Request.Query["access_token"];
        }

        if (string.IsNullOrWhiteSpace(authToken))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        AuthKey authKey;

        try
        {
            authKey = AuthKeyCryptor.DecryptKeyText(encrypted: authToken.Trim());
        }
        catch (Exception)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid or corrupted auth token."));
        }

        if (authKey == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Auth token could not be decrypted."));
        }

        // Confirm IssuedDate is after the minimum allowed date
        if (authKey.IssuedDate <= AuthKeyHelper.IssuedAfterDate)
        {
            return Task.FromResult(AuthenticateResult.Fail("Auth key has an invalid issued date."));
        }

        // Confirm IssuedDate is not in the future
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (authKey.IssuedDate > today)
        {
            return Task.FromResult(AuthenticateResult.Fail("Auth key has an issued date in the future."));
        }

        // Confirm AuthorizedEntity is valid and in the allowed list
        if (string.IsNullOrWhiteSpace(authKey.AuthorizedEntity))
        {
            return Task.FromResult(AuthenticateResult.Fail("Auth key is missing an authorized entity."));
        }

        if (Array.IndexOf(AuthorizedEntityPolicyEntities, authKey.AuthorizedEntity) < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Auth key entity is not authorized."));
        }

        // Confirm ExpiresDate, if provided, is later than today
        if (authKey.ExpiresDate.HasValue && authKey.ExpiresDate.Value <= today)
        {
            return Task.FromResult(AuthenticateResult.Fail("Auth key has expired."));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "AuthenticatedClient") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
