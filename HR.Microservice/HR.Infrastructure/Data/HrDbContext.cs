using Microsoft.EntityFrameworkCore;
using HR.Domain.Entities;

namespace HR.Infrastructure.Data;

public class HrDbContext : DbContext
{
    public HrDbContext(DbContextOptions<HrDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AdministrativeDepartment> AdministrativeDepartments => Set<AdministrativeDepartment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("branches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.BranchName).HasColumnName("branch_name");
            b.Property(x => x.BranchCode).HasColumnName("branch_code");
            b.Property(x => x.IsActive).HasColumnName("is_active");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.Property(x => x.BranchDescription).HasColumnName("branch_description");
            b.Property(x => x.BranchLocation).HasColumnName("branch_location");
        });

        modelBuilder.Entity<AdministrativeDepartment>(d =>
        {
            d.ToTable("administrative_departments");
            d.HasKey(x => x.Id);
            d.Property(x => x.Id).HasColumnName("id");
            d.Property(x => x.BranchId).HasColumnName("branch_id");
            d.Property(x => x.DepartmentName).HasColumnName("department_name");
            d.Property(x => x.DepartmentCode).HasColumnName("department_code");
            d.Property(x => x.Description).HasColumnName("description");
            d.Property(x => x.IsActive).HasColumnName("is_active");
            d.Property(x => x.CreatedAt).HasColumnName("created_at");
            d.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            d.HasOne(x => x.Branch).WithMany(b => b.Departments).HasForeignKey(x => x.BranchId);
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EmployeeNumber).HasColumnName("employee_number");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.BranchId).HasColumnName("branch_id");
            e.Property(x => x.ContractType).HasColumnName("contract_type");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.HireDate).HasColumnName("hire_date");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        base.OnModelCreating(modelBuilder);
    }
}