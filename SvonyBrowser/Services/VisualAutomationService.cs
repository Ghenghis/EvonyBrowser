using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Visual automation service for Flash-based Evony game
    /// Handles coordinate-based clicking, template matching, and OCR
    /// </summary>
    public class VisualAutomationService
    {
        #region Singleton

        private static readonly Lazy<VisualAutomationService> _lazyInstance =
            new Lazy<VisualAutomationService>(() => new VisualAutomationService(CdpConnectionService.Instance), LazyThreadSafetyMode.ExecutionAndPublication);

        public static VisualAutomationService Instance => _lazyInstance.Value;

        #endregion

        private readonly CdpConnectionService _cdp;
        private Size _viewportSize;
        
        // UI Element Map
        private readonly Dictionary<string, EvonyUIElement> _uiElements;
        
        // Configuration
        public int ClickDelay { get; set; } = 100;
        public int TypeDelay { get; set; } = 50;
        public double TemplateMatchThreshold { get; set; } = 0.8;
        
        public VisualAutomationService(CdpConnectionService cdpConnection)
        {
            _cdp = cdpConnection;
            _uiElements = InitializeUIMap();
        }
        
        #region Initialization
        
        /// <summary>
        /// Initialize the UI element map with known coordinates
        /// </summary>
        private Dictionary<string, EvonyUIElement> InitializeUIMap()
        {
            return new Dictionary<string, EvonyUIElement>
            {
                // Top Navigation Bar
                ["cityView"] = new EvonyUIElement("City View", 100, 50, "Navigate to city view"),
                ["worldMap"] = new EvonyUIElement("World Map", 200, 50, "Navigate to world map"),
                ["alliance"] = new EvonyUIElement("Alliance", 300, 50, "Open alliance panel"),
                ["mail"] = new EvonyUIElement("Mail", 400, 50, "Open mail"),
                ["events"] = new EvonyUIElement("Events", 500, 50, "Open events"),
                ["shop"] = new EvonyUIElement("Shop", 600, 50, "Open shop"),
                
                // Bottom Menu Bar
                ["buildMenu"] = new EvonyUIElement("Build Menu", 150, 600, "Open build menu"),
                ["research"] = new EvonyUIElement("Research", 250, 600, "Open research"),
                ["heroes"] = new EvonyUIElement("Heroes", 350, 600, "Open heroes"),
                ["march"] = new EvonyUIElement("March", 450, 600, "Open march panel"),
                ["items"] = new EvonyUIElement("Items", 550, 600, "Open items"),
                ["more"] = new EvonyUIElement("More", 650, 600, "Open more options"),
                
                // Resource Bar (top)
                ["food"] = new EvonyUIElement("Food", 200, 20, "Food resource"),
                ["wood"] = new EvonyUIElement("Wood", 300, 20, "Wood resource"),
                ["stone"] = new EvonyUIElement("Stone", 400, 20, "Stone resource"),
                ["iron"] = new EvonyUIElement("Iron", 500, 20, "Iron resource"),
                ["gold"] = new EvonyUIElement("Gold", 600, 20, "Gold resource"),
                
                // City Buildings (approximate center positions)
                ["castle"] = new EvonyUIElement("Castle", 960, 400, "Main castle"),
                ["barracks"] = new EvonyUIElement("Barracks", 800, 500, "Train infantry"),
                ["stables"] = new EvonyUIElement("Stables", 1100, 500, "Train cavalry"),
                ["workshop"] = new EvonyUIElement("Workshop", 700, 400, "Train siege"),
                ["academy"] = new EvonyUIElement("Academy", 1200, 400, "Research"),
                ["forge"] = new EvonyUIElement("Forge", 600, 350, "Craft equipment"),
                ["embassy"] = new EvonyUIElement("Embassy", 1300, 350, "Alliance features"),
                ["warehouse"] = new EvonyUIElement("Warehouse", 500, 450, "Resource protection"),
                ["wall"] = new EvonyUIElement("Wall", 960, 200, "City defense"),
                ["watchtower"] = new EvonyUIElement("Watchtower", 1100, 200, "Scout reports"),
                
                // Resource Buildings
                ["farm"] = new EvonyUIElement("Farm", 400, 550, "Food production"),
                ["sawmill"] = new EvonyUIElement("Sawmill", 500, 550, "Wood production"),
                ["quarry"] = new EvonyUIElement("Quarry", 600, 550, "Stone production"),
                ["mine"] = new EvonyUIElement("Mine", 700, 550, "Iron production"),
                
                // Dialog Buttons
                ["confirmButton"] = new EvonyUIElement("Confirm", 960, 600, "Confirm action"),
                ["cancelButton"] = new EvonyUIElement("Cancel", 860, 600, "Cancel action"),
                ["closeButton"] = new EvonyUIElement("Close", 1100, 200, "Close dialog"),
                ["upgradeButton"] = new EvonyUIElement("Upgrade", 960, 550, "Upgrade building"),
                ["trainButton"] = new EvonyUIElement("Train", 960, 550, "Train troops"),
                ["instantButton"] = new EvonyUIElement("Instant", 1060, 550, "Instant complete"),
                
                // Troop Training
                ["trainInfantry"] = new EvonyUIElement("Train Infantry", 300, 400, "Select infantry"),
                ["trainCavalry"] = new EvonyUIElement("Train Cavalry", 500, 400, "Select cavalry"),
                ["trainArcher"] = new EvonyUIElement("Train Archer", 700, 400, "Select archer"),
                ["trainSiege"] = new EvonyUIElement("Train Siege", 900, 400, "Select siege"),
                
                // Quantity Buttons
                ["qty1"] = new EvonyUIElement("Qty 1", 400, 500, "Select 1"),
                ["qty10"] = new EvonyUIElement("Qty 10", 500, 500, "Select 10"),
                ["qty100"] = new EvonyUIElement("Qty 100", 600, 500, "Select 100"),
                ["qtyMax"] = new EvonyUIElement("Qty Max", 700, 500, "Select max"),
                
                // March Panel
                ["marchAttack"] = new EvonyUIElement("Attack", 400, 300, "Attack march"),
                ["marchScout"] = new EvonyUIElement("Scout", 500, 300, "Scout march"),
                ["marchGather"] = new EvonyUIElement("Gather", 600, 300, "Gather march"),
                ["marchReinforce"] = new EvonyUIElement("Reinforce", 700, 300, "Reinforce march"),
                ["marchTransport"] = new EvonyUIElement("Transport", 800, 300, "Transport march"),
                
                // World Map
                ["mapZoomIn"] = new EvonyUIElement("Zoom In", 1800, 300, "Zoom in map"),
                ["mapZoomOut"] = new EvonyUIElement("Zoom Out", 1800, 400, "Zoom out map"),
                ["mapSearch"] = new EvonyUIElement("Search", 1800, 500, "Search map"),
                ["mapMyCity"] = new EvonyUIElement("My City", 1800, 600, "Go to my city")
            };
        }
        
        /// <summary>
        /// Initialize viewport size
        /// </summary>
        public async Task InitializeAsync()
        {
            var (width, height) = await _cdp.GetViewportSizeAsync();
            _viewportSize = new Size(width, height);
        }
        
        #endregion
        
        #region Basic Input
        
        /// <summary>
        /// Click at absolute coordinates
        /// </summary>
        public async Task ClickAtAsync(int x, int y, string button = "left", int clickCount = 1)
        {
            await _cdp.ClickAsync(x, y, button, clickCount);
            await Task.Delay(ClickDelay);
        }
        
        /// <summary>
        /// Click at relative coordinates (percentage of viewport)
        /// </summary>
        public async Task ClickRelativeAsync(double xPercent, double yPercent, string button = "left")
        {
            var x = (int)(_viewportSize.Width * xPercent);
            var y = (int)(_viewportSize.Height * yPercent);
            
            await ClickAtAsync(x, y, button);
        }
        
        /// <summary>
        /// Click a named UI element
        /// </summary>
        public async Task ClickElementAsync(string elementName)
        {
            if (!_uiElements.TryGetValue(elementName, out var element))
            {
                throw new ArgumentException($"Unknown UI element: {elementName}");
            }
            
            // Scale coordinates based on viewport
            var x = ScaleX(element.X);
            var y = ScaleY(element.Y);
            
            await ClickAtAsync(x, y);
        }
        
        /// <summary>
        /// Double click at coordinates
        /// </summary>
        public async Task DoubleClickAtAsync(int x, int y)
        {
            await _cdp.DoubleClickAsync(x, y);
            await Task.Delay(ClickDelay);
        }
        
        /// <summary>
        /// Right click at coordinates
        /// </summary>
        public async Task RightClickAtAsync(int x, int y)
        {
            await _cdp.RightClickAsync(x, y);
            await Task.Delay(ClickDelay);
        }
        
        /// <summary>
        /// Drag from one point to another
        /// </summary>
        public async Task DragAsync(int fromX, int fromY, int toX, int toY, int steps = 10)
        {
            await _cdp.DragAsync(fromX, fromY, toX, toY, steps);
            await Task.Delay(ClickDelay);
        }
        
        /// <summary>
        /// Scroll at coordinates
        /// </summary>
        public async Task ScrollAsync(int x, int y, int deltaX = 0, int deltaY = 0)
        {
            await _cdp.ScrollAsync(x, y, deltaX, deltaY);
            await Task.Delay(ClickDelay);
        }
        
        /// <summary>
        /// Type text
        /// </summary>
        public async Task TypeTextAsync(string text)
        {
            await _cdp.TypeTextAsync(text, TypeDelay);
        }
        
        /// <summary>
        /// Press a key
        /// </summary>
        public async Task PressKeyAsync(string key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await _cdp.PressKeyAsync(key, ctrl, alt, shift);
            await Task.Delay(50);
        }
        
        #endregion
        
        #region Screenshot & Analysis
        
        /// <summary>
        /// Take screenshot
        /// </summary>
        public async Task<byte[]> TakeScreenshotAsync(string format = "png")
        {
            return await _cdp.TakeScreenshotAsync(format);
        }
        
        /// <summary>
        /// Take screenshot of a region
        /// </summary>
        public async Task<byte[]> TakeRegionScreenshotAsync(int x, int y, int width, int height)
        {
            return await _cdp.TakeScreenshotAsync("png", 100, x, y, width, height);
        }
        
        /// <summary>
        /// Save screenshot to file
        /// </summary>
        public async Task SaveScreenshotAsync(string filePath)
        {
            var data = await TakeScreenshotAsync();
            await File.WriteAllBytesAsync(filePath, data);
        }
        
        /// <summary>
        /// Find element by template image (basic implementation)
        /// </summary>
        public async Task<Point?> FindTemplateAsync(byte[] templateImage, double threshold = 0.8)
        {
            // Take screenshot
            var screenshot = await TakeScreenshotAsync();
            
            // This is a placeholder for actual template matching
            // In production, use OpenCV or similar library
            
            // For now, return null (not found)
            // Actual implementation would use image processing
            return null;
        }
        
        /// <summary>
        /// Wait for element to appear
        /// </summary>
        public async Task<bool> WaitForElementAsync(string elementName, int timeoutMs = 10000)
        {
            var startTime = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                // Take screenshot and check for element
                // This is a placeholder - actual implementation would use template matching
                
                await Task.Delay(500);
            }
            
            return false;
        }
        
        #endregion
        
        #region Game Actions
        
        /// <summary>
        /// Navigate to city view
        /// </summary>
        public async Task GoToCityAsync()
        {
            await ClickElementAsync("cityView");
            await Task.Delay(1000);
        }
        
        /// <summary>
        /// Navigate to world map
        /// </summary>
        public async Task GoToWorldMapAsync()
        {
            await ClickElementAsync("worldMap");
            await Task.Delay(1000);
        }
        
        /// <summary>
        /// Open alliance panel
        /// </summary>
        public async Task OpenAllianceAsync()
        {
            await ClickElementAsync("alliance");
            await Task.Delay(500);
        }
        
        /// <summary>
        /// Open build menu
        /// </summary>
        public async Task OpenBuildMenuAsync()
        {
            await ClickElementAsync("buildMenu");
            await Task.Delay(500);
        }
        
        /// <summary>
        /// Open research panel
        /// </summary>
        public async Task OpenResearchAsync()
        {
            await ClickElementAsync("research");
            await Task.Delay(500);
        }
        
        /// <summary>
        /// Open heroes panel
        /// </summary>
        public async Task OpenHeroesAsync()
        {
            await ClickElementAsync("heroes");
            await Task.Delay(500);
        }
        
        /// <summary>
        /// Click on a building
        /// </summary>
        public async Task ClickBuildingAsync(string buildingName)
        {
            await ClickElementAsync(buildingName);
            await Task.Delay(500);
        }
        
        /// <summary>
        /// Upgrade a building
        /// </summary>
        public async Task UpgradeBuildingAsync(string buildingName)
        {
            await GoToCityAsync();
            await ClickBuildingAsync(buildingName);
            await ClickElementAsync("upgradeButton");
            await ClickElementAsync("confirmButton");
        }
        
        /// <summary>
        /// Train troops
        /// </summary>
        public async Task TrainTroopsAsync(string troopType, int quantity)
        {
            await GoToCityAsync();
            
            // Click appropriate building
            switch (troopType.ToLower())
            {
                case "infantry":
                    await ClickBuildingAsync("barracks");
                    break;
                case "cavalry":
                    await ClickBuildingAsync("stables");
                    break;
                case "archer":
                    await ClickBuildingAsync("barracks"); // Adjust as needed
                    break;
                case "siege":
                    await ClickBuildingAsync("workshop");
                    break;
            }
            
            await ClickElementAsync("trainButton");
            
            // Select quantity
            if (quantity >= 100)
            {
                await ClickElementAsync("qtyMax");
            }
            else if (quantity >= 10)
            {
                await ClickElementAsync("qty100");
            }
            else
            {
                await ClickElementAsync("qty10");
            }
            
            await TypeTextAsync(quantity.ToString());
            await ClickElementAsync("confirmButton");
        }
        
        /// <summary>
        /// Send a march
        /// </summary>
        public async Task SendMarchAsync(string marchType, int targetX, int targetY)
        {
            await GoToWorldMapAsync();
            
            // Click on target coordinates
            // This requires coordinate conversion
            var screenX = _viewportSize.Width / 2;
            var screenY = _viewportSize.Height / 2;
            
            await ClickAtAsync(screenX, screenY);
            await Task.Delay(500);
            
            // Select march type
            switch (marchType.ToLower())
            {
                case "attack":
                    await ClickElementAsync("marchAttack");
                    break;
                case "scout":
                    await ClickElementAsync("marchScout");
                    break;
                case "gather":
                    await ClickElementAsync("marchGather");
                    break;
                case "reinforce":
                    await ClickElementAsync("marchReinforce");
                    break;
            }
            
            await ClickElementAsync("confirmButton");
        }
        
        /// <summary>
        /// Pan the world map
        /// </summary>
        public async Task PanMapAsync(string direction, int distance = 200)
        {
            var centerX = _viewportSize.Width / 2;
            var centerY = _viewportSize.Height / 2;
            
            int toX = centerX, toY = centerY;
            
            switch (direction.ToLower())
            {
                case "up":
                    toY = centerY + distance;
                    break;
                case "down":
                    toY = centerY - distance;
                    break;
                case "left":
                    toX = centerX + distance;
                    break;
                case "right":
                    toX = centerX - distance;
                    break;
            }
            
            await DragAsync(centerX, centerY, toX, toY);
        }
        
        /// <summary>
        /// Close current dialog
        /// </summary>
        public async Task CloseDialogAsync()
        {
            await ClickElementAsync("closeButton");
            await Task.Delay(300);
        }
        
        /// <summary>
        /// Confirm action
        /// </summary>
        public async Task ConfirmAsync()
        {
            await ClickElementAsync("confirmButton");
            await Task.Delay(300);
        }
        
        /// <summary>
        /// Cancel action
        /// </summary>
        public async Task CancelAsync()
        {
            await ClickElementAsync("cancelButton");
            await Task.Delay(300);
        }
        
        #endregion
        
        #region Coordinate Scaling
        
        /// <summary>
        /// Scale X coordinate based on viewport
        /// </summary>
        private int ScaleX(int x)
        {
            // Base resolution is 1920x1080
            return (int)(x * (_viewportSize.Width / 1920.0));
        }
        
        /// <summary>
        /// Scale Y coordinate based on viewport
        /// </summary>
        private int ScaleY(int y)
        {
            // Base resolution is 1920x1080
            return (int)(y * (_viewportSize.Height / 1080.0));
        }
        
        #endregion
        
        #region UI Element Info
        
        /// <summary>
        /// Get all UI elements
        /// </summary>
        public IReadOnlyDictionary<string, EvonyUIElement> GetUIElements()
        {
            return _uiElements;
        }
        
        /// <summary>
        /// Get UI element by name
        /// </summary>
        public EvonyUIElement GetUIElement(string name)
        {
            return _uiElements.TryGetValue(name, out var element) ? element : null;
        }
        
        /// <summary>
        /// Add custom UI element
        /// </summary>
        public void AddUIElement(string name, int x, int y, string description = null)
        {
            _uiElements[name] = new EvonyUIElement(name, x, y, description);
        }
        
        /// <summary>
        /// Export UI map to JSON
        /// </summary>
        public string ExportUIMapToJson()
        {
            return JsonConvert.SerializeObject(_uiElements, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
        }
        
        /// <summary>
        /// Import UI map from JSON
        /// </summary>
        public void ImportUIMapFromJson(string json)
        {
            var elements = JsonConvert.DeserializeObject<Dictionary<string, EvonyUIElement>>(json);
            
            foreach (var kvp in elements)
            {
                _uiElements[kvp.Key] = kvp.Value;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a UI element in the Evony game
    /// </summary>
    public class EvonyUIElement
    {
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        
        public EvonyUIElement() { }
        
        public EvonyUIElement(string name, int x, int y, string description = null)
        {
            Name = name;
            X = x;
            Y = y;
            Description = description;
        }
    }
}
