using System.Net.Sockets;
using System.Text;

namespace UsbScannerClient.Services;

internal sealed class TcpScannerConnection : IDisposable
{
    private readonly SemaphoreSlim sync = new(1, 1);
    private TcpClient? client;
    private NetworkStream? stream;

    public bool IsConnected => client?.Connected == true && stream is not null;

    public async Task ConnectAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await sync.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                return;
            }

            await ConnectCoreAsync(settings, cancellationToken);
        }
        finally
        {
            sync.Release();
        }
    }

    public async Task SendScanAsync(
        string barcode,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
        ArgumentNullException.ThrowIfNull(settings);

        await sync.WaitAsync(cancellationToken);
        try
        {
            if (!IsConnected)
            {
                await ConnectCoreAsync(settings, cancellationToken);
            }

            byte[] payload = Encoding.UTF8.GetBytes(barcode + "\r\n");
            await stream!.WriteAsync(payload, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch
        {
            DisconnectCore();
            throw;
        }
        finally
        {
            sync.Release();
        }
    }

    public void Disconnect()
    {
        sync.Wait();
        try
        {
            DisconnectCore();
        }
        finally
        {
            sync.Release();
        }
    }

    public void Dispose()
    {
        Disconnect();
        sync.Dispose();
    }

    private async Task ConnectCoreAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ValidateSettings(settings);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.SendTimeoutMilliseconds);

        var tcpClient = new TcpClient
        {
            NoDelay = true
        };

        try
        {
            await tcpClient.ConnectAsync(
                settings.ServerHost.Trim(),
                settings.ServerPort,
                timeout.Token);

            tcpClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.KeepAlive,
                true);

            client = tcpClient;
            stream = tcpClient.GetStream();
        }
        catch
        {
            tcpClient.Dispose();
            throw;
        }
    }

    private static void ValidateSettings(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ServerHost))
        {
            throw new InvalidOperationException("Server host is required.");
        }

        if (settings.ServerPort is < 1 or > 65535)
        {
            throw new InvalidOperationException("Server port must be between 1 and 65535.");
        }

        if (settings.SendTimeoutMilliseconds <= 0)
        {
            throw new InvalidOperationException("Timeout must be greater than zero.");
        }
    }

    private void DisconnectCore()
    {
        try
        {
            if (client?.Connected == true)
            {
                client.Client.Shutdown(SocketShutdown.Both);
            }
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        stream?.Dispose();
        client?.Dispose();
        stream = null;
        client = null;
    }
}
