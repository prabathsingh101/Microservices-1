using Inventory.Application.GRN.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.Queries
{
    public record GetPOForGRNQuery(string poIds, Guid? GrnHeaderId = null, string? GatePassNo = null) : IRequest<POForGRNDTO?>;
}
