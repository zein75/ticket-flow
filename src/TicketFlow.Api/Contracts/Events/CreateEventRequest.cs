using System.ComponentModel.DataAnnotations;
using TicketFlow.Api.Validation;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Contracts.Events;

public sealed record CreateEventRequest(
    [Required]
    [MaxLength(200)]
    [NotBlank]
    string Title,

    [Required]
    [MaxLength(4000)]
    string Description,

    [EnumDataType(typeof(EventStatus))]
    EventStatus Status = EventStatus.Draft);
