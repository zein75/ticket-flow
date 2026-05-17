using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(reservation => reservation.Id);

        builder.HasIndex(reservation => reservation.UserId);
        builder.HasIndex(reservation => reservation.SessionId);
        builder.HasIndex(reservation => new { reservation.Status, reservation.ExpiresAtUtc })
            .HasFilter($"\"Status\" = '{ReservationStatus.Active}'");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(reservation => reservation.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(reservation => reservation.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasMany(reservation => reservation.Seats)
            .WithOne()
            .HasForeignKey(seat => seat.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(reservation => reservation.Seats)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
