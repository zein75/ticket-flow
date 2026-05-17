using TicketFlow.Domain.Abstractions;

namespace TicketFlow.Domain.Entities;

public sealed class Hall : Entity
{
    private readonly List<Seat> _seats = [];

    public Hall(Guid id, string name, int rows, int seatsPerRow)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Hall name is required.", nameof(name));
        }

        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows count must be positive.");
        }

        if (seatsPerRow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatsPerRow), "Seats per row must be positive.");
        }

        Name = name;
        Rows = rows;
        SeatsPerRow = seatsPerRow;

        GenerateSeats(rows, seatsPerRow);
    }

    public string Name { get; private set; }

    public int Rows { get; }

    public int SeatsPerRow { get; }

    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    private void GenerateSeats(int rows, int seatsPerRow)
    {
        for (var row = 1; row <= rows; row++)
        {
            for (var number = 1; number <= seatsPerRow; number++)
            {
                _seats.Add(new Seat(Guid.NewGuid(), Id, row, number));
            }
        }
    }
}
