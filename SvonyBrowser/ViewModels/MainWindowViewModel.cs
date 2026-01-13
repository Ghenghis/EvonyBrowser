using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using SvonyBrowser.Services;

namespace SvonyBrowser.ViewModels
{
    /// <summary>
    /// ViewModel for MainWindow - handles panel visibility, browser state, and tool outputs.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private bool _isLeftPanelVisible = true;
        private bool _isRightPanelVisible = true;
        private string _leftPanelUrl = string.Empty;
        private string _rightPanelUrl = string.Empty;
        private string _amfInput = string.Empty;
        private string _amfOutput = string.Empty;
        private int _troopAmount;
        private string _selectedTroopType = "Warrior";
        private string _statusMessage = "Ready";
        private bool _isLoading;
        private bool _isChatbotVisible;
        private bool _isTrafficViewerVisible;
        private bool _isProtocolExplorerVisible;
        private double _leftPanelZoom = 100;
        private double _rightPanelZoom = 100;
        private string _selectedServer = string.Empty;

        #endregion

        #region Panel Visibility Properties

        /// <summary>
        /// Gets or sets whether the left (Bot) panel is visible.
        /// </summary>
        public bool IsLeftPanelVisible
        {
            get => _isLeftPanelVisible;
            set => SetProperty(ref _isLeftPanelVisible, value);
        }

        /// <summary>
        /// Gets or sets whether the right (Client) panel is visible.
        /// </summary>
        public bool IsRightPanelVisible
        {
            get => _isRightPanelVisible;
            set => SetProperty(ref _isRightPanelVisible, value);
        }

        /// <summary>
        /// Gets or sets whether the chatbot panel is visible.
        /// </summary>
        public bool IsChatbotVisible
        {
            get => _isChatbotVisible;
            set => SetProperty(ref _isChatbotVisible, value);
        }

        /// <summary>
        /// Gets or sets whether the traffic viewer is visible.
        /// </summary>
        public bool IsTrafficViewerVisible
        {
            get => _isTrafficViewerVisible;
            set => SetProperty(ref _isTrafficViewerVisible, value);
        }

        /// <summary>
        /// Gets or sets whether the protocol explorer is visible.
        /// </summary>
        public bool IsProtocolExplorerVisible
        {
            get => _isProtocolExplorerVisible;
            set => SetProperty(ref _isProtocolExplorerVisible, value);
        }

        #endregion

        #region Browser Properties

        /// <summary>
        /// Gets or sets the URL loaded in the left browser panel.
        /// </summary>
        public string LeftPanelUrl
        {
            get => _leftPanelUrl;
            set => SetProperty(ref _leftPanelUrl, value);
        }

        /// <summary>
        /// Gets or sets the URL loaded in the right browser panel.
        /// </summary>
        public string RightPanelUrl
        {
            get => _rightPanelUrl;
            set => SetProperty(ref _rightPanelUrl, value);
        }

        /// <summary>
        /// Gets or sets the zoom level for the left panel (percentage).
        /// </summary>
        public double LeftPanelZoom
        {
            get => _leftPanelZoom;
            set => SetProperty(ref _leftPanelZoom, Math.Clamp(value, 25, 500));
        }

        /// <summary>
        /// Gets or sets the zoom level for the right panel (percentage).
        /// </summary>
        public double RightPanelZoom
        {
            get => _rightPanelZoom;
            set => SetProperty(ref _rightPanelZoom, Math.Clamp(value, 25, 500));
        }

        /// <summary>
        /// Gets or sets the selected server.
        /// </summary>
        public string SelectedServer
        {
            get => _selectedServer;
            set => SetProperty(ref _selectedServer, value);
        }

        #endregion

        #region AMF Decoder Properties

        /// <summary>
        /// Gets or sets the AMF decoder input text.
        /// </summary>
        public string AmfInput
        {
            get => _amfInput;
            set => SetProperty(ref _amfInput, value);
        }

        /// <summary>
        /// Gets or sets the AMF decoder output text.
        /// </summary>
        public string AmfOutput
        {
            get => _amfOutput;
            set => SetProperty(ref _amfOutput, value);
        }

        #endregion

        #region Troop Calculator Properties

        /// <summary>
        /// Gets or sets the troop amount for calculation.
        /// </summary>
        public int TroopAmount
        {
            get => _troopAmount;
            set => SetProperty(ref _troopAmount, Math.Max(0, value));
        }

        /// <summary>
        /// Gets or sets the selected troop type.
        /// </summary>
        public string SelectedTroopType
        {
            get => _selectedTroopType;
            set => SetProperty(ref _selectedTroopType, value);
        }

