using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.SessionId);
        builder.HasIndex(order => order.ReservationId)
            .IsUnique();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(order => order.TotalAmount, totalAmountBuilder =>
        {
            totalAmountBuilder.Property(amount => amount.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            totalAmountBuilder.Property(amount => amount.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasMany(order => order.Tickets)
            .WithOne()
            .HasForeignKey(ticket => ticket.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Tickets)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
