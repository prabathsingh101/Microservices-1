using FluentValidation;

namespace Identity.Application.Queries.LoginUser;

public class LoginUserQueryValidator
    : AbstractValidator<LoginUserQuery>
{
    public LoginUserQueryValidator()
    {
        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage("Login details are required.");

        RuleFor(x => x.Dto.Email)
            .NotEmpty()
            .EmailAddress()
            .When(x => x.Dto != null);

        RuleFor(x => x.Dto.Password)
            .NotEmpty()
            .When(x => x.Dto != null);
    }
}
