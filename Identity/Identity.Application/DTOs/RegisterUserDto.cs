namespace Identity.Application.DTOs;

public record RegisterUserDto(
    string UserName,
    string Email,
    string Password,
    List<Guid> RoleIds
);
