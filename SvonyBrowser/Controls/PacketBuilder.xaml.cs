using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SvonyBrowser.Services;

namespace SvonyBrowser.Controls
{

    /// <summary>
    /// Visual drag-and-drop interface for building and injecting custom packets.
    /// </summary>
    public partial class PacketBuilder : UserControl
    {
        #region Fields

        private readonly ObservableCollection<ParameterItem> _parameters = new ObservableCollection<ParameterItem>();
        private readonly Dictionary<string, ActionDefinition> _actionDefinitions = new ActionDefinition>();
        private string _currentAction;
        private byte[]? _previewBytes;

        #endregion

        #region Constructor

        public PacketBuilder()
        {
            InitializeComponent();
        
            ParametersItemsControl.ItemsSource = _parameters;
        
            LoadActionDefinitions();
            PopulateActionComboBox();
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when a packet is injected.
        /// </summary>
        public event Action<string, JObject> PacketInjected;

        /// <summary>
        /// Fired when a response is captured.
        /// </summary>
        public event Action<string, JObject> ResponseCaptured;

        #endregion

        #region Event Handlers

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is ComboBoxItem item)
            {
                var category = item.Content?.ToString();
                PopulateActionComboBox(category == "All" ? null : category);
            }
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionComboBox.SelectedItem is string action)
            {
                LoadActionParameters(action);
            }
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidateParameters();
        