        /// <summary>
        /// Gets the available troop types.
        /// </summary>
        public ObservableCollection<string> TroopTypes { get; } = new ObservableCollection<string>
        {
            "Warrior", "Scout", "Pikeman", "Swordsman", "Archer",
            "Cavalry", "Cataphract", "Transporter", "Ballista",
            "Battering Ram", "Catapult", "Trebuchet"
        };

        #endregion

        #region Status Properties

        /// <summary>
        /// Gets or sets the status bar message.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets whether a loading operation is in progress.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Service Status Properties

        /// <summary>
        /// Gets whether the MCP connection is active.
        /// </summary>
        public bool IsMcpConnected => McpConnectionManager.Instance.IsConnected;

        /// <summary>
        /// Gets whether the Fiddler proxy is connected.
        /// </summary>
        public bool IsFiddlerConnected => FiddlerBridge.Instance.IsConnected;

        /// <summary>
        /// Gets whether the LLM service is connected.
        /// </summary>
        public bool IsLlmConnected => LlmIntegrationService.Instance.IsConnected;

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            // Subscribe to service events for status updates
            SubscribeToServiceEvents();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows only the left (Bot) panel.
        /// </summary>
        public void ShowLeftOnly()
        {
            IsLeftPanelVisible = true;
            IsRightPanelVisible = false;
            StatusMessage = "Showing Bot panel only";
        }

        /// <summary>
        /// Shows only the right (Client) panel.
        /// </summary>
        public void ShowRightOnly()
        {
            IsLeftPanelVisible = false;
            IsRightPanelVisible = true;
            StatusMessage = "Showing Client panel only";
        }

        /// <summary>
        /// Shows both panels.
        /// </summary>
        public void ShowBothPanels()
        {
            IsLeftPanelVisible = true;
            IsRightPanelVisible = true;
            StatusMessage = "Showing both panels";
        }

        /// <summary>
        /// Swaps the panel positions.
        /// </summary>
        public void SwapPanels()
        {
            var tempUrl = LeftPanelUrl;
            LeftPanelUrl = RightPanelUrl;
            RightPanelUrl = tempUrl;

            var tempZoom = LeftPanelZoom;
            LeftPanelZoom = RightPanelZoom;
            RightPanelZoom = tempZoom;

            StatusMessage = "Panels swapped";
        }

        /// <summary>
        /// Decodes AMF data from input and sets output.
        /// </summary>
        public async Task DecodeAmfAsync()
        {
            if (string.IsNullOrWhiteSpace(AmfInput))
            {
                AmfOutput = "Please enter AMF data to decode";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Decoding AMF...";

                // Use ProtocolHandler to decode AMF
                var decoded = await Task.Run(() => 
                    ProtocolHandler.Instance.DecodeAmfData(AmfInput));

                AmfOutput = decoded ?? "Failed to decode AMF data";
                StatusMessage = "AMF decoded successfully";
            }
            catch (Exception ex)
            {
                AmfOutput = $"Error: {ex.Message}";
                StatusMessage = "AMF decode failed";
                App.Logger.Error(ex, "AMF decode failed");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Calculates troop statistics.
        /// </summary>
        public string CalculateTroops()
        {
            if (TroopAmount <= 0)
                return "Please enter a valid troop amount";

            // Basic troop calculation - can be expanded with game data
            var result = $"Troop Type: {SelectedTroopType}\n" +
                        $"Amount: {TroopAmount:N0}\n" +
                        $"Food Cost: {TroopAmount * 10:N0}\n" +
                        $"Wood Cost: {TroopAmount * 5:N0}\n" +
                        $"Stone Cost: {TroopAmount * 3:N0}\n" +
                        $"Iron Cost: {TroopAmount * 2:N0}\n" +
                        $"Training Time: {TimeSpan.FromSeconds(TroopAmount * 0.5):g}";

            StatusMessage = "Troop calculation complete";
            return result;
        }

        /// <summary>
        /// Refreshes service status properties.
        /// </summary>
        public void RefreshServiceStatus()
        {
            OnPropertiesChanged(
                nameof(IsMcpConnected),
                nameof(IsFiddlerConnected),
                nameof(IsLlmConnected));
        }

        private void SubscribeToServiceEvents()
        {
            // Subscribe to MCP connection changes
            McpConnectionManager.Instance.ConnectionStatusChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsMcpConnected));
                StatusMessage = e.IsConnected ? "MCP Connected" : "MCP Disconnected";
            };

            // Subscribe to Fiddler status changes
            FiddlerBridge.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FiddlerBridge.IsConnected))
                {
                    OnPropertyChanged(nameof(IsFiddlerConnected));
                }
            };

            // Subscribe to LLM status changes
            LlmIntegrationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LlmIntegrationService.IsConnected))
                {
                    OnPropertyChanged(nameof(IsLlmConnected));
                }
            };
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events if needed
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
