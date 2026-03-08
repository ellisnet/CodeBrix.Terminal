using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using RemoteTerminal.Shared.Messages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteTerminal.Client.ViewModels;

public interface IScrollOutputToEnd
{
    Action ScrollOutputToEnd { get; set; }
}

public interface ITerminalOutput
{
    Action<string> FeedTerminalOutput { get; set; }
}

public class MainViewModel : SimpleViewModel, IScrollOutputToEnd, ITerminalOutput
{
    private const string ServerHubEndpoint = "remoteconnection";
    private const string TestHandshakeMethod = "TestHandshake";
    private const string RegisterProcessMethod = "RegisterProcess";
    private const string StartProcessMethod = "StartProcess";
    private const string SubscribeToProcessOutputMethod = "SubscribeToProcessOutput";

    private const string ServerConnectionSection = "ServerConnection";
    private const string RemoteUrlSetting = "RemoteUrl";
    private const string AuthTokenSetting = "AuthToken";

    private readonly string _remoteServerUrl;

    private bool _isDisposed;
    private bool IsDisposed() => _isDisposed;

    private bool _isFirstFeed = true;
    private bool _isConnecting;
    private Task _remoteProcessingTask;

    private HubConnection _serverHubConnection;
    private SemaphoreSlim _serverHubConnectLocker = new (1, 1);

    #region | Hub connection maintenance |

    private record HubConnectStatus(bool IsConnected, HubConnection Connection, string ErrorMessage);

    private async Task HandleHubConnectionClosed(Exception ex)
    {
        Debug.WriteLine(ex != null
            ? $"SignalR connection lost: {ex.Message}"
            : "SignalR connection closed.");

        var connection = _serverHubConnection;
        _serverHubConnection = null;

        if (connection != null)
        {
            try { await connection.DisposeAsync(); }
            catch { /* Best-effort cleanup */ }
        }

        NotifyPropertyChanged(nameof(ConnectToServerButtonText), notifyOnMainThread: true);
        ConnectToServerCommand.RaiseCanExecuteChanged();
    }

    private async Task<HubConnectStatus> ObtainHubConnection()
    {
        var isLocked = false;
        HubConnection connection;
        string errorMessage = null;

        try
        {
            CheckDisposed(IsDisposed);
            isLocked = await _serverHubConnectLocker.WaitAsync(timeout: TimeSpan.FromSeconds(30));
            CheckDisposed(IsDisposed);

            if (_serverHubConnection == null)
            {
                _isConnecting = true;
                NotifyPropertyChanged(nameof(ConnectToServerButtonText));
                NotifyPropertyChanged(nameof(IsReadyToSend));
                NotifyPropertyChanged(nameof(IsNotReadyToSend));
                ConnectToServerCommand.RaiseCanExecuteChanged();

                var authToken = GetService<IConfiguration>()
                    .GetValue<string>($"{ServerConnectionSection}:{AuthTokenSetting}");

                connection = new HubConnectionBuilder()
                    .WithUrl($"{_remoteServerUrl.TrimEnd('/')}/{ServerHubEndpoint}", 
                        configureHttpConnection: options =>
                            {
                                options.AccessTokenProvider = () => Task.FromResult(authToken);
                            })
                    .Build();

                connection.Closed += HandleHubConnectionClosed;

                await connection.StartAsync();

                if (connection.State == HubConnectionState.Connected)
                {
                    _serverHubConnection = connection;
                }
                else
                {
                    if (connection.State is HubConnectionState.Connecting or HubConnectionState.Reconnecting)
                    {
                        await connection.StopAsync();
                    }

                    await connection.DisposeAsync();
                    errorMessage = "Unable to connect successfully to the remote Server.";
                }
            }
            else
            {
                connection = _serverHubConnection;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            errorMessage = $"Error while trying to connect: {e.Message.Trim()}";
            connection = null;
        }
        finally
        {
            if (isLocked)
            {
                try { _serverHubConnectLocker?.Release(); }
                catch (ObjectDisposedException) { /* Semaphore disposed during shutdown */ }
            }
        }

        _isConnecting = false;
        NotifyPropertyChanged(nameof(ConnectToServerButtonText), notifyOnMainThread: true);
        NotifyPropertyChanged(nameof(IsReadyToSend), notifyOnMainThread: true);
        NotifyPropertyChanged(nameof(IsNotReadyToSend), notifyOnMainThread: true);

        ConnectToServerCommand.RaiseCanExecuteChanged();

        return (connection != null && string.IsNullOrWhiteSpace(errorMessage))
            ? new HubConnectStatus(IsConnected: true, connection, ErrorMessage: string.Empty)
            : new HubConnectStatus(IsConnected: false, Connection: null,
                ErrorMessage: (string.IsNullOrWhiteSpace(errorMessage))
                    ? "An unknown error occurred while trying to connect."
                    : errorMessage.Trim());
    }

    #endregion

    private void AddRemoteProcessOutputLine(string text, 
        bool withPrecedingNewLine = false,
        bool withExtraFollowingNewLine = false)
    {
        if (text != null)
        {
            InvokeOnMainThread(() =>
            {
                //We don't want blank lines at the top of our output window
                //  so, we should ignore withPrecedingNewLine until we have
                //  actually written some text.

                var textToFeed = (withPrecedingNewLine && (!_isFirstFeed))
                    ? $"\n{text}\n"
                    : $"{text}\n";

                if (!string.IsNullOrWhiteSpace(text))
                {
                    _isFirstFeed = false;
                }

                if (withExtraFollowingNewLine)
                {
                    textToFeed += "\n";
                }
                FeedTerminalOutput?.Invoke(textToFeed);
                ScrollOutputToEnd?.Invoke();
            });
        }
    }

    private void FeedRawOutputToTerminal(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            InvokeOnMainThread(() =>
            {
                FeedTerminalOutput?.Invoke(text);
                ScrollOutputToEnd?.Invoke();
            });
        }
    }

