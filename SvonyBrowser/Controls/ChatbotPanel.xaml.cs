using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using SvonyBrowser.Services;

namespace SvonyBrowser.Controls
{

    /// <summary>
    /// Interaction logic for ChatbotPanel.xaml
    /// </summary>
    public partial class ChatbotPanel : UserControl
    {
        #region Fields

        private readonly ChatbotService _chatbotService;
        private readonly ObservableCollection<ChatMessageViewModel> _messages = new ObservableCollection<ChatMessageViewModel>();
        private CancellationTokenSource _currentRequestCts;
        private bool _isProcessing = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the messages collection.
        /// </summary>
        public ObservableCollection<ChatMessageViewModel> Messages => _messages;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a protocol action is selected for lookup.
        /// </summary>
        public event Action<string> ProtocolLookupRequested;

        /// <summary>
        /// Fired when traffic analysis is requested.
        /// </summary>
        public event Action TrafficAnalysisRequested;

        #endregion

        #region Constructor

        public ChatbotPanel()
        {
            InitializeComponent();

            _chatbotService = ChatbotService.Instance;
            MessagesItemsControl.ItemsSource = _messages;

            // Subscribe to chatbot events
            _chatbotService.MessageAdded += OnMessageAdded;
            _chatbotService.ProcessingStateChanged += OnProcessingStateChanged;
            _chatbotService.ErrorOccurred += OnErrorOccurred;

            // Add welcome message
            AddWelcomeMessage();

            Loaded += ChatbotPanel_Loaded;
            Unloaded += ChatbotPanel_Unloaded;
        }

        #endregion

        #region Event Handlers

        private void ChatbotPanel_Loaded(object sender, RoutedEventArgs e)
        {
            InputTextBox.Focus();
            UpdateStatus();
        }

        private void ChatbotPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            _currentRequestCts?.Cancel();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();

            var clearItem = new MenuItem { Header = "Clear History" };
            clearItem.Click += (s, args) => ClearHistory();
            contextMenu.Items.Add(clearItem);

            var exportItem = new MenuItem { Header = "Export Chat" };
            exportItem.Click += (s, args) => ExportChat();
            contextMenu.Items.Add(exportItem);

            contextMenu.Items.Add(new Separator());

            var settingsItem = new MenuItem { Header = "Settings" };
            settingsItem.Click += (s, args) => OpenSettings();
            contextMenu.Items.Add(settingsItem);

            contextMenu.IsOpen = true;
        }

