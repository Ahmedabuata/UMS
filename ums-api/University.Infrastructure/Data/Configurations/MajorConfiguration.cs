using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class MajorConfiguration : IEntityTypeConfiguration<Major>
{
    public void Configure(EntityTypeBuilder<Major> builder)
    {
        builder.ToTable("majors");
        builder.ConfigureBase();
        builder.Property(e => e.DepartmentId).HasColumnName("department_id");
        builder.Property(e => e.MajorName).HasColumnName("major_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.MajorCode).HasColumnName("major_code").HasMaxLength(20);
        builder.Property(e => e.TotalCreditHours).HasColumnName("total_credit_hours");
        builder.HasIndex(e => e.MajorCode).IsUnique();
        builder.HasOne(e => e.Department)
            .WithMany(d => d.Majors)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
