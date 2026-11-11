using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class WithdrawalRequestConfiguration : BaseEntityConfiguration<WithdrawalRequest>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.ToTable("withdrawal_requests");

        builder.Property(request => request.WalletId).HasColumnName("wallet_id").IsRequired();
        builder.Property(request => request.EmployeeId).HasColumnName("employee_id").IsRequired();

        builder.Property(request => request.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(request => request.ApprovedAt).HasColumnName("approved_at").HasColumnType("datetime(6)");
        builder.Property(request => request.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(request => request.DecisionNote).HasColumnName("decision_note").HasMaxLength(500);

        builder.HasIndex(request => new { request.EmployeeId, request.Status })
            .HasDatabaseName("ix_withdrawal_requests_employee_status");
    }
}
