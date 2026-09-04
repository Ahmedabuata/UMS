using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("faculties");
        builder.ConfigureBase();
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.FacultyName).HasColumnName("faculty_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.FacultyCode).HasColumnName("faculty_code").HasMaxLength(20);
        builder.Property(e => e.DeanName).HasColumnName("dean_name").HasMaxLength(100);
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.HasIndex(e => e.FacultyCode).IsUnique();
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Faculties)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
