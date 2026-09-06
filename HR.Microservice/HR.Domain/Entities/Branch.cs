using System;
using System.Collections.Generic;

namespace HR.Domain.Entities
{
    public class Branch
    {
        public Guid Id { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? BranchDescription { get; set; }
        public string? BranchLocation { get; set; }
        public ICollection<AdministrativeDepartment> Departments { get; set; } = new List<AdministrativeDepartment>();
    }
}