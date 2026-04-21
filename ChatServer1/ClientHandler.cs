using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ChatServer1
{
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly ChatServer _server;
        private string _nickname;

        public ClientHandler(TcpClient tcpClient, ChatServer server)
        {
            _tcpClient = tcpClient;
            _server = server;
            _stream = tcpClient.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
        }

        public void Handle()
        {
            try
            {
                string joinLine = _reader.ReadLine();
                if (string.IsNullOrEmpty(joinLine) || !joinLine.StartsWith("/join "))
                {
                    SendMessage("[СИСТЕМА] Необходимо представиться: /join ВашНик");
                    Disconnect();
                    return;
                }
                _nickname = joinLine.Substring(6).Trim();
                if (string.IsNullOrEmpty(_nickname) || _server.IsNicknameTaken(_nickname))
                {
                    SendMessage("[СИСТЕМА] Никнейм занят или недопустим. Переподключитесь.");
                    Disconnect();
                    return;
                }

                if (!_server.RegisterClient(_nickname, this))
                {
                    SendMessage("[СИСТЕМА] Ошибка регистрации");
                    Disconnect();
                    return;
                }

                string line;
                while ((line = _reader.ReadLine()) != null)
                {
                    if (line.StartsWith("/pm "))
                    {
                        int spaceIdx = line.IndexOf(' ', 4);
                        if (spaceIdx == -1) continue;
                        string targetNick = line.Substring(4, spaceIdx - 4);
                        string privateMsg = line.Substring(spaceIdx + 1);
                        _server.SendPrivateMessage(_nickname, targetNick, privateMsg);
                    }
                    else if (line == "/users")
                    {
                        _server.UpdateUserList();
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        _server.BroadcastMessage(_nickname, line);
                    }
                }
            }
            catch (IOException)
            {
                // Клиент разорвал соединение
            }
            catch (Exception)
            {
                // Другая ошибка
            }
            finally
            {
                Disconnect();
            }
        }

        public void SendMessage(string message)
        {
            try
            {
                _writer.WriteLine(message);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!string.IsNullOrEmpty(_nickname))
                _server.UnregisterClient(_nickname);
            _writer?.Close();
            _reader?.Close();
            _stream?.Close();
            _tcpClient?.Close();
        }
    }
}