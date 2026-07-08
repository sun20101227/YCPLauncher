using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YCPLauncher.Services;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

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
        
        InitializeWebViewAsync();

        // Add some mock messages
        Messages.Add(new ChatMessage { Username = "System", Message = "欢迎来到 Yachiyo Cup 官方直播间！请大家文明发言。", Time = DateTime.Now.ToString("HH:mm") });
        Messages.Add(new ChatMessage { Username = "PlayerOne", Message = "这画质绝了！", Time = DateTime.Now.ToString("HH:mm") });
        Messages.Add(new ChatMessage { Username = "CS_God", Message = "NAVI 今天状态怎么样？", Time = DateTime.Now.ToString("HH:mm") });
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "YCPLauncher_WebView"));
            await PlayerWebView.EnsureCoreWebView2Async(env);
            
            PlayerWebView.CoreWebView2.Navigate("https://player.bilibili.com/player.html?bvid=BV1GJ411x7h7&high_quality=1&danmaku=0");
            
            PlayerWebView.NavigationCompleted += (s, e) =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WebView2 initialization failed: " + ex.Message);
        }
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
