using System;

namespace RemoteTerminal.Shared.Messages;

public class ProcessStartResponse
{
    public Guid RequestId { get; set; }
    public DateTime ResponseTimestampUtc { get; set; } = DateTime.UtcNow;
    public bool IsStarted { get; set; }
    public string ResponseText { get; set; }
    public string ErrorMessage { get; set; }
}
