using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.HasIndex(ticket => ticket.OrderId);
        builder.HasIndex(ticket => new { ticket.SessionId, ticket.SeatId })
            .IsUnique();

        builder.HasOne<Order>()
            .WithMany(order => order.Tickets)
            .HasForeignKey(ticket => ticket.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(ticket => ticket.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Seat>()
            .WithMany()
            .HasForeignKey(ticket => ticket.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ticket => ticket.QrCodePayload)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
    }
}
