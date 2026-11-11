using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(user => user.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(user => user.Username)
            .IsUnique()
            .HasDatabaseName("ux_users_username");

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Mobile)
            .HasColumnName("mobile")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(user => user.Mobile)
            .IsUnique()
            .HasDatabaseName("ux_users_mobile");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(256);

        builder.Property(user => user.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(user => user.FailedLoginCount)
            .HasColumnName("failed_login_count")
            .IsRequired();

        builder.Property(user => user.LockoutEnd)
            .HasColumnName("lockout_end")
            .HasColumnType("datetime(6)");

        builder.HasMany(user => user.UserRoles)
            .WithOne(userRole => userRole.User)
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.UserDepartments)
            .WithOne(userDepartment => userDepartment.User)
            .HasForeignKey(userDepartment => userDepartment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(user => user.IsActive)
            .HasDatabaseName("ix_users_is_active");
    }
}
