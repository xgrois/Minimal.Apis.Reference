using Microsoft.AspNetCore.Authentication;

namespace Library.Api.Auth;

public class ApiKeyAuthSchemeOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; } = "ApI@KeY"; // Should be called from Azure Key-Vault or similar
}