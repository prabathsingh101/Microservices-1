using MediatR;
using System;

namespace Inventory.Application.GRN.Command
{
    public class CancelGRNCommand : IRequest<bool>
    {
        public Guid GrnId { get; set; }
        public string CancelledBy { get; set; } = string.Empty;
    }
}
