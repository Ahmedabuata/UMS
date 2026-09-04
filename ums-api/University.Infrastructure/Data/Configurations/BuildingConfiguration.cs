using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings");
        builder.ConfigureBase();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);
        builder.Property(e => e.Floors).HasColumnName("floors");
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Classrooms)
            .WithOne(c => c.Building)
            .HasForeignKey(c => c.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}