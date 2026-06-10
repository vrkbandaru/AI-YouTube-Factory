using Microsoft.AspNetCore.SignalR;
using AIYouTubeFactory.Core.Models;

namespace AIYouTubeFactory.API.Hubs;

public class ContentGenerationHub : Hub
{
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}

public class AgentProgressReporter : IProgress<AgentProgressUpdate>
{
    private readonly IHubContext<ContentGenerationHub> _hub;
    private readonly string _sessionId;

    public AgentProgressReporter(IHubContext<ContentGenerationHub> hub, string sessionId)
    {
        _hub = hub;
        _sessionId = sessionId;
    }

    public void Report(AgentProgressUpdate value)
    {
        _hub.Clients.Group(_sessionId).SendAsync("AgentProgress", value);
    }
}
