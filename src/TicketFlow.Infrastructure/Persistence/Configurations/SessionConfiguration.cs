using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(session => session.Id);

        builder.HasIndex(session => session.EventId);
        builder.HasIndex(session => session.HallId);

        builder.HasOne<Event>()
            .WithMany(ticketEvent => ticketEvent.Sessions)
            .HasForeignKey(session => session.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Hall>()
            .WithMany()
            .HasForeignKey(session => session.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(session => session.StartsAtUtc)
            .IsRequired();

        builder.Property(session => session.EndsAtUtc)
            .IsRequired();

        builder.OwnsOne(session => session.Price, priceBuilder =>
        {
            priceBuilder.Property(price => price.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(price => price.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
