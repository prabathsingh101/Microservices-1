using Company.Application.Common.Interfaces;
using Company.Application.Company.Commands.UploadLogo;
using MediatR;

namespace Company.Application.Company.Handler
{
    public class UploadLogoHandler : IRequestHandler<UploadLogoCommand, bool>
    {
        private readonly ICompanyRepository _repo;

        public UploadLogoHandler(ICompanyRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(UploadLogoCommand request, CancellationToken cancellationToken)
        {
            var profile = await _repo.GetByIdAsync(request.Id);
            if (profile == null) return false;

            profile.LogoUrl = request.LogoUrl;
            var result = await _repo.UpsertCompanyProfileAsync(profile);
            
            return result != Guid.Empty;
        }
    }
}