            if (errors.Count == 0)
            {
                MessageBox.Show("All parameters are valid!", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Validation errors:\n{string.Join("\n", errors)}", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void InjectButton_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidateParameters();
            if (errors.Count > 0)
            {
                MessageBox.Show($"Cannot inject - validation errors:\n{string.Join("\n", errors)}", 
                    "Injection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var action = _currentAction ?? ActionComboBox.Text;
                var parameters = BuildParameterObject();

                // Inject the packet
                InjectPacket(action, parameters);

                PacketInjected?.Invoke(action, parameters);

                MessageBox.Show($"Packet injected successfully!\nAction: {action}", 
                    "Injection Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Injection failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Packet Template (*.pkt)|*.pkt|JSON (*.json)|*.json",
                DefaultExt = ".pkt",
                FileName = _currentAction ?? "packet_template"
            };

            if (dialog.ShowDialog() == true)
            {
                var template = new PacketTemplate
                {
                    Action = _currentAction ?? ActionComboBox.Text,
                    Parameters = _parameters.ToDictionary(p => p.Name, p => p.Value),
                    CaptureResponse = AutoResponseCheckBox.IsChecked == true,
                    MockResponse = MockResponseCheckBox.IsChecked == true
                };

                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(template, Formatting.Indented));
                MessageBox.Show("Template saved successfully!", "Save", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Packet Template (*.pkt)|*.pkt|JSON (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var template = JsonConvert.DeserializeObject<PacketTemplate>(json);

                    if (template != null)
                    {
                        ActionComboBox.Text = template.Action;
                        LoadActionParameters(template.Action);

                        foreach (var param in template.Parameters)
                        {
                            var item = _parameters.FirstOrDefault(p => p.Name == param.Key);
                            if (item != null)
                            {
                                item.Value = param.Value;
                            }
                        }

                        AutoResponseCheckBox.IsChecked = template.CaptureResponse;
                        MockResponseCheckBox.IsChecked = template.MockResponse;

                        UpdatePreview();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load template: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewModeChanged(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        #endregion

        #region Private Methods

        private void LoadActionDefinitions()
        {
            // Load action definitions from protocol database
            var protocolDbPath = Path.Combine(App.DataPath, "protocol-db.json");
        
            if (File.Exists(protocolDbPath))
            {
                try
                {
                    var json = File.ReadAllText(protocolDbPath);
                    var db = JsonConvert.DeserializeObject<Dictionary<string, ActionDefinition>>(json);
                    if (db != null)
                    {
                        foreach (var kvp in db)
                        {
                            _actionDefinitions[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex, "Failed to load protocol database");
                }
            }

            // Add common actions if not loaded
            if (_actionDefinitions.Count == 0)
            {
                AddDefaultActionDefinitions();
            }
        }

        private void AddDefaultActionDefinitions()
        {
            _actionDefinitions["castle.getCastleInfo"] = new ActionDefinition
            {
                Action = "castle.getCastleInfo",
                Category = "castle",
                Description = "Get full castle information",
                Parameters = new List<ParameterDefinition>
                {
                    new() { Name = "castleId", DataType = "int", Required = true, Description = "Castle ID" }
                }
            };

            _actionDefinitions["castle.upgradeBuilding"] = new ActionDefinition
            {
                Action = "castle.upgradeBuilding",
                Category = "castle",
                Description = "Upgrade a building",
                Parameters = new List<ParameterDefinition>
                {
                    new() { Name = "castleId", DataType = "int", Required = true, Description = "Castle ID" },
                    new() { Name = "positionId", DataType = "int", Required = true, Description = "Building position" }
                }
            };

            _actionDefinitions["hero.hireHero"] = new ActionDefinition
            {
                Action = "hero.hireHero",
                Category = "hero",
                Description = "Hire a new hero",
                Parameters = new List<ParameterDefinition>
                {
                    new() { Name = "castleId", DataType = "int", Required = true, Description = "Castle ID" },
                    new() { Name = "heroName", DataType = "string", Required = true, Description = "Hero name" },
                    new() { Name = "useGems", DataType = "bool", Required = false, Description = "Use gems for hiring" }
                }
            };

            _actionDefinitions["troop.trainTroop"] = new ActionDefinition
            {
                Action = "troop.trainTroop",
                Category = "troop",
                Description = "Train troops",
                Parameters = new List<ParameterDefinition>
                {
                    new() { Name = "castleId", DataType = "int", Required = true, Description = "Castle ID" },
                    new() { Name = "troopType", DataType = "string", Required = true, Description = "Troop type code" },
                    new() { Name = "amount", DataType = "int", Required = true, Description = "Number to train" }
                }
            };

            _actionDefinitions["army.sendArmy"] = new ActionDefinition
            {
                Action = "army.sendArmy",
                Category = "army",
                Description = "Send army on a mission",
                Parameters = new List<ParameterDefinition>
                {
                    new() { Name = "armyId", DataType = "int", Required = true, Description = "Army ID" },
                    new() { Name = "targetX", DataType = "int", Required = true, Description = "Target X coordinate" },
                    new() { Name = "targetY", DataType = "int", Required = true, Description = "Target Y coordinate" },
                    new() { Name = "missionType", DataType = "string", Required = true, Description = "Mission type (attack/scout/reinforce)" }
                }
            };

            // Add more default actions...
        }

        private void PopulateActionComboBox(string category = null)
        {
            ActionComboBox.Items.Clear();

            var actions = _actionDefinitions.Values
                .Where(a => category == null || a.Category == category)
                .OrderBy(a => a.Action)
                .Select(a => a.Action);

            foreach (var action in actions)
            {
                ActionComboBox.Items.Add(action);
            }
        }

        private void LoadActionParameters(string action)
        {
            _currentAction = action;
            _parameters.Clear();

            if (_actionDefinitions.TryGetValue(action, out var definition))
            {
                foreach (var param in definition.Parameters)
                {
                    _parameters.Add(new ParameterItem
                    {
                        Name = param.Name,
                        DataType = param.DataType,
                        Required = param.Required ? "Required" : "Optional",
                        RequiredColor = param.Required ? Brushes.Orange : Brushes.Gray,
                        Value = param.DefaultValue ?? "",
                        Description = param.Description
                    });
                }
            }

            UpdatePreview();
        }

        private List<string> ValidateParameters()
        {
            var errors = new List<string>();

            foreach (var param in _parameters)
            {
                if (param.Required == "Required" && string.IsNullOrWhiteSpace(param.Value))
                {
                    errors.Add($"Required parameter '{param.Name}' is missing");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(param.Value))
                {
                    switch (param.DataType)
                    {
                        case "int":
                            if (!int.TryParse(param.Value, out _))
                                errors.Add($"Parameter '{param.Name}' must be an integer");
                            break;
                        case "long":
                            if (!long.TryParse(param.Value, out _))
                                errors.Add($"Parameter '{param.Name}' must be a long integer");
                            break;
                        case "bool":
                            if (!bool.TryParse(param.Value, out _) && param.Value != "0" && param.Value != "1")
                                errors.Add($"Parameter '{param.Name}' must be a boolean");
                            break;
                        case "double":
                            if (!double.TryParse(param.Value, out _))
                                errors.Add($"Parameter '{param.Name}' must be a number");
                            break;
                    }
                }
            }

            return errors;
        }

        private JObject BuildParameterObject()
        {
            var obj = new JObject();

            foreach (var param in _parameters)
            {
                if (string.IsNullOrWhiteSpace(param.Value))
                    continue;

                JToken value = param.DataType switch
                {
                    "int" => int.Parse(param.Value),
                    "long" => long.Parse(param.Value),
                    "bool" => bool.Parse(param.Value) || param.Value == "1",
                    "double" => double.Parse(param.Value),
                    "array" => JArray.Parse(param.Value),
                    "object" => JObject.Parse(param.Value),
                    _ => param.Value
                };

                obj[param.Name] = value;
            }

            return obj;
        }

        private void UpdatePreview()
        {
            try
            {
                var action = _currentAction ?? ActionComboBox.Text;
                var parameters = BuildParameterObject();

                if (HexViewRadio.IsChecked == true)
                {
                    // Generate AMF3 hex preview
                    _previewBytes = EncodeToAmf3(action, parameters);
                    PreviewTextBlock.Text = FormatHex(_previewBytes);
                }
                else
                {
                    // JSON preview
                    var json = new JObject
                    {
                        ["action"] = action,
                        ["parameters"] = parameters
                    };
                    PreviewTextBlock.Text = json.ToString(Formatting.Indented);
                }
            }
            catch (Exception ex)
            {
                PreviewTextBlock.Text = $"Error generating preview: {ex.Message}";
            }
        }

        private byte[] EncodeToAmf3(string action, JObject parameters)
        {
            // Simplified AMF3 encoding - in production, use proper AMF3 library
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            // AMF3 header
            writer.Write((byte)0x00); // Version
            writer.Write((byte)0x00); // Header count
            writer.Write((byte)0x00); // Reserved
            writer.Write((byte)0x01); // Message count

            // Message
            writer.Write((ushort)0); // Target URI length
            writer.Write((ushort)0); // Response URI length

            // Action string
            var actionBytes = Encoding.UTF8.GetBytes(action);
            writer.Write((byte)0x06); // AMF3 string marker
            writer.Write((byte)((actionBytes.Length << 1) | 1));
            writer.Write(actionBytes);

            // Parameters (simplified)
            var paramJson = parameters.ToString();
            var paramBytes = Encoding.UTF8.GetBytes(paramJson);
            writer.Write((byte)0x06); // AMF3 string marker
            writer.Write(paramBytes.Length);
            writer.Write(paramBytes);

            return ms.ToArray();
        }

        private string FormatHex(byte[] bytes)
        {
            var sb = new StringBuilder();
        
            for (int i = 0; i < bytes.Length; i += 16)
            {
                // Offset
                sb.Append($"{i:X8}  ");

                // Hex bytes
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytes.Length)
                        sb.Append($"{bytes[i + j]:X2} ");
                    else
                        sb.Append("   ");

                    if (j == 7) sb.Append(" ");
                }

                sb.Append(" ");

                // ASCII
                for (int j = 0; j < 16 && i + j < bytes.Length; j++)
                {
                    var b = bytes[i + j];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void InjectPacket(string action, JObject parameters)
        {
            // Send packet through the protocol handler
            ProtocolHandler.Instance.SendPacket(action, parameters);
        }

        #endregion
    }

    #region Models

    public class ParameterItem : INotifyPropertyChanged
    {
        private string _value = "";

        public string Name { get; set; } = "";
        public string DataType { get; set; } = "string";
        public string Required { get; set; } = "Optional";
        public Brush RequiredColor { get; set; } = Brushes.Gray;
        public string Description { get; set; } = "";

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ActionDefinition
    {
        public string Action { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ParameterDefinition> Parameters { get; set; } = new List<ParameterDefinition>();
    }

    public class ParameterDefinition
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "string";
        public bool Required { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; } = "";
    }

    public class PacketTemplate
    {
        public string Action { get; set; } = "";
        public Dictionary<string, string> Parameters { get; set; } = new string>();
        public bool CaptureResponse { get; set; }
        public bool MockResponse { get; set; }
    }

    #endregion

}