using MediatR;

public class CreatePOResponse
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = null!;
}

public record CreatePurchaseOrderCommand(CreatePurchaseOrderDto PoData) : IRequest<CreatePOResponse>;
