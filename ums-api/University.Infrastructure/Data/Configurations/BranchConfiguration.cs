using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.ConfigureBase();
        builder.Property(e => e.BranchName).HasColumnName("branch_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.BranchCode).HasColumnName("branch_code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.BranchLocation).HasColumnName("branch_location").HasMaxLength(200);
        builder.Property(e => e.BranchDescription).HasColumnName("branch_description").HasMaxLength(500);
        builder.HasIndex(e => e.BranchCode).IsUnique();
    }
}
