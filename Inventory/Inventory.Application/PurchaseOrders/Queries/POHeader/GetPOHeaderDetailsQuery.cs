using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;

public record GetPOHeaderDetailsQuery(Guid PurchaseOrderId) : IRequest<POHeaderDetailsDto>;
