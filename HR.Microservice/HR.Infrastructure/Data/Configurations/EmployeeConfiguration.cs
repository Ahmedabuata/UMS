using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HR.Domain.Entities;
namespace HR.Infrastructure.Data.Configurations;
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ExternalUserId).IsRequired();
        builder.HasIndex(e => e.ExternalUserId).IsUnique();
        builder.Property(e => e.EmployeeNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(e => e.EmployeeNumber).IsUnique();
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
    }
}
