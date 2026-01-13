using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SvonyBrowser.Services;

namespace SvonyBrowser.Controls
{
    /// <summary>
    /// Customizable multi-row Status Bar with real-time widgets
    /// </summary>
    public partial class StatusBar : UserControl
    {
        private readonly StatusBarManager _manager;

        public StatusBar()
        {
            InitializeComponent();
            _manager = StatusBarManager.Instance;
            
            // Register all data providers
            _manager.RegisterMcpDataProviders();
            _manager.RegisterLlmDataProviders();
            _manager.RegisterGameStateDataProviders();
            _manager.RegisterNetworkDataProviders();
            _manager.RegisterAutomationDataProviders();
            _manager.RegisterSystemDataProviders();
            
            // Load default configuration
            _manager.LoadDefaultConfiguration();
            
            // Bind widgets to rows
            RefreshWidgetBindings();
            
            // Subscribe to config changes
            _manager.ConfigChanged += OnConfigChanged;
            
            // Start the update loop
            _manager.Start();
        }

        private void RefreshWidgetBindings()
        {
            Row1Widgets.ItemsSource = _manager.GetWidgetsForRow(0);
            Row2Widgets.ItemsSource = _manager.GetWidgetsForRow(1);
            Row3Widgets.ItemsSource = _manager.GetWidgetsForRow(2);
        }

        private void OnConfigChanged(object sender, StatusBarConfigChangedEventArgs e)
        {
            Dispatcher.Invoke(RefreshWidgetBindings);
        }

        #region Context Menu Handlers

        private void Customize_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StatusBarCustomizationDialog(_manager);
            if (dialog.ShowDialog() == true)
            {
                RefreshWidgetBindings();
            }
        }

        private void ToggleRow1_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var row = this.FindName("Row1Widgets") as ItemsControl;
            if (row != null && row.Parent is Border border)
            {
                border.Visibility = menuItem?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ToggleRow2_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var row = this.FindName("Row2Widgets") as ItemsControl;
            if (row != null && row.Parent is Border border)
            {
                border.Visibility = menuItem?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ToggleRow3_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var row = this.FindName("Row3Widgets") as ItemsControl;
            if (row != null && row.Parent is Border border)
            {
                border.Visibility = menuItem?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset Status Bar to default configuration?",
                "Reset Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _manager.LoadDefaultConfiguration();
                RefreshWidgetBindings();
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                FileName = "statusbar-config.json"
            };

            if (dialog.ShowDialog() == true)
            {
                _manager.SaveConfiguration(dialog.FileName);
                MessageBox.Show("Configuration saved successfully.", "Save Configuration", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                _manager.LoadConfiguration(dialog.FileName);
                RefreshWidgetBindings();
                MessageBox.Show("Configuration loaded successfully.", "Load Configuration", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        public void Dispose()
        {
            _manager.Stop();
            _manager.ConfigChanged -= OnConfigChanged;
        }
    }

    /// <summary>
    /// Dialog for customizing Status Bar widgets
    /// </summary>
    public class StatusBarCustomizationDialog : Window
    {
        private readonly StatusBarManager _manager;
        private readonly ListBox _availableWidgets;
        private readonly ListBox _activeWidgets;

        public StatusBarCustomizationDialog(StatusBarManager manager)
        {
            _manager = manager;
            Title = "Customize Status Bar";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(18, 18, 26));

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            // Headers
            var availableHeader = new TextBlock
            {
                Text = "Available Widgets",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 5, 0, 0)
            };
            Grid.SetColumn(availableHeader, 0);
            Grid.SetRow(availableHeader, 0);
            grid.Children.Add(availableHeader);

            var activeHeader = new TextBlock
            {
                Text = "Active Widgets",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 5, 0, 0)
            };
            Grid.SetColumn(activeHeader, 2);
            Grid.SetRow(activeHeader, 0);
            grid.Children.Add(activeHeader);

            // Available widgets list
            _availableWidgets = new ListBox
            {
                Margin = new Thickness(10),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(26, 26, 36)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(45, 45, 61))
            };
            Grid.SetColumn(_availableWidgets, 0);
            Grid.SetRow(_availableWidgets, 1);
            grid.Children.Add(_availableWidgets);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            var addButton = new Button { Content = "Add →", Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(10, 5, 10, 5) };
            addButton.Click += AddWidget_Click;
            buttonPanel.Children.Add(addButton);

            var removeButton = new Button { Content = "← Remove", Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(10, 5, 10, 5) };
            removeButton.Click += RemoveWidget_Click;
            buttonPanel.Children.Add(removeButton);

            var moveUpButton = new Button { Content = "↑ Up", Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(10, 5, 10, 5) };
            moveUpButton.Click += MoveUp_Click;
            buttonPanel.Children.Add(moveUpButton);

            var moveDownButton = new Button { Content = "↓ Down", Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(10, 5, 10, 5) };
            moveDownButton.Click += MoveDown_Click;
            buttonPanel.Children.Add(moveDownButton);

            Grid.SetColumn(buttonPanel, 1);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            // Active widgets list
            _activeWidgets = new ListBox
            {
                Margin = new Thickness(10),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(26, 26, 36)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(45, 45, 61))
            };
            Grid.SetColumn(_activeWidgets, 2);
            Grid.SetRow(_activeWidgets, 1);
            grid.Children.Add(_activeWidgets);

            // Dialog buttons
            var dialogButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5), Padding = new Thickness(10, 5, 10, 5) };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            dialogButtons.Children.Add(okButton);

            var cancelButton = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(5), Padding = new Thickness(10, 5, 10, 5) };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            dialogButtons.Children.Add(cancelButton);

            Grid.SetColumn(dialogButtons, 0);
            Grid.SetColumnSpan(dialogButtons, 3);
            Grid.SetRow(dialogButtons, 2);
            grid.Children.Add(dialogButtons);

            Content = grid;

            LoadWidgets();
        }

        private void LoadWidgets()
        {
            _availableWidgets.Items.Clear();
            _activeWidgets.Items.Clear();

            foreach (var widget in _manager.GetAvailableWidgets())
            {
                var item = new ListBoxItem
                {
                    Content = $"{widget.Title} ({widget.Category})",
                    Tag = widget
                };

                if (widget.IsActive)
                {
                    _activeWidgets.Items.Add(item);
                }
                else
                {
                    _availableWidgets.Items.Add(item);
                }
            }
        }

        private void AddWidget_Click(object sender, RoutedEventArgs e)
        {
            if (_availableWidgets.SelectedItem is ListBoxItem item && item.Tag is StatusBarWidget widget)
            {
                _manager.ActivateWidget(widget.Id, widget.DefaultRow, widget.DefaultPosition);
                LoadWidgets();
            }
        }

        private void RemoveWidget_Click(object sender, RoutedEventArgs e)
        {
            if (_activeWidgets.SelectedItem is ListBoxItem item && item.Tag is StatusBarWidget widget)
            {
                _manager.DeactivateWidget(widget.Id);
                LoadWidgets();
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_activeWidgets.SelectedItem is ListBoxItem item && item.Tag is StatusBarWidget widget)
            {
                var newPosition = Math.Max(0, widget.Position - 1);
                _manager.MoveWidget(widget.Id, widget.Row, newPosition);
                LoadWidgets();
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_activeWidgets.SelectedItem is ListBoxItem item && item.Tag is StatusBarWidget widget)
            {
                _manager.MoveWidget(widget.Id, widget.Row, widget.Position + 1);
                LoadWidgets();
            }
        }
    }
}
