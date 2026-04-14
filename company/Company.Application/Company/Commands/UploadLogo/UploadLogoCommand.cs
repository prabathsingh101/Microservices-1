using MediatR;

namespace Company.Application.Company.Commands.UploadLogo
{
    public record UploadLogoCommand(Guid Id, string LogoUrl) : IRequest<bool>;
}
