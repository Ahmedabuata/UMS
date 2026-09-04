using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using University.Core.Entities;

namespace University.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.ConfigureBase();
        // Id is the shared primary key: users.id == employees.id (or students.id).
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(true);
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.RoleId);

        // Shared-Primary-Key 1:1: users.id == employees.id.
        // Employee is principal; deleting an Employee deletes this User (Cascade), but deleting
        // this User does NOT delete the Employee.
        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
