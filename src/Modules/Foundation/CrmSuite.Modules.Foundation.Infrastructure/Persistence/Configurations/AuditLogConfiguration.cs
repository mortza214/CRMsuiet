using CrmSuite.Modules.Foundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(log => log.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(log => log.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(log => log.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("longtext");

        builder.Property(log => log.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("longtext");

        builder.Property(log => log.UserId)
            .HasColumnName("user_id");

        builder.Property(log => log.UserIp)
            .HasColumnName("user_ip")
            .HasMaxLength(64);

        builder.Property(log => log.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(log => log.Timestamp);
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => new { log.EntityName, log.EntityId });
    }
}
