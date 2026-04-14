// Application/Commands/BulkDeletePOItemsCommand.cs
using MediatR;

public record BulkDeletePOItemsCommand(Guid PurchaseOrderId, List<Guid> ItemIds) : IRequest<bool>;
