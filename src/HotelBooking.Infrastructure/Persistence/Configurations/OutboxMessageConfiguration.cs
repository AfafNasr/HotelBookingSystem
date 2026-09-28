using HotelBooking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(
            "OutboxMessages",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_OutboxMessages_AttemptCount",
                    "[AttemptCount] >= 0");
            });

        builder.HasKey(message =>
            message.Id);

        builder.Property(message =>
                message.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(message =>
                message.DeduplicationKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message =>
                message.Payload)
            .IsRequired();

        builder.Property(message =>
                message.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(message =>
                message.DeduplicationKey)
            .IsUnique();

        /*
         * Main worker query:
         *
         * ProcessedAt IS NULL
         * FailedAt IS NULL
         * NextAttemptAt <= now
         */
        builder.HasIndex(message =>
            new
            {
                message.ProcessedAt,
                message.FailedAt,
                message.NextAttemptAt,
                message.OccurredAt
            });
    }
}