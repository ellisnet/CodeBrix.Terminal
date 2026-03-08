using System;
using System.Collections.Generic;

namespace RemoteTerminal.Shared.Messages;

public class ProcessRegisterRequest
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTime RequestTimestampUtc { get; set; } = DateTime.UtcNow;
    public string Process { get; set; }
    public IList<string> Arguments { get; set; }
}
