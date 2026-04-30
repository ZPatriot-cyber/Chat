using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClient1
{
    public class ChatClient
    {
        private TcpClient _tcpClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private Thread _readThread;
        private bool _isConnected;
        private string _nickname;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnError;
        public void Connect(string ip, int port, string nickname)
        {
            if (_isConnected) return;
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(ip, port);
                var stream = _tcpClient.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                _nickname = nickname;

                _writer.WriteLine($"/join {_nickname}");
                _isConnected = true;
                OnConnected?.Invoke();

                _readThread = new Thread(ReadLoop) { IsBackground = true };
                _readThread.Start();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Не удалось подключиться: {ex.Message}");
            }
        }