using Microsoft.EntityFrameworkCore;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence;

public sealed class TicketFlowDbContext : DbContext
{
    public TicketFlowDbContext(DbContextOptions<TicketFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Hall> Halls => Set<Hall>();

    public DbSet<Seat> Seats => Set<Seat>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketFlowDbContext).Assembly);
    }
}
