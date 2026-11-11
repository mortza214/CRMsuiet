using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(token => token.Token)
            .HasColumnName("token")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(token => token.Token)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_token");

        builder.Property(token => token.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(token => token.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("datetime(6)");

        builder.Property(token => token.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(64);

        builder.Property(token => token.ReplacedByToken)
            .HasColumnName("replaced_by_token")
            .HasMaxLength(200);

        builder.Property(token => token.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(64);

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(token => new { token.UserId, token.ExpiresAt })
            .HasDatabaseName("ix_refresh_tokens_user_expires");

        builder.HasQueryFilter(token => !token.User.IsDeleted);
    }
}
