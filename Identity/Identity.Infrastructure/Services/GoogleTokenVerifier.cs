using Identity.Application.Interfaces;
using System.Text.Json;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Verifies Google ID Token by calling Google's tokeninfo endpoint.
/// No extra NuGet package needed — uses plain HTTP call.
/// </summary>
public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public GoogleTokenVerifier(
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo?> VerifyAsync(string idToken)
    {
        try
        {
            // Google's token verification endpoint
            var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}";

            var client = _httpClientFactory.CreateClient("GoogleAuth");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GOOGLE-VERIFIER] Token verification failed. Status: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GOOGLE-VERIFIER] Token response received.");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Validate: audience must match our Client ID
            var expectedClientId = _configuration["Google:ClientId"];
            if (!string.IsNullOrEmpty(expectedClientId))
            {
                var aud = root.TryGetProperty("aud", out var audProp) ? audProp.GetString() : null;
                if (aud != expectedClientId)
                {
                    Console.WriteLine($"[GOOGLE-VERIFIER] Audience mismatch. Expected: {expectedClientId}, Got: {aud}");
                    return null;
                }
            }

            // Extract user info from token payload
            var sub   = root.TryGetProperty("sub",   out var subProp)   ? subProp.GetString()   : null;
            var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var name  = root.TryGetProperty("name",  out var nameProp)  ? nameProp.GetString()  : null;
            var pic   = root.TryGetProperty("picture", out var picProp) ? picProp.GetString()   : null;

            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
            {
                Console.WriteLine("[GOOGLE-VERIFIER] Missing sub or email in token.");
                return null;
            }

            return new GoogleUserInfo
            {
                GoogleId = sub,
                Email    = email,
                Name     = name ?? email.Split('@')[0],
                Picture  = pic
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GOOGLE-VERIFIER] Exception: {ex.Message}");
            return null;
        }
    }
}
