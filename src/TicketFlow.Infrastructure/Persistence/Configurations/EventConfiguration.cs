using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(ticketEvent => ticketEvent.Id);

        builder.Property(ticketEvent => ticketEvent.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ticketEvent => ticketEvent.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(ticketEvent => ticketEvent.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(ticketEvent => ticketEvent.OrganizerId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ticketEvent => ticketEvent.Sessions)
            .WithOne()
            .HasForeignKey(session => session.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ticketEvent => ticketEvent.Sessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
