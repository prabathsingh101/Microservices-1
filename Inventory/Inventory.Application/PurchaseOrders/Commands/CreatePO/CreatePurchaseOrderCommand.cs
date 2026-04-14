using MediatR;

public record CreatePurchaseOrderCommand(CreatePurchaseOrderDto PoData) : IRequest<bool>;
