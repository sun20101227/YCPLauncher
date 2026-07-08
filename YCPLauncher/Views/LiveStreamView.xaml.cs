using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class LiveStreamView : System.Windows.Controls.UserControl
{
    public class ChatMessage
    {
        public string Username { get; set; } = "";
        public string Message { get; set; } = "";
        public string Time { get; set; } = "";
        public string Initial => !string.IsNullOrEmpty(Username) ? Username.Substring(0, 1).ToUpper() : "?";
    }

    public ObservableCollection<ChatMessage> Messages { get; set; } = new();

    public LiveStreamView()
    {
        InitializeComponent();
        ChatList.ItemsSource = Messages;
        
        // Add some mock messages
        Messages.Add(new ChatMessage { Username = "System", Message = "欢迎来到 Yachiyo Cup 官方直播间！请大家文明发言。", Time = DateTime.Now.ToString("HH:mm") });
        Messages.Add(new ChatMessage { Username = "PlayerOne", Message = "这画质绝了！", Time = DateTime.Now.ToString("HH:mm") });
        Messages.Add(new ChatMessage { Username = "CS_God", Message = "NAVI 今天状态怎么样？", Time = DateTime.Now.ToString("HH:mm") });
    }

    private void SendMessage()
    {
        var text = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var player = AuthService.LoadPlayer();
        string username = player != null ? player.DisplayName : "Me";

        Messages.Add(new ChatMessage 
        { 
            Username = username, 
            Message = text, 
            Time = DateTime.Now.ToString("HH:mm") 
        });
        
        ChatInput.Text = "";
        ChatScroller.ScrollToEnd();
    }

    private void ChatInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendMessage();
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }
}
