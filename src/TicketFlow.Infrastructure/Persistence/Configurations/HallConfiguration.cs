using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations;

internal sealed class HallConfiguration : IEntityTypeConfiguration<Hall>
{
    public void Configure(EntityTypeBuilder<Hall> builder)
    {
        builder.ToTable("halls");

        builder.HasKey(hall => hall.Id);

        builder.Property(hall => hall.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(hall => hall.Rows)
            .IsRequired();

        builder.Property(hall => hall.SeatsPerRow)
            .IsRequired();

        builder.HasMany(hall => hall.Seats)
            .WithOne()
            .HasForeignKey(seat => seat.HallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(hall => hall.Seats)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
