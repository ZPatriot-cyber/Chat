using System;
using System.Linq;
using System.Windows;

namespace ChatServer1
{
    public partial class MainWindow : Window
    {
        private readonly ChatServer _server;

        public MainWindow()
        {
            InitializeComponent();
            _server = new ChatServer();
            SubscribeToServerEvents();
        }

        private void SubscribeToServerEvents()
        {
            _server.OnServerStarted += port =>
                Dispatcher.Invoke(new Action(() =>
                {
                    AddLog($"[СЕРВЕР] Запущен на порту {port}");
                    StatusText.Text = "✅ Сервер запущен";
                    StartBtn.IsEnabled = false;
                    StopBtn.IsEnabled = true;
            string localIp = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
    .AddressList
    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    ?.ToString() ?? "неизвестен";
            IpText.Text = $"📡 IP: {localIp}:{port}";
            AddLog($"[СЕРВЕР] Локальный IP: {localIp}");
        }));

            _server.OnServerStopped += () =>
                Dispatcher.Invoke(new Action(() =>
                {
                    AddLog("[СЕРВЕР] Остановлен");
                    StatusText.Text = "⛔ Сервер не запущен";
                    StartBtn.IsEnabled = true;
                    StopBtn.IsEnabled = false;
                    ClientsCountText.Text = "Подключено клиентов: 0";
                }));

            _server.OnClientConnected += nickname =>
                Dispatcher.Invoke(new Action(() =>
                {
                    AddLog($"[+] {nickname} подключился");
                    UpdateClientsCount();
                }));

            _server.OnClientDisconnected += nickname =>
                Dispatcher.Invoke(new Action(() =>
                {
                    AddLog($"[-] {nickname} отключился");
                    UpdateClientsCount();
                }));

            _server.OnMessageReceived += (sender, message) =>
                Dispatcher.Invoke(new Action(() => AddLog($"[{sender}] {message}")));

            _server.OnError += error =>
                Dispatcher.Invoke(new Action(() => AddLog($"[ОШИБКА] {error}")));
        }

        private void AddLog(string text)
        {
            LogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {text}");
            if (LogListBox.Items.Count > 500)
                LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
        }

        private void UpdateClientsCount()
        {
            ClientsCountText.Text = $"Подключено клиентов: {_server.GetConnectedClientsCount()}";
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PortBox.Text, out int port))
                _server.Start(port);
            else
                MessageBox.Show("Введите корректный номер порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e) => _server.Stop();

        protected override void OnClosed(EventArgs e) => _server.Stop();
    }
}