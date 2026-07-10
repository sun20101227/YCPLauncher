using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class ChangelogDialog : Window
{
    public ChangelogDialog()
    {
        InitializeComponent();
        Loaded += ChangelogDialog_Loaded;
    }

    private async void ChangelogDialog_Loaded(object sender, RoutedEventArgs e)
    {
        var apiService = new ApiService();
        var releases = await apiService.GetAllReleasesAsync();

        ReleasesPanel.Children.Clear();

        if (releases == null || releases.Count == 0)
        {
            ReleasesPanel.Children.Add(new TextBlock 
            { 
                Text = "无法获取更新日志，请检查网络。", 
                Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush"), 
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center 
            });
            return;
        }

        foreach (var release in releases)
        {
            var card = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(10, 255, 255, 255)),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var sp = new StackPanel();
            
            var headerSp = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            headerSp.Children.Add(new TextBlock 
            { 
                Text = release.TagName, 
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush"), 
                FontSize = 18, 
                FontWeight = FontWeights.Bold 
            });
            headerSp.Children.Add(new TextBlock 
            { 
                Text = release.PublishedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), 
                Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush"), 
                FontSize = 12, 
                Margin = new Thickness(12, 6, 0, 0) 
            });

            sp.Children.Add(headerSp);

            var bodyText = new TextBlock { TextWrapping = TextWrapping.Wrap, LineHeight = 22, FontSize = 14 };
            RenderMarkdown(bodyText, release.Body ?? "");
            sp.Children.Add(bodyText);

            card.Child = sp;
            ReleasesPanel.Children.Add(card);
        }
    }

    private static void RenderMarkdown(TextBlock textBlock, string md)
    {
        textBlock.Inlines.Clear();
        if (string.IsNullOrEmpty(md)) return;

        var lines = md.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        bool firstLine = true;

        foreach (var line in lines)
        {
            if (!firstLine)
            {
                textBlock.Inlines.Add(new LineBreak());
            }
            firstLine = false;

            string currentLine = line.TrimEnd();
            
            if (currentLine.StartsWith("### "))
            {
                textBlock.Inlines.Add(new Run(currentLine.Substring(4)) { FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)) });
                continue;
            }
            if (currentLine.StartsWith("## "))
            {
                textBlock.Inlines.Add(new Run(currentLine.Substring(3)) { FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)) });
                textBlock.Inlines.Add(new LineBreak());
                continue;
            }
            if (currentLine.StartsWith("# "))
            {
                textBlock.Inlines.Add(new Run(currentLine.Substring(2)) { FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)) });
                textBlock.Inlines.Add(new LineBreak());
                continue;
            }

            if (currentLine.StartsWith("- "))
            {
                textBlock.Inlines.Add(new Run(" •  ") { FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 0)) });
                currentLine = currentLine.Substring(2);
            }

            var parts = System.Text.RegularExpressions.Regex.Split(currentLine, @"(\*\*.*?\*\*)");
            foreach (var part in parts)
            {
                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                {
                    textBlock.Inlines.Add(new Run(part.Substring(2, part.Length - 4)) { FontWeight = FontWeights.Bold });
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    var codeParts = System.Text.RegularExpressions.Regex.Split(part, @"(`.*?`)");
                    foreach (var cPart in codeParts)
                    {
                        if (cPart.StartsWith("`") && cPart.EndsWith("`") && cPart.Length > 2)
                        {
                            textBlock.Inlines.Add(new Run(cPart.Substring(1, cPart.Length - 2)) 
                            { 
                                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 180, 100))
                            });
                        }
                        else if (!string.IsNullOrEmpty(cPart))
                        {
                            textBlock.Inlines.Add(new Run(cPart) { Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush") });
                        }
                    }
                }
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
