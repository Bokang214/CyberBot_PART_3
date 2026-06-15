using System;
using System.Collections.Generic;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberBot_PART_3
{
    public partial class MainWindow : Window
    {
        private CyberBot cyberBot;

        // DELEGATE DECLARATION (for POE marks)
        public delegate void MessageDelegate(string message);
        private MessageDelegate _messageDelegate;

        public MainWindow()
        {
            InitializeComponent();
            cyberBot = new CyberBot();

            // Initialize delegate
            _messageDelegate = new MessageDelegate(LogMessageToFile);

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        // Delegate target method
        private void LogMessageToFile(string message)
        {
            try
            {
                string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "delegate_log.txt");
                System.IO.File.AppendAllText(logFile, $"{DateTime.Now:HH:mm:ss}: {message}{Environment.NewLine}");
            }
            catch { }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ActivityLogger.LogSessionBoundary(isStart: true);
            DatabaseManager.Initialise(out _);
            _ = Task.Run(() => PlayVoiceGreeting());

            await AddBotMessage("🛡️ Welcome to the Cybersecurity Awareness Bot — Part 3!");
            await Task.Delay(200);
            await AddBotMessage("I can help you with cybersecurity topics, quiz you, and keep an activity log.");
            await Task.Delay(200);
            await AddBotMessage("What's your name?");

            // Invoke delegate
            _messageDelegate?.Invoke("Application started");
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            ActivityLogger.LogSessionBoundary(isStart: false);
            _messageDelegate?.Invoke("Application closed");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessUserInput();
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                await ProcessUserInput();
            }
        }

        private async Task ProcessUserInput()
        {
            string userInput = InputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userInput)) return;

            SwitchTab("chat");

            AddUserMessage(userInput);
            InputTextBox.Clear();
            InputTextBox.Focus();

            ShowTypingIndicator(true);
            string response = await Task.Run(() => BotHandler.ProcessInput(userInput, cyberBot));
            ShowTypingIndicator(false);
            await AddBotMessage(response);

            // Invoke delegate for user interaction
            _messageDelegate?.Invoke($"User: {userInput.Substring(0, Math.Min(userInput.Length, 50))}");

            UpdateQuizPanel();

            await Task.Delay(50);
            ChatScrollViewer.ScrollToBottom();
        }

        private void AddUserMessage(string message)
        {
            Border border = new Border { Style = (Style)FindResource("UserBubble") };
            TextBlock text = new TextBlock
            {
                Text = $"👤 {message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14
            };
            border.Child = text;
            ChatPanel.Children.Add(border);
        }

        private async Task AddBotMessage(string message)
        {
            Border border = new Border { Style = (Style)FindResource("BotBubble") };
            TextBlock text = new TextBlock
            {
                Text = "🤖 ",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
            };
            border.Child = text;
            ChatPanel.Children.Add(border);
            ChatScrollViewer.ScrollToBottom();

            string fullMessage = message;
            for (int i = 0; i <= fullMessage.Length; i++)
            {
                text.Text = $"🤖 {fullMessage.Substring(0, i)}";
                await Task.Delay(6);
                ChatScrollViewer.ScrollToBottom();
            }

            ApplyColourFormatting(text, fullMessage);
            ChatScrollViewer.ScrollToBottom();
            UpdateQuizPanel();
        }

        private void PlayVoiceGreeting()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.wav");
                if (System.IO.File.Exists(path))
                {
                    using (SoundPlayer player = new SoundPlayer(path))
                    {
                        player.PlaySync();
                    }
                }
            }
            catch { }
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            QuizPanelBorder.Visibility = Visibility.Collapsed;
            ActivityLogger.Log("Chat cleared by user");
            await AddBotMessage("✨ Chat cleared! I'm still here — type 'help' to see what I can do.");
            _messageDelegate?.Invoke("Chat cleared");
        }

        private Border? _typingBubble;

        private void ShowTypingIndicator(bool show)
        {
            if (show)
            {
                _typingBubble = new Border { Style = (Style)FindResource("BotBubble") };
                TextBlock tb = new TextBlock
                {
                    Text = "🤖  typing…",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                };
                _typingBubble.Child = tb;
                ChatPanel.Children.Add(_typingBubble);
                ChatScrollViewer.ScrollToBottom();
            }
            else
            {
                if (_typingBubble != null && ChatPanel.Children.Contains(_typingBubble))
                    ChatPanel.Children.Remove(_typingBubble);
                _typingBubble = null;
            }
        }

        private static void ApplyColourFormatting(TextBlock tb, string fullText)
        {
            tb.Text = string.Empty;
            tb.Inlines.Clear();

            foreach (string rawLine in fullText.Split('\n'))
            {
                Run run = new Run(rawLine) { Foreground = PickLineColour(rawLine) };
                tb.Inlines.Add(run);
                tb.Inlines.Add(new LineBreak());
            }
        }

        private static Brush PickLineColour(string line)
        {
            if (line.StartsWith("🎣") || line.StartsWith("🔑") || line.StartsWith("🌐") ||
                line.StartsWith("🛡️") || line.StartsWith("🔒") || line.StartsWith("🎭") ||
                line.StartsWith("🦠") || line.StartsWith("🔏") || line.StartsWith("🚨") ||
                line.StartsWith("📝") || line.StartsWith("📜") || line.StartsWith("⭐") ||
                line.StartsWith("📋") || line.StartsWith("📄") || line.StartsWith("🎮") ||
                line.StartsWith("🤖"))
                return new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));

            if (line.TrimStart().StartsWith("✓") || line.StartsWith("✅"))
                return new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));

            if (line.TrimStart().StartsWith("✗") || line.StartsWith("❌"))
                return new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));

            if (line.TrimStart().StartsWith("•"))
                return new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xCC));

            if (line.TrimStart().StartsWith("💡"))
                return new SolidColorBrush(Color.FromRgb(0xF0, 0x88, 0x3E));

            return new SolidColorBrush(Color.FromRgb(0xC9, 0xD1, 0xD9));
        }

        // TAB NAVIGATION
        private void TabChat_Click(object sender, RoutedEventArgs e) => SwitchTab("chat");
        private void TabTasks_Click(object sender, RoutedEventArgs e) { SwitchTab("tasks"); LoadTaskPanel(); }
        private void TabQuiz_Click(object sender, RoutedEventArgs e) => SwitchTab("quiz");
        private void TabLog_Click(object sender, RoutedEventArgs e) { SwitchTab("log"); LoadLogPanel(); }

        private void SwitchTab(string tab)
        {
            PanelChat.Visibility = tab == "chat" ? Visibility.Visible : Visibility.Collapsed;
            PanelTasks.Visibility = tab == "tasks" ? Visibility.Visible : Visibility.Collapsed;
            PanelQuiz.Visibility = tab == "quiz" ? Visibility.Visible : Visibility.Collapsed;
            PanelLog.Visibility = tab == "log" ? Visibility.Visible : Visibility.Collapsed;

            TabChat.Style = (Style)FindResource(tab == "chat" ? "TabButtonActive" : "TabButton");
            TabTasks.Style = (Style)FindResource(tab == "tasks" ? "TabButtonActive" : "TabButton");
            TabQuiz.Style = (Style)FindResource(tab == "quiz" ? "TabButtonActive" : "TabButton");
            TabLog.Style = (Style)FindResource(tab == "log" ? "TabButtonActive" : "TabButton");
        }

        // QUIZ PANEL METHODS
        private void UpdateQuizPanel()
        {
            if (!QuizEngine.IsActive)
            {
                QuizPanelBorder.Visibility = Visibility.Collapsed;
                return;
            }
            QuizPanelBorder.Visibility = Visibility.Visible;
            RenderQuizButtons();
        }

        private void RenderQuizButtons()
        {
            QuizOptionsPanel.Children.Clear();
            string[] labels = { "A", "B", "C", "D" };
            foreach (string label in labels)
            {
                Button btn = new Button
                {
                    Content = $"  {label}",
                    Style = (Style)FindResource("QuizButton"),
                    Tag = label,
                };
                btn.Click += QuizOptionButton_Click;
                QuizOptionsPanel.Children.Add(btn);
            }
            QuizProgressText.Text = "Quiz active — click A/B/C/D or type your answer.";
        }

        private async void QuizOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                InputTextBox.Text = tag;
                await ProcessUserInput();
            }
        }

        // TASK ASSISTANT
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                TaskStatusText.Text = "⚠️ Please enter a task title.";
                return;
            }

            var task = new TaskItem
            {
                Title = title,
                Description = GenerateDescription(title),
                ReminderDate = TaskReminderDate.SelectedDate,
            };

            if (DatabaseManager.AddTask(task, out string error))
            {
                ActivityLogger.Log($"Task added: {title}");
                TaskTitleBox.Text = string.Empty;
                TaskReminderDate.SelectedDate = null;
                TaskStatusText.Text = $"✅ Task '{title}' added!";
                LoadTaskPanel();
                _messageDelegate?.Invoke($"Task added: {title}");
            }
            else
            {
                TaskStatusText.Text = $"⚠️ {error}";
            }
        }

        private void RefreshTasks_Click(object sender, RoutedEventArgs e) => LoadTaskPanel();

        private void LoadTaskPanel()
        {
            TaskListPanel.Children.Clear();
            var tasks = DatabaseManager.GetAllTasks(out string error);

            if (tasks.Count == 0)
            {
                TaskListPanel.Children.Add(new TextBlock
                {
                    Text = "No tasks yet. Add one above!",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    FontSize = 13,
                    Margin = new Thickness(0, 10, 0, 0),
                });
                return;
            }

            foreach (var t in tasks)
            {
                Border row = new Border { Style = (Style)FindResource("TaskRow") };
                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel info = new StackPanel();
                info.Children.Add(new TextBlock
                {
                    Text = $"{t.StatusDisplay}  [{t.Id}]  {t.Title}",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(t.IsComplete ? Color.FromRgb(0x48, 0x4F, 0x58) : Color.FromRgb(0xC9, 0xD1, 0xD9)),
                });
                info.Children.Add(new TextBlock
                {
                    Text = t.Description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    TextWrapping = TextWrapping.Wrap,
                });
                if (t.ReminderDate.HasValue)
                {
                    info.Children.Add(new TextBlock
                    {
                        Text = t.ReminderDisplay,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0x88, 0x3E)),
                    });
                }
                Grid.SetColumn(info, 0);

                Button completeBtn = new Button
                {
                    Content = "✅ Done",
                    Style = (Style)FindResource("ModernButton"),
                    Width = 75,
                    Height = 30,
                    FontSize = 11,
                    Tag = t.Id,
                    IsEnabled = !t.IsComplete,
                };
                completeBtn.Click += CompleteTaskBtn_Click;
                Grid.SetColumn(completeBtn, 1);

                Button deleteBtn = new Button
                {
                    Content = "🗑️ Del",
                    Style = (Style)FindResource("DangerButton"),
                    Width = 65,
                    Height = 30,
                    FontSize = 11,
                    Margin = new Thickness(6, 0, 0, 0),
                    Tag = t.Id,
                };
                deleteBtn.Click += DeleteTaskBtn_Click;
                Grid.SetColumn(deleteBtn, 2);

                g.Children.Add(info);
                g.Children.Add(completeBtn);
                g.Children.Add(deleteBtn);
                row.Child = g;
                TaskListPanel.Children.Add(row);
            }
        }

        private void CompleteTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                DatabaseManager.MarkComplete(id, out _);
                ActivityLogger.Log($"Task {id} completed");
                LoadTaskPanel();
                _messageDelegate?.Invoke($"Task {id} completed");
            }
        }

        private void DeleteTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                DatabaseManager.DeleteTask(id, out _);
                ActivityLogger.Log($"Task {id} deleted");
                LoadTaskPanel();
                _messageDelegate?.Invoke($"Task {id} deleted");
            }
        }

        private static string GenerateDescription(string title)
        {
            string l = title.ToLowerInvariant();
            if (l.Contains("2fa")) return "Enable two-factor authentication for extra security.";
            if (l.Contains("password")) return "Review and update passwords to be strong and unique.";
            if (l.Contains("backup")) return "Back up your data following the 3-2-1 rule.";
            if (l.Contains("vpn")) return "Set up a VPN to secure your internet connection.";
            if (l.Contains("update")) return "Apply the latest security updates to patch vulnerabilities.";
            return $"Complete the cybersecurity task: {title}.";
        }

        // QUIZ TAB
        private void StartQuizTab_Click(object sender, RoutedEventArgs e)
        {
            ActivityLogger.Log("Quiz started via Quiz tab");
            string firstQuestion = QuizEngine.StartQuiz();
            RenderQuizTabQuestion(firstQuestion);
            QuizTabStatus.Text = $"Round in progress — {QuizEngine.Score} correct so far.";
            _messageDelegate?.Invoke("Quiz started");
        }

        private void RenderQuizTabQuestion(string questionBlock)
        {
            QuizTabOptions.Children.Clear();
            QuizTabFeedback.Text = string.Empty;

            string[] lines = questionBlock.Split('\n');
            string question = string.Empty;
            var options = new List<string>();

            foreach (string line in lines)
            {
                string t = line.Trim();
                if (t.StartsWith("A)") || t.StartsWith("B)") || t.StartsWith("C)") || t.StartsWith("D)"))
                    options.Add(t);
                else if (!t.StartsWith("🎮") && !t.StartsWith("──") && !string.IsNullOrWhiteSpace(t))
                    question += t + " ";
            }

            QuizTabQuestion.Text = question.Trim();

            foreach (string opt in options)
            {
                string captured = opt;
                Button btn = new Button
                {
                    Content = captured,
                    Style = (Style)FindResource("QuizButton"),
                    Margin = new Thickness(0, 4, 0, 0),
                    Tag = captured[0].ToString(),
                };
                btn.Click += QuizTabOption_Click;
                QuizTabOptions.Children.Add(btn);
            }
        }

        private void QuizTabOption_Click(object sender, RoutedEventArgs e)
        {
            if (!QuizEngine.IsActive) return;
            if (sender is not Button btn) return;

            string answer = btn.Tag?.ToString() ?? string.Empty;
            string feedback = QuizEngine.SubmitAnswer(answer);
            ActivityLogger.Log($"Quiz answer: {answer}");

            if (QuizEngine.IsActive)
            {
                string[] parts = feedback.Split(new[] { "\n\n" }, 2, StringSplitOptions.None);
                QuizTabFeedback.Text = parts[0];
                QuizTabFeedback.Foreground = parts[0].StartsWith("✅")
                    ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                    : new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));

                if (parts.Length > 1)
                    RenderQuizTabQuestion(parts[1]);
            }
            else
            {
                QuizTabOptions.Children.Clear();
                QuizTabQuestion.Text = string.Empty;
                QuizTabFeedback.Text = feedback;
                QuizTabFeedback.Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
                QuizTabStatus.Text = $"Session total: {QuizEngine.TotalScore} / {QuizEngine.TotalPlayed}";
                ActivityLogger.Log($"Quiz completed — score: {QuizEngine.TotalScore}/{QuizEngine.TotalPlayed}");
                _messageDelegate?.Invoke($"Quiz completed: {QuizEngine.TotalScore}/{QuizEngine.TotalPlayed}");
            }
        }

        // ACTIVITY LOG TAB
        private void RefreshLog_Click(object sender, RoutedEventArgs e) => LoadLogPanel();

        private void ShowMoreLog_Click(object sender, RoutedEventArgs e)
        {
            string more = ActivityLogger.ShowMore();
            LogPanel.Text = more;
            ShowMoreBtn.IsEnabled = ActivityLogger.HasMore;
        }

        private void LoadLogPanel()
        {
            string logText = ActivityLogger.ViewLog();
            LogPanel.Text = logText;
            ShowMoreBtn.IsEnabled = ActivityLogger.HasMore;
        }
    }
}