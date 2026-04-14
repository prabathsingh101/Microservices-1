using MediatR;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest;
