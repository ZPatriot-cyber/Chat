using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer1
{
    public class ChatServer
    {
        private TcpListener _listener;
        private readonly Dictionary<string, ClientHandler> _clients = new Dictionary<string, ClientHandler>();
        private readonly object _lockObj = new object();
        private bool _isRunning;
        private Thread _acceptThread;

        public event Action<int> OnServerStarted;
        public event Action OnServerStopped;
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action<string, string> OnMessageReceived;
        public event Action<string> OnError;

        //ALO MAKSIM
        public void Start(int port)
        {
            if (_isRunning) return;
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isRunning = true;
                OnServerStarted?.Invoke(port);

                _acceptThread = new Thread(AcceptClients) { IsBackground = true };
                _acceptThread.Start();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Ошибка запуска: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listener?.Stop();

            lock (_lockObj)
            {
                foreach (var handler in _clients.Values)
                    handler.Disconnect();
                _clients.Clear();
            }
            OnServerStopped?.Invoke();
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient tcpClient = _listener.AcceptTcpClient();
                    var handler = new ClientHandler(tcpClient, this);
                    var clientThread = new Thread(handler.Handle) { IsBackground = true };
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Ошибка принятия клиента: {ex.Message}");
                }
            }
        }

        public bool RegisterClient(string nickname, ClientHandler handler)
        {
            lock (_lockObj)
            {
                if (_clients.ContainsKey(nickname))
                    return false;
                _clients[nickname] = handler;
            }
            OnClientConnected?.Invoke(nickname);
            BroadcastSystemMessage($"{nickname} вошёл в чат", nickname);
            UpdateUserList();
            return true;
        }

        public void UnregisterClient(string nickname)
        {
            lock (_lockObj)
            {
                if (_clients.ContainsKey(nickname))
                    _clients.Remove(nickname);
            }
            OnClientDisconnected?.Invoke(nickname);
            BroadcastSystemMessage($"{nickname} покинул чат");
            UpdateUserList();
        }

        public void BroadcastMessage(string senderNick, string message)
        {
            string formatted = $"{senderNick}: {message}";
            OnMessageReceived?.Invoke(senderNick, message);
            lock (_lockObj)
            {
                foreach (var client in _clients.Values)
                    client.SendMessage(formatted);
            }
        }

        public void BroadcastSystemMessage(string message, string excludeNick = null)
        {
            string sysMsg = $"[СИСТЕМА] {message}";
            lock (_lockObj)
            {
                foreach (var kvp in _clients)
                {
                    if (excludeNick == null || kvp.Key != excludeNick)
                        kvp.Value.SendMessage(sysMsg);
                }
            }
        }

        public void SendPrivateMessage(string fromNick, string toNick, string message)
        {
            ClientHandler target;
            lock (_lockObj)
            {
                if (!_clients.TryGetValue(toNick, out target))
                {
                    if (_clients.TryGetValue(fromNick, out var fromClient))
                        fromClient.SendMessage($"[СИСТЕМА] Пользователь {toNick} не найден");
                    return;
                }
            }
            target.SendMessage($"(личное) {fromNick}: {message}");
            lock (_lockObj)
            {
                if (_clients.TryGetValue(fromNick, out var fromClient))
                    fromClient.SendMessage($"(личное → {toNick}) {message}");
            }
            OnMessageReceived?.Invoke(fromNick, $"(личное → {toNick}) {message}");
        }

        public void UpdateUserList()
        {
            string userList;
            lock (_lockObj)
            {
                userList = "/users " + string.Join(",", _clients.Keys);
            }
            lock (_lockObj)  // ← БАГИ: второй lock того же объекта = дедлок
            {
                foreach (var client in _clients.Values)
                    client.SendMessage(userList);
            }
        }

        public bool IsNicknameTaken(string nickname)
        {
            lock (_lockObj)
            {
                return _clients.ContainsKey(nickname);
            }
        }

        public int GetConnectedClientsCount()
        {
            lock (_lockObj)
            {
                return _clients.Count;
            }
        }
    }
}