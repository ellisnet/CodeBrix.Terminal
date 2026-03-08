using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteTerminal.Server.Auth;
using RemoteTerminal.Server.Processing;
using RemoteTerminal.Shared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteTerminal.Server.Hubs;

[Authorize(AuthSettings.AuthorizedEntityPolicy)]
public class RemoteConnectionHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, RegisteredProcess> RegisteredProcesses =
        new ();

    public Task<string> TestHandshake(string message)
    {
        var response = "The incoming message could not be understood by the Server.";

        if (!string.IsNullOrWhiteSpace(message)
            && DateTime.TryParse(message.Trim(), out var parsed))
        {
            response = $"Response to a client message that was sent at: {parsed:g}";
        }

        return Task.FromResult(response);
    }

    public Task<ProcessRegisterResponse> RegisterProcess(ProcessRegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestId.Equals(Guid.Empty))
        {
            throw new ArgumentException($"Invalid {nameof(request.RequestId)} value.", nameof(request));
        }

        var response = new ProcessRegisterResponse
        {
            RequestId = request.RequestId,
            ResponseTimestampUtc = DateTime.UtcNow,
            ResponseText = null,
            ErrorMessage = "The process was unable to be registered.",
        };

        try
        {
            var process = new RegisteredProcess(request);
            if (RegisteredProcesses.TryAdd(request.RequestId, process))
            {
                response.ErrorMessage = null;
                response.ResponseText = "Process registered successfully.";
            }
        }
        catch (Exception e)
        {
            response.ErrorMessage += $" {e.Message}";
        }

        return Task.FromResult(response);
    }

    public Task<ProcessStartResponse> StartProcess(Guid requestId)
    {
        if (requestId.Equals(Guid.Empty))
        {
            throw new ArgumentException("Invalid requestId value.", nameof(requestId));
        }

        if (!RegisteredProcesses.TryGetValue(requestId, out var process))
        {
            throw new HubException($"No registered process found for request ID: {requestId}");
        }

        var response = new ProcessStartResponse
        {
            RequestId = requestId,
            ResponseTimestampUtc = DateTime.UtcNow,
            IsStarted = false,
            ResponseText = null,
            ErrorMessage = "The process was unable to start.",
        };

        try
        {
            response = process.Start();
        }
        catch (Exception e)
        {
            response.ErrorMessage += $" {e.Message}";
        }

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ProcessOutputResponse> SubscribeToProcessOutput(
        Guid requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!RegisteredProcesses.TryGetValue(requestId, out var process))
        {
            throw new HubException($"No process found for request ID: {requestId}");
        }

        await foreach (var output in process.OutputReader.ReadAllAsync(cancellationToken))
        {
            yield return output;

            if (output.IsEndOfOutput)
            {
                RegisteredProcesses.TryRemove(requestId, out _);
                process.Dispose();
            }
        }
    }
}
