using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using SvonyBrowser.Services;

namespace SvonyBrowser.Controls
{

    /// <summary>
    /// Interaction logic for ProtocolExplorer.xaml
    /// </summary>
    public partial class ProtocolExplorer : UserControl
    {
        #region Fields

        private readonly ProtocolHandler _protocolHandler;
        private ProtocolAction? _selectedAction;
        private Dictionary<string, List<ProtocolAction>> _categorizedActions = new Dictionary<string, List<ProtocolAction>>();

        #endregion

        #region Events

        /// <summary>
        /// Fired when an action is selected for testing in chat.
        /// </summary>
        public event Action<string> TestInChatRequested;

        #endregion

        #region Constructor

        public ProtocolExplorer()
        {
            InitializeComponent();
            _protocolHandler = ProtocolHandler.Instance;
        
            Loaded += ProtocolExplorer_Loaded;
        }

        #endregion

        #region Event Handlers

        private async void ProtocolExplorer_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_protocolHandler.IsInitialized)
            {
                await _protocolHandler.InitializeAsync();
            }

            LoadActions();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.ToLowerInvariant() ?? "";
            FilterActions(searchText);
        }

        private void ActionsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is ProtocolAction action)
            {
                ShowActionDetails(action);
            }
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAction == null) return;

            var example = GenerateExampleRequest(_selectedAction);
            Clipboard.SetText(example);

            MessageBox.Show("Copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TestInChat_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAction == null) return;

            TestInChatRequested?.Invoke($"Look up protocol action: {_selectedAction.Name}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the action list.
        /// </summary>
        public void RefreshActions()
        {
            LoadActions();
        }

        /// <summary>
        /// Selects an action by name.
        /// </summary>
        public void SelectAction(string actionName)
        {
            var action = _protocolHandler.LookupAction(actionName);
            if (action != null)
            {
                ShowActionDetails(action);
            
                // Expand and select in tree
                foreach (TreeViewItem categoryItem in ActionsTreeView.Items)
                {
                    foreach (TreeViewItem actionItem in categoryItem.Items)
                    {
                        if (actionItem.Tag is ProtocolAction a && a.Name == actionName)
                        {
                            categoryItem.IsExpanded = true;
                            actionItem.IsSelected = true;
                            actionItem.BringIntoView();
                            return;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void LoadActions()
        {
            ActionsTreeView.Items.Clear();
            _categorizedActions.Clear();

            // Get all categories
            var categories = _protocolHandler.GetCategories().OrderBy(c => c).ToList();

            foreach (var category in categories)
            {
                var actions = _protocolHandler.GetActionsByCategory(category).OrderBy(a => a.Name).ToList();
                _categorizedActions[category] = actions;

                var categoryItem = new TreeViewItem
                {
                    Header = $"📁 {char.ToUpper(category[0])}{category.Substring(1)} ({actions.Count})",
                    IsExpanded = false
                };

                foreach (var action in actions)
                {
                    var actionItem = new TreeViewItem
                    {
                        Header = action.Name,
                        Tag = action
                    };
                    categoryItem.Items.Add(actionItem);
                }

                ActionsTreeView.Items.Add(categoryItem);
            }
        }

        private void FilterActions(string searchText)
        {
            ActionsTreeView.Items.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                LoadActions();
                return;
            }

            var matchingActions = _protocolHandler.SearchActions(searchText, 100).ToList();
            var grouped = matchingActions.GroupBy(a => a.Category ?? "other").OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var categoryItem = new TreeViewItem
                {
                    Header = $"📁 {char.ToUpper(group.Key[0])}{group.Key.Substring(1)} ({group.Count()})",
                    IsExpanded = true
                };

                foreach (var action in group.OrderBy(a => a.Name))
                {
                    var actionItem = new TreeViewItem
                    {
                        Header = action.Name,
                        Tag = action
                    };
                    categoryItem.Items.Add(actionItem);
                }

                ActionsTreeView.Items.Add(categoryItem);
            }
        }

        private void ShowActionDetails(ProtocolAction action)
        {
            _selectedAction = action;
            DetailsPanel.Visibility = Visibility.Visible;

            ActionNameText.Text = action.Name;
            ActionCategoryText.Text = $"Category: {action.Category ?? "unknown"}";
            DescriptionText.Text = action.Description ?? "No description available.";
            CommandIdText.Text = action.CommandId.ToString();

            // Parameters
            var parameters = action.Parameters?.Select(p => new ParameterViewModel
            {
                Name = p.Key,
                Type = p.Value
            }).ToList() ?? new List<ParameterViewModel>();

            ParametersItemsControl.ItemsSource = parameters;

            // Example request
            ExampleRequestText.Text = GenerateExampleRequest(action);
        }

        private string GenerateExampleRequest(ProtocolAction action)
        {
            var example = new Dictionary<string, object>
            {
                ["cmd"] = action.CommandId,
                ["action"] = action.Name,
                ["seq"] = 1,
                ["data"] = GenerateExampleData(action.Parameters)
            };

            return JsonConvert.SerializeObject(example, Formatting.Indented);
        }

        private Dictionary<string, object> GenerateExampleData(Dictionary<string, string> parameters)
        {
            var data = new Dictionary<string, object>();

            if (parameters == null) return data;

            foreach (var param in parameters)
            {
                data[param.Key] = param.Value switch
                {
                    "int" => 1,
                    "string" => "example",
                    "bool" => true,
                    "array" => new object[] { },
                    "object" => new Dictionary<string, object>(),
                    _ => null!
                };
            }

            return data;
        }

        #endregion
    }

    /// <summary>
    /// View model for parameters.
    /// </summary>
    public class ParameterViewModel
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

}