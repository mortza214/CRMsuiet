using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : BaseEntityConfiguration<Permission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.Property(permission => permission.Code)
            .HasColumnName("code")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique()
            .HasDatabaseName("ux_permissions_code");

        builder.Property(permission => permission.Group)
            .HasColumnName("group_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(permission => permission.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(permission => permission.Group)
            .HasDatabaseName("ix_permissions_group_name");

        builder.HasMany(permission => permission.RolePermissions)
            .WithOne(rolePermission => rolePermission.Permission)
            .HasForeignKey(rolePermission => rolePermission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
