using MediatR;

public record DeletePurchaseOrderCommand(Guid Id) : IRequest<bool>;
