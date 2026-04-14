using FluentValidation;
using Suppliers.Application.Features.Suppliers.Commands;

namespace Suppliers.Application.Features.Suppliers.Validators
{
    public class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
    {
        public CreateSupplierValidator()
        {
            RuleFor(x => x.SupplierData.name).NotEmpty().WithMessage("Supplier name is required.");
        }
    }
}
