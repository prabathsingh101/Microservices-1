using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Services
{
    public interface IPdfService
    {
        byte[] Convert(string htmlContent);
    }
}
