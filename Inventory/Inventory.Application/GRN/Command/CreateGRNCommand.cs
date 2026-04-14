using Inventory.Application.GRN.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.Command
{
    public record CreateGRNCommand(SaveGRNCommandDTO Data) : IRequest<string>;
}
