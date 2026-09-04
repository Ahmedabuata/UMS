using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("modules");
        builder.ConfigureBase();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.BranchCode).HasDatabaseName("idx_modules_branch_code");
        builder.HasMany(e => e.Permissions)
            .WithOne()
            .HasForeignKey(p => p.ModuleCode)
            .HasPrincipalKey(m => m.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
