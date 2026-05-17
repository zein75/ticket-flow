using TicketFlow.Domain.Abstractions;

namespace TicketFlow.Domain.Entities;

public sealed class Seat : Entity
{
    public Seat(Guid id, Guid hallId, int row, int number)
        : base(id)
    {
        if (hallId == Guid.Empty)
        {
            throw new ArgumentException("Hall id cannot be empty.", nameof(hallId));
        }

        if (row <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Seat row must be positive.");
        }

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Seat number must be positive.");
        }

        HallId = hallId;
        Row = row;
        Number = number;
    }

    public Guid HallId { get; }

    public int Row { get; }

    public int Number { get; }
}