        private void QuickAction_ProtocolLookup(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "Look up protocol action: ";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        private void QuickAction_Calculator(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "Calculate training ";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        private void QuickAction_Traffic(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "Show recent traffic";
            InputTextBox.Focus();
            _ = SendMessageAsync();
        }

        private void QuickAction_Knowledge(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "Search knowledge base: ";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        private void QuickAction_BuildOrder(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "Generate balanced build order";
            InputTextBox.Focus();
            _ = SendMessageAsync();
        }

        private void OnMessageAdded(ChatMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                var vm = new ChatMessageViewModel(message);
                _messages.Add(vm);
                ScrollToBottom();
            });
        }

        private void OnProcessingStateChanged(bool isProcessing)
        {
            Dispatcher.Invoke(() =>
            {
                _isProcessing = isProcessing;
                LoadingOverlay.Visibility = isProcessing ? Visibility.Visible : Visibility.Collapsed;
                SendButton.IsEnabled = !isProcessing;
                InputTextBox.IsEnabled = !isProcessing;

                if (isProcessing)
                {
                    StatusText.Text = "Thinking...";
                    StartLoadingAnimation();
                }
                else
                {
                    StatusText.Text = "Ready";
                    StopLoadingAnimation();
                }
            });
        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Error: {error}";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a message programmatically.
        /// </summary>
        public async Task SendMessageAsync(string message)
        {
            InputTextBox.Text = message;
            await SendMessageAsync();
        }

        /// <summary>
        /// Clears the chat history.
        /// </summary>
        public void ClearHistory()
        {
            _messages.Clear();
            _chatbotService.ClearHistory();
            AddWelcomeMessage();
        }

        /// <summary>
        /// Updates the game context.
        /// </summary>
        public void UpdateContext(ChatContext context)
        {
            _chatbotService.UpdateContext(context);
        }

        #endregion

        #region Private Methods

        private async Task SendMessageAsync()
        {
            var message = InputTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(message)) return;
            if (_isProcessing) return;

            InputTextBox.Text = "";
            InputTextBox.Focus();

            try
            {
                _currentRequestCts = new CancellationTokenSource();
                await _chatbotService.SendMessageAsync(message, _currentRequestCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Error sending message");
            }
            finally
            {
                _currentRequestCts?.Dispose();
                _currentRequestCts = null;
            }
        }

        private void AddWelcomeMessage()
        {
            var welcomeMessage = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = "👋 Hello! I'm your Evony Co-Pilot. I can help you with:\n\n" +
                          "• **Protocol Analysis** - Look up game commands and decode traffic\n" +
                          "• **Calculations** - Training costs, march times, combat estimates\n" +
                          "• **Knowledge** - Game mechanics, strategies, and tips\n" +
                          "• **Automation** - Build orders, attack plans, and more\n\n" +
                          "How can I assist you today?",
                Timestamp = DateTime.UtcNow
            };

            var vm = new ChatMessageViewModel(welcomeMessage);
            _messages.Add(vm);
        }

        private void UpdateStatus()
        {
            var mcpManager = McpConnectionManager.Instance;
            var statuses = mcpManager.GetAllStatuses();

            var connectedCount = 0;
            foreach (var status in statuses.Values)
            {
                if (status == McpConnectionStatus.Connected)
                    connectedCount++;
            }

            if (connectedCount == statuses.Count && connectedCount > 0)
            {
                StatusText.Text = "All systems ready";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else if (connectedCount > 0)
            {
                StatusText.Text = $"{connectedCount}/{statuses.Count} servers connected";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else
            {
                StatusText.Text = "Connecting...";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
        }

        private void ScrollToBottom()
        {
            MessagesScrollViewer.ScrollToEnd();
        }

        private void StartLoadingAnimation()
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };
            LoadingRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
        }

        private void StopLoadingAnimation()
        {
            LoadingRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
        }

        private void ExportChat()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|JSON files (*.json)|*.json",
                    DefaultExt = ".md",
                    FileName = $"chat-export-{DateTime.Now:yyyyMMdd-HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var extension = System.IO.Path.GetExtension(dialog.FileName).ToLower();
                    string content;

                    switch (extension)
                    {
                        case ".json":
                            content = JsonConvert.SerializeObject(_messages, new JsonSerializerSettings { Formatting = Formatting.Indented });
                            break;
                        case ".txt":
                            content = string.Join("\n\n", _messages.Select(m => $"[{m.Timestamp:HH:mm:ss}] {m.Role}: {m.Content}"));
                            break;
                        default: // .md
                            var sb = new System.Text.StringBuilder();
                            sb.AppendLine("# Chat Export");
                            sb.AppendLine($"*Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*\n");
                            foreach (var msg in _messages)
                            {
                                sb.AppendLine($"## {msg.Role} ({msg.Timestamp:HH:mm:ss})\n");
                                sb.AppendLine(msg.Content);
                                sb.AppendLine();
                            }
                            content = sb.ToString();
                            break;
                    }

                    System.IO.File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show($"Chat exported to {dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSettings()
        {
            try
            {
                var settingsWindow = new SettingsControlCenter();
                settingsWindow.Owner = Window.GetWindow(this);
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    /// <summary>
    /// View model for chat messages.
    /// </summary>
    public class ChatMessageViewModel
    {
        private readonly ChatMessage _message;

        public ChatMessageViewModel(ChatMessage message)
        {
            _message = message;
        }

        public string Content => _message.Content;
    
        public DateTime Timestamp => _message.Timestamp;
    
        public ChatRole Role => _message.Role;

        public string TimestampFormatted => _message.Timestamp.ToLocalTime().ToString("HH:mm");

        public string SourcesFormatted => _message.Sources?.Count > 0
            ? $"📎 {string.Join(", ", _message.Sources)}"
            : "";

        public bool HasSources => _message.Sources?.Count > 0;

        public HorizontalAlignment HorizontalAlignment =>
            _message.Role == ChatRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public Brush BackgroundBrush
        {
            get
            {
                if (_message.IsError)
                    return new SolidColorBrush(Color.FromRgb(127, 29, 29));

                return _message.Role == ChatRole.User
                    ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                    : new SolidColorBrush(Color.FromRgb(55, 65, 81));
            }
        }
    }

}