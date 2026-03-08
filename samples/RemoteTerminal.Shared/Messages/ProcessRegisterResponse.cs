using System;

namespace RemoteTerminal.Shared.Messages;

public class ProcessRegisterResponse
{
    public Guid RequestId { get; set; }
    public DateTime ResponseTimestampUtc { get; set; } = DateTime.UtcNow;
    public string ResponseText { get; set; }
    public string ErrorMessage { get; set; }
}
