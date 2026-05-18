using Microsoft.AspNetCore.SignalR;

namespace TicketFlow.Api.Hubs;

public sealed class SeatHub : Hub
{
    public Task JoinSessionGroup(Guid sessionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
    }

    public Task LeaveSessionGroup(Guid sessionId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
    }

    public static string GetSessionGroupName(Guid sessionId)
    {
        return $"session:{sessionId}";
    }
}
