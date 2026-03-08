using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RemoteTerminal.Server.Auth;

public record AuthKey(
    [property: JsonPropertyName("iss")] DateOnly IssuedDate,
    [property: JsonPropertyName("exp")] DateOnly? ExpiresDate,
    [property: JsonPropertyName("ent")] string AuthorizedEntity
);

public static class AuthKeyHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly DateOnly IssuedAfterDate = new (2026, 1, 1);

    public static byte[] ToBytes(this AuthKey authKey)
    {
        ArgumentNullException.ThrowIfNull(authKey);

        if (authKey.IssuedDate <= IssuedAfterDate)
        {
            throw new ArgumentException($"IssuedDate must be later than {IssuedAfterDate:M/d/yyyy}", nameof(authKey));
        }

        if (string.IsNullOrWhiteSpace(authKey.AuthorizedEntity))
        {
            throw new ArgumentException("AuthorizedEntity must not be null or whitespace.", nameof(authKey));
        }

        if (authKey.ExpiresDate.HasValue && authKey.ExpiresDate.Value <= authKey.IssuedDate)
        {
            throw new ArgumentException("ExpiresDate, if provided, must be later than the IssuedDate.", nameof(authKey));
        }

        var json = JsonSerializer.Serialize(authKey, SerializerOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    public static AuthKey FromBytes(byte[] authKeyBytes)
    {
        ArgumentNullException.ThrowIfNull(authKeyBytes);

        if (authKeyBytes.Length == 0)
        {
            throw new ArgumentException($"The {nameof(AuthKey)} byte array must not be empty.", nameof(authKeyBytes));
        }

        var json = Encoding.UTF8.GetString(authKeyBytes);

        AuthKey authKey;
        try
        {
            authKey = JsonSerializer.Deserialize<AuthKey>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"The decoded string failed to be deserialized to an instance of {nameof(AuthKey)}: {json}",
                ex);
        }

        if (authKey == null)
        {
            throw new InvalidOperationException(
                $"The decoded string could not be deserialized to an instance of {nameof(AuthKey)}: {json}");
        }

        return authKey;
    }
}
