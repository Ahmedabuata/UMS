using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class GraduationRequestConfiguration : IEntityTypeConfiguration<GraduationRequest>
{
    public void Configure(EntityTypeBuilder<GraduationRequest> builder)
    {
        builder.ToTable("graduation_requests");
        builder.ConfigureBase();
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.RequestDate).HasColumnName("request_date");
        builder.Property(e => e.ExpectedGraduationDate).HasColumnName("expected_graduation_date");
        builder.Property(e => e.Status).HasColumnName("status")
            .HasVarcharEnumConversion<GraduationStatus>()
            .HasMaxLength(20);
        builder.Property(e => e.ClearanceStatus).HasColumnName("clearance_status")
            .HasVarcharEnumConversion<ClearanceStatus>()
            .HasMaxLength(20);
        builder.Property(e => e.GpaAtRequest).HasColumnName("gpa_at_request").HasPrecision(4, 2);
        builder.Property(e => e.TotalCreditsAtRequest).HasColumnName("total_credits_at_request");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.HasOne(e => e.Student)
            .WithMany(s => s.GraduationRequests)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
