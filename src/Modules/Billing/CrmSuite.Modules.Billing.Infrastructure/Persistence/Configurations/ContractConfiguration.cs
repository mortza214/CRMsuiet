using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class ContractConfiguration : BaseEntityConfiguration<Contract>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.Property(contract => contract.ContractNumber)
            .HasColumnName("contract_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(contract => contract.ContractNumber)
            .IsUnique()
            .HasDatabaseName("ux_contracts_contract_number");

        builder.Property(contract => contract.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(contract => contract.OrderId).HasColumnName("order_id");

        builder.Property(contract => contract.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(contract => contract.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(contract => contract.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(contract => contract.Content)
            .HasColumnName("content")
            .HasColumnType("longtext")
            .IsRequired();

        builder.HasIndex(contract => contract.CustomerId)
            .HasDatabaseName("ix_contracts_customer_id");
    }
}
