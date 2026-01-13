using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SvonyBrowser.Services;

namespace SvonyBrowser.Controls
{
    /// <summary>
    /// Individual Status Bar widget control that renders based on widget type
    /// </summary>
    public partial class StatusBarWidgetControl : UserControl
    {
        private StatusBarWidget? _widget;
        private TextBlock? _labelText;
        private TextBlock? _valueText;
        private ProgressBar? _progressBar;
        private Ellipse? _trafficLight;
        private Canvas? _sparklineCanvas;

        public static readonly DependencyProperty WidgetProperty =
            DependencyProperty.Register(nameof(Widget), typeof(StatusBarWidget), 
                typeof(StatusBarWidgetControl), new PropertyMetadata(null, OnWidgetChanged));

        public StatusBarWidget? Widget
        {
            get => (StatusBarWidget?)GetValue(WidgetProperty);
            set => SetValue(WidgetProperty, value);
        }

        public StatusBarWidgetControl()
        {
            InitializeComponent();
        }

        private static void OnWidgetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBarWidgetControl control)
            {
                control.SetupWidget(e.NewValue as StatusBarWidget);
            }
        }

        private void SetupWidget(StatusBarWidget? widget)
        {
            if (_widget != null)
            {
                _widget.PropertyChanged -= OnWidgetPropertyChanged;
            }

            _widget = widget;

            if (_widget == null)
            {
                ContentGrid.Children.Clear();
                return;
            }

            _widget.PropertyChanged += OnWidgetPropertyChanged;
            BuildWidgetUI();
            UpdateWidgetDisplay();
        }

        private void BuildWidgetUI()
        {
            if (_widget == null) return;

            ContentGrid.Children.Clear();
            ContentGrid.ColumnDefinitions.Clear();

            switch (_widget.Type)
            {
                case WidgetType.ProgressBar:
                    BuildProgressBarWidget();
                    break;
                case WidgetType.Label:
                    BuildLabelWidget();
                    break;
                case WidgetType.Sparkline:
                    BuildSparklineWidget();
                    break;
                case WidgetType.TrafficLight:
                    BuildTrafficLightWidget();
                    break;
                case WidgetType.Counter:
                    BuildCounterWidget();
                    break;
                case WidgetType.Timer:
                    BuildTimerWidget();
                    break;
                case WidgetType.Temperature:
                    BuildTemperatureWidget();
                    break;
                default:
                    BuildLabelWidget();
                    break;
            }

            // Set tooltip
            TooltipTitle.Text = _widget.Title;
            TooltipDetails.Text = $"Category: {_widget.Category}\nRefresh: {_widget.RefreshRateMs}ms";
        }

        #region Widget Builders

        private void BuildProgressBarWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title,
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Progress bar
            _progressBar = new ProgressBar
            {
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 61)),
                Foreground = GetColorForScheme(_widget.ColorScheme),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _progressBar.Template = CreateProgressBarTemplate();
            Grid.SetColumn(_progressBar, 1);
            ContentGrid.Children.Add(_progressBar);

            // Value
            _valueText = new TextBlock
            {
                Foreground = (Brush)FindResource("TextPrimary"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            Grid.SetColumn(_valueText, 2);
            ContentGrid.Children.Add(_valueText);
        }

        private void BuildLabelWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title + ":",
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Value
            _valueText = new TextBlock
            {
                Foreground = (Brush)FindResource("TextPrimary"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_valueText, 1);
            ContentGrid.Children.Add(_valueText);
        }

        private void BuildSparklineWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title,
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Sparkline canvas
            _sparklineCanvas = new Canvas
            {
                Height = 16,
                Width = 50,
                Background = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_sparklineCanvas, 1);
            ContentGrid.Children.Add(_sparklineCanvas);

            // Value
            _valueText = new TextBlock
            {
                Foreground = (Brush)FindResource("TextPrimary"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(_valueText, 2);
            ContentGrid.Children.Add(_valueText);
        }

        private void BuildTrafficLightWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title,
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Traffic light
            _trafficLight = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = (Brush)FindResource("TrafficGray"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_trafficLight, 1);
            ContentGrid.Children.Add(_trafficLight);

            // Value
            _valueText = new TextBlock
            {
                Foreground = (Brush)FindResource("TextPrimary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(_valueText, 2);
            ContentGrid.Children.Add(_valueText);
        }

        private void BuildCounterWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title + ":",
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Value with special styling for counters
            _valueText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_valueText, 1);
            ContentGrid.Children.Add(_valueText);
        }

        private void BuildTimerWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title + ":",
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Timer value with monospace font
            _valueText = new TextBlock
            {
                Foreground = (Brush)FindResource("TextPrimary"),
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_valueText, 1);
            ContentGrid.Children.Add(_valueText);
        }

        private void BuildTemperatureWidget()
        {
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            _labelText = new TextBlock
            {
                Text = _widget!.Title + ":",
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(_labelText, 0);
            ContentGrid.Children.Add(_labelText);

            // Temperature value with color coding
            _valueText = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_valueText, 1);
            ContentGrid.Children.Add(_valueText);
        }

        #endregion

        #region Update Methods

        private void OnWidgetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(UpdateWidgetDisplay);
        }

        private void UpdateWidgetDisplay()
        {
            if (_widget == null) return;

            // Update value text
            if (_valueText != null)
            {
                _valueText.Text = _widget.DisplayText;
            }

            // Update tooltip
            TooltipValue.Text = _widget.DisplayText;

            // Type-specific updates
            switch (_widget.Type)
            {
                case WidgetType.ProgressBar:
                    UpdateProgressBar();
                    break;
                case WidgetType.TrafficLight:
                    UpdateTrafficLight();
                    break;
                case WidgetType.Sparkline:
                    UpdateSparkline();
                    break;
                case WidgetType.Temperature:
                    UpdateTemperature();
                    break;
            }

            // Update status-based styling
            UpdateStatusStyling();
        }

        private void UpdateProgressBar()
        {
            if (_progressBar != null && _widget != null)
            {
                _progressBar.Value = _widget.Progress * 100;
            }
        }

        private void UpdateTrafficLight()
        {
            if (_trafficLight != null && _widget != null)
            {
                _trafficLight.Fill = _widget.Status switch
                {
                    WidgetStatus.Normal => (Brush)FindResource("TrafficGreen"),
                    WidgetStatus.Warning => (Brush)FindResource("TrafficYellow"),
                    WidgetStatus.Error => (Brush)FindResource("TrafficRed"),
                    _ => (Brush)FindResource("TrafficGray")
                };
            }
        }

        private void UpdateSparkline()
        {
            if (_sparklineCanvas == null || _widget?.SparklineData == null || _widget.SparklineData.Count < 2)
                return;

            _sparklineCanvas.Children.Clear();

            var data = _widget.SparklineData;
            var width = _sparklineCanvas.Width;
            var height = _sparklineCanvas.Height;
            var max = data.Max();
            var min = data.Min();
            var range = max - min;
            if (range == 0) range = 1;

            var polyline = new Polyline
            {
                Stroke = GetColorForScheme(_widget.ColorScheme),
                StrokeThickness = 1.5,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (int i = 0; i < data.Count; i++)
            {
                var x = (i / (double)(data.Count - 1)) * width;
                var y = height - ((data[i] - min) / range) * height;
                polyline.Points.Add(new Point(x, y));
            }

            _sparklineCanvas.Children.Add(polyline);
        }

        private void UpdateTemperature()
        {
            if (_valueText != null && _widget?.CurrentValue is int temp)
            {
                _valueText.Foreground = temp switch
                {
                    < 50 => new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // Green
                    < 70 => new SolidColorBrush(Color.FromRgb(234, 179, 8)),   // Yellow
                    < 85 => new SolidColorBrush(Color.FromRgb(249, 115, 22)),  // Orange
                    _ => new SolidColorBrush(Color.FromRgb(239, 68, 68))       // Red
                };
            }
        }

        private void UpdateStatusStyling()
        {
            if (_widget == null) return;

            // Update border based on status
            WidgetBorder.BorderBrush = _widget.Status switch
            {
                WidgetStatus.Warning => new SolidColorBrush(Color.FromRgb(234, 179, 8)),
                WidgetStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => (Brush)FindResource("WidgetBorder")
            };
        }

        #endregion

        #region Helpers

        private Brush GetColorForScheme(string scheme)
        {
            return scheme.ToLower() switch
            {
                "purple" => (Brush)FindResource("ProgressPurple"),
                "blue" => (Brush)FindResource("ProgressBlue"),
                "green" => (Brush)FindResource("ProgressGreen"),
                "yellow" => (Brush)FindResource("ProgressYellow"),
                "red" => (Brush)FindResource("ProgressRed"),
                "cyan" => (Brush)FindResource("ProgressCyan"),
                "orange" => (Brush)FindResource("ProgressOrange"),
                "gray" => (Brush)FindResource("ProgressGray"),
                _ => (Brush)FindResource("ProgressBlue")
            };
        }

        private ControlTemplate CreateProgressBarTemplate()
        {
            var template = new ControlTemplate(typeof(ProgressBar));
            
            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            
            var trackFactory = new FrameworkElementFactory(typeof(Border));
            trackFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            trackFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            trackFactory.Name = "PART_Track";
            
            var indicatorFactory = new FrameworkElementFactory(typeof(Border));
            indicatorFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ForegroundProperty));
            indicatorFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            indicatorFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
            indicatorFactory.Name = "PART_Indicator";
            
            gridFactory.AppendChild(trackFactory);
            gridFactory.AppendChild(indicatorFactory);
            
            template.VisualTree = gridFactory;
            
            return template;
        }

        #endregion
    }
}