    public MainViewModel()
    {
        if (!IsDesignMode(true))
        {
            //Read the ServerConnection.RemoteUrl setting from the appsettings.json file
            _remoteServerUrl = GetService<IConfiguration>()
                .GetValue<string>($"{ServerConnectionSection}:{RemoteUrlSetting}");
            if (string.IsNullOrWhiteSpace(_remoteServerUrl))
            {
                throw new InvalidOperationException(
                    $"The {ServerConnectionSection}.{RemoteUrlSetting} section of the appsettings configuration file appears to be missing a valid value.");
            }

            Debug.WriteLine("Main view model startup.");
        }
        else
        {
            //Nothing to do here, this is the code path that is followed when a development tool loads
            //  the ViewModel to try to evaluate its properties/methods - we don't want any logic here.
        }
    }

    #region | Bindable properties |

    public string ConnectToServerButtonText => (_serverHubConnection != null)
        ? "Connected"
        : (_isConnecting)
            ? "Connecting..."
            : "Connect to Server";

    public string RemoteProcessOutput
    {
        get;
        set => SetProperty(ref field, (value ?? string.Empty));
    } = string.Empty;

    private string _inputToSend = string.Empty;
    [AffectsCommands(nameof(SendInputToRemoteCommand))]
    public string InputToSend
    {
        get => _inputToSend;
        //set => SetProperty(ref field, (value ?? string.Empty));
        set
        {
            var returnPressed = false;
            var cleanValue = value ?? string.Empty;
            if (cleanValue.Contains('\r') || cleanValue.Contains('\n'))
            {
                returnPressed = true;
                cleanValue = cleanValue.Replace("\r", "").Replace("\n", "");
            }

            _inputToSend = cleanValue;

            if (returnPressed)
            {
                // Defer the notification so it fires after the binding engine's
                // current update cycle completes — the TextBox will then re-read
                // the cleaned value.
                InvokeOnMainThread(() => ThisPropertyChanged());
            }
            else
            {
                ThisPropertyChanged();
            }

            if ((!string.IsNullOrWhiteSpace(cleanValue)) && returnPressed)
            {
                // ReSharper disable once AsyncVoidLambda - intentionally a fire-and-forget call
                new Task(async () => await DoSendInputToRemote()).Start();
            }
        }
    }

    public bool IsReadyToSend => (!_isDisposed)
                                 && (!_isConnecting)
                                 && (_serverHubConnection != null)
                                 && (_remoteProcessingTask == null);

    public bool IsNotReadyToSend => !IsReadyToSend;

    #endregion

    #region | Commands and their implementations |

    #region ConnectToServerCommand

    private SimpleCommand _connectToServerCommand;
    public SimpleCommand ConnectToServerCommand =>
        (_connectToServerCommand ??= new SimpleCommand(CanConnectToServer, DoConnectToServer));

    private bool CanConnectToServer() => (!_isDisposed) 
                                         && (_serverHubConnection == null)
                                         && (!_isConnecting); 

    private async Task DoConnectToServer()
    {
        if (CanConnectToServer())
        {
            try
            {
                CheckDisposed(IsDisposed);

                var connectStatus = await ObtainHubConnection();
                if (!string.IsNullOrWhiteSpace(connectStatus.ErrorMessage))
                {
                    throw new Exception(connectStatus.ErrorMessage);
                }
                else if ((!connectStatus.IsConnected) || connectStatus.Connection == null)
                {
                    throw new Exception("The connection to the remote server seems to have failed.");
                }

                var connectionResponse = await connectStatus.Connection.InvokeAsync<string>(TestHandshakeMethod, DateTime.Now.ToString("O"));

                await ShowInfo(connectionResponse);
            }
            catch (Exception e)
            {
                await ShowError(e, "Error while connecting to the Server application.");
            }
        }
    }

    #endregion

    #region SendInputToRemoteCommand

    private SimpleCommand _sendInputToRemoteCommand;
    public SimpleCommand SendInputToRemoteCommand =>
        (_sendInputToRemoteCommand ??= new SimpleCommand(CanSendInputToRemote, DoSendInputToRemote));

