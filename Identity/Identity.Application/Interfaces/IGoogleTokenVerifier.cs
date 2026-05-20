namespace Identity.Application.Interfaces;

/// <summary>
/// Verifies a Google ID token and extracts user info from it.
/// </summary>
public interface IGoogleTokenVerifier
{
    /// <summary>
    /// Verifies the Google ID token and returns user payload.
    /// Returns null if the token is invalid.
    /// </summary>
    Task<GoogleUserInfo?> VerifyAsync(string idToken);
}

public class GoogleUserInfo
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
}
