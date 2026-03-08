using RemoteTerminal.Shared.Messages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;

namespace RemoteTerminal.Server.Processing;

public class RegisteredProcess : IDisposable
{
    private Process _process;
    private readonly Channel<ProcessOutputResponse> _outputChannel =
        Channel.CreateUnbounded<ProcessOutputResponse>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });

    public Guid RequestId { get; }
    public DateTime RequestTimestampUtc { get; }
    public DateTime? StartedTimestampUtc { get; private set; }
    public bool IsStarted { get; private set; }
    public ChannelReader<ProcessOutputResponse> OutputReader => _outputChannel.Reader;

    public RegisteredProcess(ProcessRegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestId = request.RequestId;
        RequestTimestampUtc = request.RequestTimestampUtc;

        if (string.IsNullOrWhiteSpace(request.Process))
        {
            throw new ArgumentException($"The {nameof(request.Process)} property value cannot be null or blank.", nameof(request));
        }

        var arguments = (request.Arguments?.Any(a => !string.IsNullOrWhiteSpace(a)) ?? false)
            ? string.Join(' ', request.Arguments
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(s => s.Trim()))
            : string.Empty;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = request.Process.Trim(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false, //TODO: Might want this to be true?
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;
        _process.Exited += OnProcessExited;
    }

    public ProcessStartResponse Start()
    {
        string errorMessage = null;

        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            StartedTimestampUtc = DateTime.UtcNow;
            IsStarted = true;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            _outputChannel.Writer.TryComplete(e);
        }

        return new ProcessStartResponse
        {
            RequestId = RequestId,
            ResponseTimestampUtc = DateTime.UtcNow,
            IsStarted = IsStarted,
            ResponseText = string.Empty,
            ErrorMessage = errorMessage,
        };
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;

        _outputChannel.Writer.TryWrite(new ProcessOutputResponse
        {
            OutputType = ProcessOutputType.StandardOutput,
            RequestId = RequestId,
            OutputTimestampUtc = DateTime.UtcNow,
            OutputText = e.Data,
            IsEndOfOutput = false
        });
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;

        _outputChannel.Writer.TryWrite(new ProcessOutputResponse
        {
            OutputType = ProcessOutputType.StandardError,
            RequestId = RequestId,
            OutputTimestampUtc = DateTime.UtcNow,
            OutputText = e.Data,
            IsEndOfOutput = false
        });
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        _outputChannel.Writer.TryWrite(new ProcessOutputResponse
        {
            OutputType = ProcessOutputType.StandardOutput,
            RequestId = RequestId,
            OutputTimestampUtc = DateTime.UtcNow,
            OutputText = string.Empty,
            IsEndOfOutput = true
        });

        _outputChannel.Writer.TryComplete();
    }

    #region | IDisposable implementation |

    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_process is not null)
        {
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnErrorDataReceived;
            _process.Exited -= OnProcessExited;

            try
            {
                if (IsStarted && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Process may have already exited.
            }

            _process.Dispose();
            _process = null;
        }

        _outputChannel.Writer.TryComplete();
    }

    #endregion
}
