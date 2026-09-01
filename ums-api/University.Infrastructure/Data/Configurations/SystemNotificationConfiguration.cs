using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;
using University.Shared.Enums;

namespace University.Infrastructure.Data.Configurations;

public class SystemNotificationConfiguration : IEntityTypeConfiguration<SystemNotification>
{
    public void Configure(EntityTypeBuilder<SystemNotification> builder)
    {
        builder.ToTable("system_notifications");
        builder.ConfigureBase();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Message).HasColumnName("message").IsRequired();
        builder.Property(e => e.NotificationType).HasColumnName("notification_type")
            .HasVarcharEnumConversion<NotificationType>()
            .HasMaxLength(30);
        builder.Property(e => e.IsRead).HasColumnName("is_read");
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
