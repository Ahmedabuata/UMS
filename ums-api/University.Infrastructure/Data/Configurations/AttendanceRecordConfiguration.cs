using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");
        builder.ConfigureBase();
        builder.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(e => e.AttendanceDate).HasColumnName("attendance_date").IsRequired();
        builder.Property(e => e.IsPresent).HasColumnName("is_present").IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(255);
        builder.HasIndex(e => new { e.EnrollmentId, e.AttendanceDate }).IsUnique();
        builder.HasOne(e => e.Enrollment)
            .WithMany(en => en.AttendanceRecords)
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
