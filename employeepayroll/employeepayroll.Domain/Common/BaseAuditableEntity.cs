using System;

namespace employeepayroll.Domain.Common
{
    public abstract class BaseAuditableEntity : IMultiTenant
    {
        public Guid? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public Guid? ModifiedBy { get; set; }
    }
}