    private bool CanSendInputToRemote() => (!string.IsNullOrWhiteSpace(InputToSend))
                                           && IsReadyToSend;

    private async Task DoSendInputToRemote()
    {
        if (CanSendInputToRemote())
        {
            try
            {
                CheckDisposed(IsDisposed);

                var parts = InputToSend.Split(' ')
                    .Where((w => !string.IsNullOrWhiteSpace(w)))
                    .Select(s => s.Trim())
                    .ToArray();

                var registerRequest = new ProcessRegisterRequest
                {
                    Process = parts[0],
                    Arguments = (parts.Length > 0)
                        ? parts[1..]
                        : [],
                };

                var connectStatus = await ObtainHubConnection();
                if (!string.IsNullOrWhiteSpace(connectStatus.ErrorMessage))
                {
                    throw new Exception(connectStatus.ErrorMessage);
                }
                else if ((!connectStatus.IsConnected) || connectStatus.Connection == null)
                {
                    throw new Exception("The connection to the remote server seems to have failed.");
                }

                //Register our new process
                var registerResponse =
                    await connectStatus.Connection.InvokeAsync<ProcessRegisterResponse>(RegisterProcessMethod,
                        registerRequest);

                if (registerResponse == null)
                {
                    throw new HttpRequestException($"No response was received from {RegisterProcessMethod} at the Server.");
                }
                else if (!string.IsNullOrWhiteSpace(registerResponse.ErrorMessage))
                {
                    throw new HttpRequestException($"{RegisterProcessMethod} error response from Server: {registerResponse.ErrorMessage}");
                }

                AddRemoteProcessOutputLine($"> {InputToSend.Trim()}",
                    withPrecedingNewLine: true, withExtraFollowingNewLine: true);
                _inputToSend = string.Empty;
                NotifyPropertyChanged(nameof(InputToSend), notifyOnMainThread: true);

                //Subscribe to process output before starting the process
                _remoteProcessingTask = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var output in connectStatus.Connection.StreamAsync<ProcessOutputResponse>(
                            SubscribeToProcessOutputMethod, registerRequest.RequestId))
                        {
                            FeedRawOutputToTerminal((output.OutputType == ProcessOutputType.StandardError)
                                ? $"[ERROR] {output.OutputText}\n"
                                : $"{output.OutputText}\n");

                            if (output.IsEndOfOutput)
                            {
                                _remoteProcessingTask = null;
                                NotifyPropertyChanged(nameof(IsReadyToSend), notifyOnMainThread: true);
                                NotifyPropertyChanged(nameof(IsNotReadyToSend), notifyOnMainThread: true);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddRemoteProcessOutputLine($"[ERROR] {ex.Message}\n{ex}");
                        _remoteProcessingTask = null;
                        NotifyPropertyChanged(nameof(IsReadyToSend), notifyOnMainThread: true);
                        NotifyPropertyChanged(nameof(IsNotReadyToSend), notifyOnMainThread: true);
                    }
                });
                NotifyPropertyChanged(nameof(IsReadyToSend), notifyOnMainThread: true);
                NotifyPropertyChanged(nameof(IsNotReadyToSend), notifyOnMainThread: true);

                //Start the process
                var startResponse =
                    await connectStatus.Connection.InvokeAsync<ProcessStartResponse>(StartProcessMethod,
                        registerRequest.RequestId);

                if (startResponse == null)
                {
                    throw new HttpRequestException($"No response was received from {StartProcessMethod} at the Server.");
                }
                else if (!string.IsNullOrWhiteSpace(startResponse.ErrorMessage))
                {
                    throw new HttpRequestException($"{StartProcessMethod} error response from Server: {startResponse.ErrorMessage}");
                }
                else if (!startResponse.IsStarted)
                {
                    throw new HttpRequestException($"The process could not be started via {StartProcessMethod} on the Server.");
                }
            }
            catch (Exception e)
            {
                await ShowError(e, "Error while trying to send input to the Server.");
            }
        }
    }

    #endregion

    #endregion

    #region | IScrollOutputToEnd implementation |

    public Action ScrollOutputToEnd { get; set; }

    #endregion

    #region | ITerminalOutput implementation |

    public Action<string> FeedTerminalOutput { get; set; }

    #endregion

    #region | IDisposable implementation |

    public override void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _connectToServerCommand?.Dispose();
            _connectToServerCommand = null;
            _sendInputToRemoteCommand?.Dispose();
            _sendInputToRemoteCommand = null;

            try
            {
                _serverHubConnectLocker?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Best-effort lock acquisition during disposal
            }

            try
            {
                if (_serverHubConnection != null)
                {
                    _serverHubConnection.StopAsync().GetAwaiter().GetResult();
                    _serverHubConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    _serverHubConnection = null;
                }
            }
            finally
            {
                _serverHubConnectLocker?.Dispose();
                _serverHubConnectLocker = null;
            }
        }

        base.Dispose();
    }

    #endregion
}
