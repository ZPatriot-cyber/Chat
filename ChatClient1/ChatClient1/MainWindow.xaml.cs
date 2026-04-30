using System;
using System.Collections.ObjectModel;
using System.Windows;
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