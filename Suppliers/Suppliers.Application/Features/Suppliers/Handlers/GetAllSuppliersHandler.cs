using MediatR;

public class GetAllSuppliersHandler : IRequestHandler<GetAllSuppliersQuery, IEnumerable<SupplierDto>>
{
    private readonly ISupplierRepository _repository;

    public GetAllSuppliersHandler(ISupplierRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SupplierDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await _repository.GetAllAsync();

        // Mapping Entity to DTO
        return suppliers.Select(s => new SupplierDto(
            s.Id,
            s.Name,
            s.Phone,
            s.GstIn,
            s.Address,
            s.Email,
            s.IsActive,
            s.CreatedBy,
            s.DefaultPriceListId
        ));
    }
}