using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("classrooms");
        builder.ConfigureBase();
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.RoomNumber).HasColumnName("room_number").HasMaxLength(20).IsRequired();
        builder.Property(e => e.BuildingName).HasColumnName("building_name").HasMaxLength(50);
        builder.Property(e => e.Capacity).HasColumnName("capacity");
        builder.Property(e => e.RoomType).HasColumnName("room_type")
            .HasVarcharEnumConversion<RoomType>()
            .HasMaxLength(20);
        builder.HasIndex(e => new { e.BranchId, e.RoomNumber }).IsUnique();
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Classrooms)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
