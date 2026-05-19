using FluentValidation;
using TicketFlow.Api.Contracts.Auth;
using TicketFlow.Api.Contracts.Events;
using TicketFlow.Api.Contracts.Halls;
using TicketFlow.Api.Contracts.Payments;
using TicketFlow.Api.Contracts.Reservations;
using TicketFlow.Api.Contracts.Tickets;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
        RuleFor(request => request.Password)
            .NotEmpty();
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(request => request.Description)
            .NotNull()
            .MaximumLength(4000);
        RuleFor(request => request.Status)
            .IsInEnum();
    }
}

public sealed class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(request => request.Description)
            .NotNull()
            .MaximumLength(4000);
        RuleFor(request => request.Status)
            .IsInEnum();
    }
}

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(request => request.HallId)
            .NotEmpty();
        RuleFor(request => request.EndsAtUtc)
            .GreaterThan(request => request.StartsAtUtc);
        RuleFor(request => request.PriceAmount)
            .InclusiveBetween(0, 999999999);
        RuleFor(request => request.PriceCurrency)
            .NotEmpty()
            .Length(3);
    }
}

public sealed class CreateHallRequestValidator : AbstractValidator<CreateHallRequest>
{
    public CreateHallRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(request => request.Rows)
            .InclusiveBetween(1, 200);
        RuleFor(request => request.SeatsPerRow)
            .InclusiveBetween(1, 500);
    }
}

public sealed class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(request => request.SessionId)
            .NotEmpty();
        RuleFor(request => request.SeatIds)
            .NotEmpty();
        RuleForEach(request => request.SeatIds)
            .NotEmpty();
    }
}

public sealed class PayReservationRequestValidator : AbstractValidator<PayReservationRequest>
{
    public PayReservationRequestValidator()
    {
        RuleFor(request => request.CardNumber)
            .NotEmpty()
            .MaximumLength(32);
    }
}

public sealed class ValidateTicketRequestValidator : AbstractValidator<ValidateTicketRequest>
{
    public ValidateTicketRequestValidator()
    {
        RuleFor(request => request.Payload)
            .NotEmpty();
    }
}
