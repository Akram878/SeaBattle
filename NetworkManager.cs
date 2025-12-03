using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SeaBattle
{
    /// <summary>
    /// Responsible for network communication between two machines (Host/Client)
    /// with a simple text-based protocol.
    /// Events:
    /// - Connected
    /// - ShotReceived(x, y)
    /// - ResultReceived("HIT"/"MISS"/"WIN")
    /// - ResetReceived()
    /// - Disconnected()
    /// </summary>
    public class NetworkManager
    {
        private readonly bool _isHost;
        private readonly string _ip;
        private readonly int _port;

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        private Thread _workerThread;
        private CancellationTokenSource _cts;
        private readonly object _syncRoot = new object();
        private bool _disconnectedRaised = false;

        public event Action Connected;
        public event Action<int, int> ShotReceived;
        public event Action<string> ResultReceived;
        public event Action ResetReceived;
        public event Action Disconnected;

        public NetworkManager(bool isHost, string ip, int port)
        {
            _isHost = isHost;
            _ip = ip;
            _port = port;
        }

        /// <summary>
        /// Starts the network session (Host or Client).
        /// Should be called from a background thread.
        /// </summary>
        public void Start()
        {
            _cts = new CancellationTokenSource();

            if (_isHost)
                StartAsHost();
            else
                StartAsClient();
        }

        private void StartAsHost()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                // Blocking call until a client connects.
                _client = _listener.AcceptTcpClient();

                SetupStreams();
                RaiseConnected();
                StartWorkerLoop();
            }
            catch
            {
                Cleanup();
                RaiseDisconnected();
            }
        }

        private void StartAsClient()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(_ip, _port);

                SetupStreams();
                RaiseConnected();
                StartWorkerLoop();
            }
            catch
            {
                Cleanup();
                RaiseDisconnected();
            }
        }

        private void SetupStreams()
        {
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
        }

        private void StartWorkerLoop()
        {
            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true
            };
            _workerThread.Start();
        }

        private void WorkerLoop()
        {
            var token = _cts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    string line;
                    try
                    {
                        line = _reader.ReadLine();
                    }
                    catch
                    {
                        break; // read failed → connection lost
                    }

                    if (line == null)
                        break; // remote closed connection

                    HandleIncomingLine(line);
                }
            }
            finally
            {
                Cleanup();
                RaiseDisconnected();
            }
        }

        private void HandleIncomingLine(string line)
        {
            // Simple protocol:
            // "SHOT:x:y" / "RESULT:..." / "RESET" / "CLOSE"
            if (line.StartsWith("SHOT:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':');
                if (parts.Length == 3 &&
                    int.TryParse(parts[1], out int x) &&
                    int.TryParse(parts[2], out int y))
                {
                    ShotReceived?.Invoke(x, y);
                }
            }
            else if (line.StartsWith("RESULT:", StringComparison.OrdinalIgnoreCase))
            {
                var result = line.Substring("RESULT:".Length).Trim();
                ResultReceived?.Invoke(result);
            }
            else if (line.Equals("RESET", StringComparison.OrdinalIgnoreCase))
            {
                ResetReceived?.Invoke();
            }
            else if (line.Equals("CLOSE", StringComparison.OrdinalIgnoreCase))
            {
                // Remote requested closing
                _cts.Cancel();
            }
        }

        public void SendShot(int x, int y)
        {
            lock (_syncRoot)
            {
                if (_writer == null) return;
                try
                {
                    _writer.WriteLine($"SHOT:{x}:{y}");
                }
                catch
                {
                    // WorkerLoop will handle disconnection
                }
            }
        }

        public void SendResult(string result)
        {
            lock (_syncRoot)
            {
                if (_writer == null) return;
                try
                {
                    _writer.WriteLine($"RESULT:{result}");
                }
                catch
                {
                    // WorkerLoop will handle disconnection
                }
            }
        }

        public void SendReset()
        {
            lock (_syncRoot)
            {
                if (_writer == null) return;
                try
                {
                    _writer.WriteLine("RESET");
                }
                catch
                {
                    // WorkerLoop will handle disconnection
                }
            }
        }

        /// <summary>
        /// Closes the network session.
        /// Prefer calling this from a background thread.
        /// </summary>
        public void Close()
        {
            lock (_syncRoot)
            {
                if (_cts == null)
                    return;

                try
                {
                    _writer?.WriteLine("CLOSE");
                }
                catch { }

                try
                {
                    _cts.Cancel();
                }
                catch { }

                Cleanup();
            }
        }

        private void Cleanup()
        {
            lock (_syncRoot)
            {
                try { _reader?.Dispose(); } catch { }
                try { _writer?.Dispose(); } catch { }
                try { _stream?.Close(); } catch { }
                try { _client?.Close(); } catch { }
                try { _listener?.Stop(); } catch { }

                _reader = null;
                _writer = null;
                _stream = null;
                _client = null;
                _listener = null;
            }
        }

        private void RaiseConnected()
        {
            try { Connected?.Invoke(); } catch { }
        }

        private void RaiseDisconnected()
        {
            lock (_syncRoot)
            {
                if (_disconnectedRaised)
                    return;

                _disconnectedRaised = true;
            }

            try { Disconnected?.Invoke(); } catch { }
        }
    }
}
