using RemoteTerminal.Server.Auth;
using SilverAssertions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using TestAuthKey = RemoteTerminal.Server.Auth.AuthKey;

namespace RemoteTerminal.AuthKey.Tests;

public class AuthKeyCryptorTests
{
    private readonly ITestOutputHelper _output;

    private JsonSerializerOptions JsonOptions { get; } =
        new ()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

    public AuthKeyCryptorTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Theory]
    [InlineData("2026-02-01", "2032-12-31", "Authorized_Entity")]
    [InlineData("2026-02-01", null, "Authorized_Entity")]
    //[InlineData("2026-02-01", "2029-12-31", "RemoteTerminalClient")]
    public void can_EncryptKey(string issuedDate, string expiresDate, string authorizedEntity)
    {
        //Arrange
        var issued = DateOnly.Parse(issuedDate);
        DateOnly? expires = (string.IsNullOrWhiteSpace(expiresDate))
            ? null
            : DateOnly.Parse(expiresDate);

        var authKeyToEncrypt = new TestAuthKey(
            IssuedDate: issued, 
            ExpiresDate: expires, 
            AuthorizedEntity: authorizedEntity);

        //Act
        var encryptedText = AuthKeyCryptor.EncryptKey(authKeyToEncrypt);

        //Output
        _output.WriteLine("Encrypted the following key:\n"
            + JsonSerializer.Serialize(authKeyToEncrypt, JsonOptions));
        _output.WriteLine($"\nAs:\n{encryptedText}");

        //Assert
        encryptedText.Should().NotBeNullOrWhiteSpace();
        var decrypted = AuthKeyCryptor.DecryptKeyText(encryptedText);
        decrypted.Should().BeEquivalentTo(authKeyToEncrypt);
        _output.WriteLine("\nUnencrypted from text:\n"
                          + JsonSerializer.Serialize(decrypted, JsonOptions));
    }

    [Theory]
    [InlineData("f2DoXKKXIxAoHKVzxwooK+VXKHLPQ16DYYmLT2N4iDEM/4DQdJ47DlKtwc/YX62HFihcEuOv7h1Aj8yVwbriI8jXJJyYHIXUGyf+LdD0p3glOAaASmIf5ghRevtd", 
        "Authorized_Entity")]
    [InlineData("3YKYC9Beyfq67rLs7WLjrT+w8hUp6b+6cG0nIsbYdDZWguGCtaEsuO8sqKVc7JQ0Fu0iyi62y5LIHmi/+N68kA2GoYZwWSnFT+nh5YHFtoci8w0JFuBglhUsAkcioIxy",
        "RemoteTerminalClient")]
    public void can_DecryptKeyText(string encryptedText, string expectedAuthorizedEntity)
    {
        //Act
        var decrypted = AuthKeyCryptor.DecryptKeyText(encryptedText);

        //Output
        _output.WriteLine($"Decrypted key text:\n{encryptedText}\nAs:\n"
                          + JsonSerializer.Serialize(decrypted, JsonOptions));

        //Assert
        decrypted.AuthorizedEntity.Should().Be(expectedAuthorizedEntity);
    }
}
