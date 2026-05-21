using Inventory.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class ExpenseBudget : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ExpenseCategoryId { get; set; }

        public virtual ExpenseCategory? ExpenseCategory { get; set; }

        public decimal BudgetAmount { get; set; }

        /// <summary>
        /// Computed from ExpenseEntries for this category/month/year
        /// </summary>
        public decimal SpentAmount { get; set; } = 0;

        /// <summary>
        /// Month number (1-12)
        /// </summary>
        [Range(1, 12)]
        public int Month { get; set; }

        /// <summary>
        /// 4-digit year e.g. 2025
        /// </summary>
        public int Year { get; set; }
    }
}
