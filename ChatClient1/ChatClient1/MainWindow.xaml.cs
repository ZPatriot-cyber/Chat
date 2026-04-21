using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChatClient1
{
    public partial class MainWindow : Window
    {
        private readonly ChatClient _chatClient;
        private readonly ObservableCollection<ChatMessage> _messages = new ObservableCollection<ChatMessage>();
        private string _myNickname;

        public MainWindow()
        {
            InitializeComponent();
            MessagesList.ItemsSource = _messages;
            _chatClient = new ChatClient();
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _chatClient.OnConnected += () =>
                Dispatcher.Invoke(new Action(() =>
                {
                    ConnectionStatus.Text = "✅ Подключено";
                    ConnectionStatus.Foreground = Brushes.Green;
                    ConnectBtn.IsEnabled = false;
                    DisconnectBtn.IsEnabled = true;
                    _myNickname = NickBox.Text.Trim();
                    AddSystemMessage($"Добро пожаловать, {_myNickname}!");
                }));

            _chatClient.OnDisconnected += () =>
                Dispatcher.Invoke(new Action(() =>
                {
                    ConnectionStatus.Text = "⛔ Отключён";
                    ConnectionStatus.Foreground = Brushes.Red;
                    ConnectBtn.IsEnabled = true;
                    DisconnectBtn.IsEnabled = false;
                    AddSystemMessage("Соединение потеряно");
                    UsersListBox.ItemsSource = null;
                }));

            _chatClient.OnMessageReceived += (formattedMsg) =>
                Dispatcher.Invoke(new Action(() =>
                {
                    if (formattedMsg.StartsWith("/users "))
                    {
                        string users = formattedMsg.Substring(7);
                        string[] list = users.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        UsersListBox.ItemsSource = list;
                    }
                    else
                    {
                        ParseAndAddMessage(formattedMsg);
                    }
                }));

            _chatClient.OnError += (err) =>
                Dispatcher.Invoke(new Action(() => AddSystemMessage($"Ошибка: {err}")));
        }

        private void ParseAndAddMessage(string raw)
        {
            var msg = new ChatMessage { Time = DateTime.Now.ToString("HH:mm:ss") };

            if (raw.StartsWith("[СИСТЕМА]"))
            {
                msg.DisplayName = "Система";
                msg.Text = raw.Substring(9);
                msg.NameColor = Brushes.Orange;
                msg.Background = Brushes.LightYellow;
                msg.Alignment = HorizontalAlignment.Left;
            }
            else if (raw.StartsWith("(личное → "))
            {
                int endIdx = raw.IndexOf(')');
                string to = raw.Substring(9, endIdx - 9);
                string content = raw.Substring(endIdx + 2);
                msg.DisplayName = $"→ {to} (личное)";
                msg.Text = content;
                msg.NameColor = Brushes.Purple;
                msg.Background = Brushes.Lavender;
                msg.Alignment = HorizontalAlignment.Right;
            }
            else if (raw.StartsWith("(личное)"))
            {
                string rest = raw.Substring(9);
                int colon = rest.IndexOf(':');
                if (colon > 0)
                {
                    string sender = rest.Substring(0, colon).Trim();
                    string text = rest.Substring(colon + 1).Trim();
                    msg.DisplayName = $"{sender} (личное)";
                    msg.Text = text;
                    msg.NameColor = Brushes.Blue;
                    msg.Background = Brushes.LightBlue;
                    msg.Alignment = HorizontalAlignment.Left;
                }
                else
                {
                    msg.DisplayName = "Личное";
                    msg.Text = rest;
                    msg.Background = Brushes.LightBlue;
                    msg.Alignment = HorizontalAlignment.Left;
                }
            }
            else if (raw.Contains(':'))
            {
                int colon = raw.IndexOf(':');
                string sender = raw.Substring(0, colon);
                string text = raw.Substring(colon + 1).Trim();
                msg.DisplayName = sender;
                msg.Text = text;
                if (sender == _myNickname)
                {
                    msg.Background = Brushes.LightGreen;
                    msg.Alignment = HorizontalAlignment.Right;
                    msg.NameColor = Brushes.DarkGreen;
                }
                else
                {
                    msg.Background = Brushes.White;
                    msg.Alignment = HorizontalAlignment.Left;
                    msg.NameColor = Brushes.Black;
                }
            }
            else
            {
                msg.DisplayName = "Сообщение";
                msg.Text = raw;
                msg.Background = Brushes.LightGray;
                msg.Alignment = HorizontalAlignment.Center;
            }

            _messages.Add(msg);
            ScrollToBottom();
        }

        private void AddSystemMessage(string text)
        {
            _messages.Add(new ChatMessage
            {
                DisplayName = "Система",
                Text = text,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                NameColor = Brushes.Orange,
                Background = Brushes.LightYellow,
                Alignment = HorizontalAlignment.Left
            });
            ScrollToBottom();
        }

        private void ScrollToBottom() => MessagesScrollViewer.ScrollToBottom();

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpBox.Text.Trim();
            if (!int.TryParse(PortBox.Text, out int port))
            {
                MessageBox.Show("Неверный порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string nick = NickBox.Text.Trim();
            if (string.IsNullOrEmpty(nick))
            {
                MessageBox.Show("Введите ник", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _chatClient.Connect(ip, port, nick);
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e) => _chatClient.Disconnect();
        private void SendBtn_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void MessageInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !(Keyboard.Modifiers == ModifierKeys.Shift))
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendMessage()
        {
            string text = MessageInputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            _chatClient.SendMessage(text);
            MessageInputBox.Clear();
        }

        protected override void OnClosing(CancelEventArgs e) => _chatClient.Disconnect();
    }

    public class ChatMessage : INotifyPropertyChanged
    {
        private string _displayName;
        private string _text;
        private string _time;
        private Brush _nameColor = Brushes.Black;
        private Brush _background = Brushes.White;
        private HorizontalAlignment _alignment = HorizontalAlignment.Left;

        public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }
        public string Text { get => _text; set { _text = value; OnPropertyChanged(); } }
        public string Time { get => _time; set { _time = value; OnPropertyChanged(); } }
        public Brush NameColor { get => _nameColor; set { _nameColor = value; OnPropertyChanged(); } }
        public Brush Background { get => _background; set { _background = value; OnPropertyChanged(); } }
        public HorizontalAlignment Alignment { get => _alignment; set { _alignment = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}