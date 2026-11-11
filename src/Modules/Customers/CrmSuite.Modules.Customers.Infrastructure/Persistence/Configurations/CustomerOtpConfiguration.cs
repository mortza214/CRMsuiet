using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerOtpConfiguration : BaseEntityConfiguration<CustomerOtp>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerOtp> builder)
    {
        builder.ToTable("customer_otps");

        builder.Property(otp => otp.Mobile)
            .HasColumnName("mobile")
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(otp => otp.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(otp => otp.ExpiresAtUtc)
            .HasColumnName("expires_at")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(otp => otp.IsUsed)
            .HasColumnName("is_used")
            .IsRequired();

        builder.Property(otp => otp.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(otp => otp.RequestedByIp)
            .HasColumnName("requested_by_ip")
            .HasMaxLength(64);

        builder.Property(otp => otp.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100);

        builder.HasIndex(otp => otp.CorrelationId)
            .IsUnique()
            .HasDatabaseName("ux_customer_otps_correlation_id");

        builder.HasIndex(otp => new { otp.Mobile, otp.IsUsed, otp.ExpiresAtUtc })
            .HasDatabaseName("ix_customer_otps_mobile_used_expires");
    }
}
