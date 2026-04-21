namespace Identity.Application.DTOs
{
    public record LoginDto(string Email, string Password, string? CompanyCode = null);
}
