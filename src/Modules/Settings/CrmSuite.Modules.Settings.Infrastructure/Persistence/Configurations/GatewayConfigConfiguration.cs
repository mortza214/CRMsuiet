using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Settings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Settings.Infrastructure.Persistence.Configurations;

public sealed class GatewayConfigConfiguration : BaseEntityConfiguration<GatewayConfig>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GatewayConfig> builder)
    {
        builder.ToTable("gateway_configs");

        builder.Property(config => config.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(config => config.Provider)
            .IsUnique()
            .HasDatabaseName("ux_gateway_configs_provider");

        builder.Property(config => config.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(config => config.MerchantIdEncrypted)
            .HasColumnName("merchant_id_encrypted")
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(config => config.TerminalIdEncrypted)
            .HasColumnName("terminal_id_encrypted")
            .HasColumnType("longtext")
            .IsRequired();
    }
}
