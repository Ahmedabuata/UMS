using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("guardians");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
        builder.Property(e => e.Relationship).HasColumnName("relationship").HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Guardians)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
