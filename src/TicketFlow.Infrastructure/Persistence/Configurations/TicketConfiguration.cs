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

        builder.Property(ticket => ticket.QrCodePayload)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
    }
}
