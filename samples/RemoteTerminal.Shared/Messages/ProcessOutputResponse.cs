using System;

namespace RemoteTerminal.Shared.Messages;

public enum ProcessOutputType
{
    StandardOutput,
    StandardError,
}

public class ProcessOutputResponse
{
    public ProcessOutputType OutputType { get; set; }
    public Guid RequestId { get; set; }
    public DateTime OutputTimestampUtc { get; set; } = DateTime.UtcNow;
    public string OutputText { get; set; }
    public bool IsEndOfOutput { get; set; }
}
