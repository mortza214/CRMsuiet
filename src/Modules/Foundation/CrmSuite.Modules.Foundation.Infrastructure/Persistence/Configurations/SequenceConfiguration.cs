using CrmSuite.Modules.Foundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;

public sealed class SequenceConfiguration : BaseEntityConfiguration<Sequence>
{
    private static readonly DateTime SeedTimestamp =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<Sequence> builder)
    {
        builder.ToTable("sequences");

        builder.Property(sequence => sequence.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(sequence => sequence.Name)
            .IsUnique()
            .HasDatabaseName("ux_sequences_name");

        builder.Property(sequence => sequence.Prefix)
            .HasColumnName("prefix")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(sequence => sequence.CurrentValue)
            .HasColumnName("current_value")
            .IsRequired();

        builder.Property(sequence => sequence.Format)
            .HasColumnName("format")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "InvoiceNumber",
                Prefix = "INV-",
                CurrentValue = 0L,
                Format = "{Prefix}{Year}-{CurrentValue:D5}",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "ContractNumber",
                Prefix = "CTR-",
                CurrentValue = 0L,
                Format = "{Prefix}{Year}-{CurrentValue:D5}",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "OrderNumber",
                Prefix = "ORD-",
                CurrentValue = 0L,
                Format = "{Prefix}{Year}-{CurrentValue:D5}",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "TicketNumber",
                Prefix = "TKT-",
                CurrentValue = 0L,
                Format = "{Prefix}{Year}-{CurrentValue:D5}",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
