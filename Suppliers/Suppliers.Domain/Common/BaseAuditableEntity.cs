using System;

namespace Suppliers.Domain.Common
{
    public abstract class BaseAuditableEntity : IMultiTenant
    {
        public Guid? CompanyId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }    
}
