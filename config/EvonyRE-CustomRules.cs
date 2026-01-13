using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using Fiddler;

// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║                    EVONY RE TOOLKIT - Custom Fiddler Rules                     ║
// ║                                                                                ║
// ║  Purpose: Filter and analyze Evony game traffic from cc2.evony.com            ║
// ║  Features:                                                                     ║
// ║    - Only show cc2.evony.com traffic (configurable)                           ║
// ║    - Packet type highlighting (login, march, battle, etc.)                    ║
// ║    - AMF3 binary packet detection                                             ║
// ║    - SWF file auto-extraction                                                 ║
// ║    - Stealth mode (remove proxy headers)                                      ║
// ║    - AutoEvony traffic detection                                              ║
// ║    - Real-time traffic logging                                                ║
// ║                                                                                ║
// ║  Installation: Copy to Documents\Fiddler2\Scripts\CustomRules.cs              ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝

namespace Fiddler
{
    public static class Handlers
    {
        // ═══════════════════════════════════════════════════════════════════════════
        // CONFIGURATION - Modify these settings as needed
        // ═══════════════════════════════════════════════════════════════════════════
        
        // Target Evony server (change this to switch servers)
        public static string TargetServer = "cc2.evony.com";
        
        // Base path for the toolkit
        public static string ToolkitPath = @"D:\Fiddler-FlashBrowser";
        
        // ═══════════════════════════════════════════════════════════════════════════
        // RULE OPTIONS - Toggle via Rules menu
        // ═══════════════════════════════════════════════════════════════════════════
        
        [RulesOption("&Evony Only Mode (cc2.evony.com)", "Evony RE")]
        [BindPref("evonyre.rules.EvonyOnlyMode")]
        public static bool m_EvonyOnlyMode = true;
        
        [RulesOption("&Stealth Mode (Remove Proxy Headers)", "Evony RE")]
        [BindPref("evonyre.rules.StealthMode")]
        public static bool m_StealthMode = true;
        
        [RulesOption("&Highlight Packet Types", "Evony RE")]
        [BindPref("evonyre.rules.HighlightPackets")]
        public static bool m_HighlightPackets = true;
        
        [RulesOption("&Auto-Extract SWF Files", "Evony RE")]
        [BindPref("evonyre.rules.AutoExtractSWF")]
        public static bool m_AutoExtractSWF = true;
        
        [RulesOption("&Log All Traffic", "Evony RE")]
        [BindPref("evonyre.rules.LogTraffic")]
        public static bool m_LogTraffic = true;
        
        [RulesOption("&Detect AutoEvony/Bots", "Evony RE")]
        [BindPref("evonyre.rules.DetectBots")]
        public static bool m_DetectBots = true;
        
        [RulesOption("&Decode AMF3 Packets", "Evony RE")]
        [BindPref("evonyre.rules.DecodeAMF")]
        public static bool m_DecodeAMF = true;
        
        [RulesOption("Hide &Non-Evony Traffic", "Evony RE")]
        [BindPref("evonyre.rules.HideNonEvony")]
        public static bool m_HideNonEvony = true;
        
        // ═══════════════════════════════════════════════════════════════════════════
        // CUSTOM COLUMNS
        // ═══════════════════════════════════════════════════════════════════════════
        
        [BindUIColumn("Evony Type", 80)]
        public static string GetEvonyPacketType(Session oS)
        {
            if (!IsEvonyTraffic(oS)) return "";
            
            string url = oS.fullUrl.ToLower();
            string body = oS.GetRequestBodyAsString().ToLower();
            
            // Detect packet types based on URL and body content
            if (url.Contains("login") || body.Contains("login")) return "LOGIN";
            if (url.Contains("march") || body.Contains("march") || body.Contains("troop")) return "MARCH";
            if (url.Contains("battle") || body.Contains("battle") || body.Contains("attack")) return "BATTLE";
            if (url.Contains("resource") || body.Contains("resource") || body.Contains("gold")) return "RESOURCE";
            if (url.Contains("chat") || body.Contains("chat") || body.Contains("message")) return "CHAT";
            if (url.Contains("build") || body.Contains("build") || body.Contains("upgrade")) return "BUILD";
            if (url.Contains("research") || body.Contains("research") || body.Contains("tech")) return "RESEARCH";
            if (url.Contains("alliance") || body.Contains("alliance") || body.Contains("guild")) return "ALLIANCE";
            if (url.EndsWith(".swf")) return "SWF";
            if (IsAMFContent(oS)) return "AMF";
            
            return "OTHER";
        }
        
        [BindUIColumn("Size", 60)]
        public static string GetPacketSize(Session oS)
        {
            long size = oS.requestBodyBytes?.Length ?? 0;
            size += oS.responseBodyBytes?.Length ?? 0;
            
            if (size < 1024) return $"{size}B";
            if (size < 1048576) return $"{size/1024}KB";
            return $"{size/1048576}MB";
        }
        
        // ═══════════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════════════
        
        private static bool IsEvonyTraffic(Session oS)
        {
            string host = oS.hostname.ToLower();
            return host.Contains("evony.com") || host.Contains(TargetServer.ToLower());
        }
        
        private static bool IsTargetServerTraffic(Session oS)
        {
            string host = oS.hostname.ToLower();
            return host == TargetServer.ToLower() || host.EndsWith("." + TargetServer.ToLower());
        }
        
        private static bool IsAMFContent(Session oS)
        {
            string contentType = oS.oRequest["Content-Type"] ?? "";
            if (contentType.Contains("amf") || contentType.Contains("x-amf")) return true;
            
            // Check for AMF magic bytes (0x00 0x03 for AMF3)
            byte[] body = oS.requestBodyBytes;
            if (body != null && body.Length >= 2)
            {
                if (body[0] == 0x00 && body[1] == 0x03) return true;
            }
            
            return false;
        }
        
        private static bool IsSWFFile(Session oS)
        {
            return oS.fullUrl.ToLower().EndsWith(".swf") || 
                   (oS.oResponse != null && oS.oResponse["Content-Type"]?.Contains("shockwave-flash") == true);
        }
        
        private static bool IsAutoEvonyTraffic(Session oS)
        {
            string userAgent = oS.oRequest["User-Agent"] ?? "";
            string[] botPatterns = { "autoevony", "yaeb", "evony-bot", "selenium", "headless" };
            
            foreach (string pattern in botPatterns)
            {
                if (userAgent.ToLower().Contains(pattern)) return true;
            }
            
            // Check for unusual request patterns
            if (oS.oRequest["X-Requested-With"]?.Contains("bot") == true) return true;
            
            return false;
        }
        
        private static void LogTraffic(Session oS, string packetType)
        {
            if (!m_LogTraffic) return;
            
            try
            {
                string logDir = Path.Combine(ToolkitPath, "logs");
                string logFile = Path.Combine(logDir, $"traffic-{DateTime.Now:yyyy-MM-dd}.log");
                
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}|{packetType}|{oS.id}|{oS.RequestMethod}|{oS.fullUrl}|{oS.responseCode}|{oS.requestBodyBytes?.Length ?? 0}|{oS.responseBodyBytes?.Length ?? 0}\n";
                
                File.AppendAllText(logFile, logEntry);
            }
            catch { /* Ignore logging errors */ }
        }
        
        private static void ExtractSWF(Session oS)
        {
            if (!m_AutoExtractSWF || !IsSWFFile(oS)) return;
            if (oS.responseBodyBytes == null || oS.responseBodyBytes.Length == 0) return;
            
            try
            {
                string swfDir = Path.Combine(ToolkitPath, "extracted-swf");
                string sessionDir = Path.Combine(swfDir, DateTime.Now.ToString("yyyy-MM-dd"));
                
                if (!Directory.Exists(sessionDir)) Directory.CreateDirectory(sessionDir);
                
                // Get filename from URL
                Uri uri = new Uri(oS.fullUrl);
                string filename = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(filename)) filename = $"swf_{oS.id}.swf";
                
                // Sanitize filename
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    filename = filename.Replace(c, '_');
                }
                
                string filepath = Path.Combine(sessionDir, filename);
                
                // Add index if file exists
                int index = 1;
                string baseName = Path.GetFileNameWithoutExtension(filepath);
                string ext = Path.GetExtension(filepath);
                while (File.Exists(filepath))
                {
                    filepath = Path.Combine(sessionDir, $"{baseName}_{index}{ext}");
                    index++;
                }
                
                File.WriteAllBytes(filepath, oS.responseBodyBytes);
                
                // Log extraction
                string logFile = Path.Combine(swfDir, "extraction-log.txt");
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{oS.fullUrl}|{filepath}|{oS.responseBodyBytes.Length} bytes\n");
                
                oS["ui-comments"] = $"SWF extracted: {filename}";
            }
            catch (Exception ex)
            {
                oS["ui-comments"] = $"SWF extraction failed: {ex.Message}";
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════════════
        // MAIN REQUEST HANDLER
        // ═══════════════════════════════════════════════════════════════════════════
        
        public static void OnBeforeRequest(Session oS)
        {
            // Hide non-Evony traffic if enabled
            if (m_HideNonEvony && m_EvonyOnlyMode && !IsEvonyTraffic(oS))
            {
                oS["ui-hide"] = "true";
                return;
            }
            
            // Only process target server traffic in Evony Only Mode
            if (m_EvonyOnlyMode && !IsTargetServerTraffic(oS))
            {
                oS["ui-hide"] = "true";
                return;
            }
            
            // Stealth mode - remove proxy-revealing headers
            if (m_StealthMode)
            {
                oS.oRequest.headers.Remove("Via");
                oS.oRequest.headers.Remove("X-Forwarded-For");
                oS.oRequest.headers.Remove("X-Forwarded-Host");
                oS.oRequest.headers.Remove("X-Forwarded-Proto");
                oS.oRequest.headers.Remove("Proxy-Connection");
                oS.oRequest.headers.Remove("X-ProxyUser-IP");
                
                // Ensure normal connection header
                oS.oRequest["Connection"] = "keep-alive";
            }
            
            // Detect AutoEvony/bot traffic
            if (m_DetectBots && IsAutoEvonyTraffic(oS))
            {
                oS["ui-color"] = "#FF00FF"; // Magenta for bots
                oS["ui-bold"] = "true";
                oS["ui-comments"] = "⚠️ AutoEvony/Bot Traffic Detected";
            }
            
            // Packet type highlighting
            if (m_HighlightPackets && IsEvonyTraffic(oS))
            {
                string type = GetEvonyPacketType(oS);
                
                switch (type)
                {
                    case "LOGIN":
                        oS["ui-color"] = "#00FF00"; // Green
                        break;
                    case "MARCH":
                        oS["ui-color"] = "#FF6600"; // Orange
                        break;
                    case "BATTLE":
                        oS["ui-color"] = "#FF0000"; // Red
                        oS["ui-bold"] = "true";
                        break;
                    case "RESOURCE":
                        oS["ui-color"] = "#00FFFF"; // Cyan
                        break;
                    case "CHAT":
                        oS["ui-color"] = "#FFFF00"; // Yellow
                        break;
                    case "BUILD":
                        oS["ui-color"] = "#9966FF"; // Purple
                        break;
                    case "RESEARCH":
                        oS["ui-color"] = "#FF66FF"; // Pink
                        break;
                    case "ALLIANCE":
                        oS["ui-color"] = "#66FF66"; // Light green
                        break;
                    case "SWF":
                        oS["ui-color"] = "#0099FF"; // Blue
                        oS["ui-bold"] = "true";
                        break;
                    case "AMF":
                        oS["ui-color"] = "#FF9900"; // Amber
                        oS["ui-italic"] = "true";
                        break;
                }
                
                // Log the traffic
                LogTraffic(oS, type);
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════════════
        // MAIN RESPONSE HANDLER
        // ═══════════════════════════════════════════════════════════════════════════
        
        public static void OnBeforeResponse(Session oS)
        {
            if (!IsEvonyTraffic(oS)) return;
            
            // Extract SWF files automatically
            ExtractSWF(oS);
            
            // Highlight error responses
            if (oS.responseCode >= 400)
            {
                oS["ui-color"] = "#FF0000";
                oS["ui-bold"] = "true";
                oS["ui-strikeout"] = "true";
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════════════
        // MENU ACTIONS
        // ═══════════════════════════════════════════════════════════════════════════
        
        [ToolsAction("Export Session for RTE", "Evony RE")]
        public static void DoExportForRTE()
        {
            Session[] arrSessions = FiddlerApplication.UI.GetAllSessions();
            
            if (arrSessions.Length == 0)
            {
                MessageBox.Show("No sessions to export.", "Evony RE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            string exportDir = Path.Combine(ToolkitPath, "captures");
            string exportFile = Path.Combine(exportDir, $"evony-capture-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");
                
                bool first = true;
                foreach (Session oS in arrSessions)
                {
                    if (!IsEvonyTraffic(oS)) continue;
                    
                    if (!first) sb.AppendLine(",");
                    first = false;
                    
                    sb.AppendLine("  {");
                    sb.AppendLine($"    \"id\": {oS.id},");
                    sb.AppendLine($"    \"timestamp\": \"{oS.Timers.ClientBeginRequest:O}\",");
                    sb.AppendLine($"    \"method\": \"{oS.RequestMethod}\",");
                    sb.AppendLine($"    \"url\": \"{EscapeJson(oS.fullUrl)}\",");
                    sb.AppendLine($"    \"responseCode\": {oS.responseCode},");
                    sb.AppendLine($"    \"requestSize\": {oS.requestBodyBytes?.Length ?? 0},");
                    sb.AppendLine($"    \"responseSize\": {oS.responseBodyBytes?.Length ?? 0},");
                    sb.AppendLine($"    \"packetType\": \"{GetEvonyPacketType(oS)}\",");
                    sb.AppendLine($"    \"isAMF\": {IsAMFContent(oS).ToString().ToLower()},");
                    sb.AppendLine($"    \"isSWF\": {IsSWFFile(oS).ToString().ToLower()},");
                    sb.AppendLine($"    \"requestHeaders\": \"{EscapeJson(oS.oRequest.headers.ToString())}\",");
                    sb.AppendLine($"    \"requestBody\": \"{EscapeJson(Convert.ToBase64String(oS.requestBodyBytes ?? new byte[0]))}\",");
                    sb.AppendLine($"    \"responseBody\": \"{EscapeJson(Convert.ToBase64String(oS.responseBodyBytes ?? new byte[0]))}\"");
                    sb.Append("  }");
                }
                
                sb.AppendLine();
                sb.AppendLine("]");
                
                File.WriteAllText(exportFile, sb.ToString());
                
                MessageBox.Show($"Exported {arrSessions.Length} sessions to:\n{exportFile}", "Evony RE Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Evony RE Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
        
        [ToolsAction("Change Target Server...", "Evony RE")]
        public static void DoChangeServer()
        {
            string newServer = FiddlerApplication.UI.GetUserInput("Enter Evony Server", "Enter server domain (e.g., cc1, cc2, cc3):", TargetServer.Replace(".evony.com", ""));
            
            if (!string.IsNullOrEmpty(newServer))
            {
                if (!newServer.Contains(".evony.com"))
                {
                    newServer = newServer + ".evony.com";
                }
                TargetServer = newServer;
                MessageBox.Show($"Target server changed to: {TargetServer}\n\nNote: Clear sessions and refresh to see only new server traffic.", "Evony RE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        [ToolsAction("Open SWF Folder", "Evony RE")]
        public static void DoOpenSWFFolder()
        {
            string swfDir = Path.Combine(ToolkitPath, "extracted-swf");
            if (!Directory.Exists(swfDir)) Directory.CreateDirectory(swfDir);
            System.Diagnostics.Process.Start("explorer.exe", swfDir);
        }
        
        [ToolsAction("Open Captures Folder", "Evony RE")]
        public static void DoOpenCapturesFolder()
        {
            string capturesDir = Path.Combine(ToolkitPath, "captures");
            if (!Directory.Exists(capturesDir)) Directory.CreateDirectory(capturesDir);
            System.Diagnostics.Process.Start("explorer.exe", capturesDir);
        }
        
        [ToolsAction("Open Logs Folder", "Evony RE")]
        public static void DoOpenLogsFolder()
        {
            string logsDir = Path.Combine(ToolkitPath, "logs");
            if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
            System.Diagnostics.Process.Start("explorer.exe", logsDir);
        }
        
        [ContextAction("Copy as AMF Hex", "Evony RE")]
        public static void DoCopyAsAMFHex(Session[] arrSessions)
        {
            if (arrSessions.Length == 0) return;
            
            StringBuilder sb = new StringBuilder();
            foreach (Session oS in arrSessions)
            {
                if (oS.requestBodyBytes != null && oS.requestBodyBytes.Length > 0)
                {
                    sb.AppendLine($"// Session {oS.id} - Request");
                    sb.AppendLine(BitConverter.ToString(oS.requestBodyBytes).Replace("-", " "));
                    sb.AppendLine();
                }
                if (oS.responseBodyBytes != null && oS.responseBodyBytes.Length > 0)
                {
                    sb.AppendLine($"// Session {oS.id} - Response");
                    sb.AppendLine(BitConverter.ToString(oS.responseBodyBytes).Replace("-", " "));
                    sb.AppendLine();
                }
            }
            
            Clipboard.SetText(sb.ToString());
            FiddlerApplication.UI.SetStatusText($"Copied {arrSessions.Length} session(s) as hex to clipboard");
        }
        
        [ContextAction("Save SWF to Disk", "Evony RE")]
        public static void DoSaveSWF(Session[] arrSessions)
        {
            int saved = 0;
            foreach (Session oS in arrSessions)
            {
                if (IsSWFFile(oS) && oS.responseBodyBytes != null)
                {
                    ExtractSWF(oS);
                    saved++;
                }
            }
            
            if (saved > 0)
            {
                string swfDir = Path.Combine(ToolkitPath, "extracted-swf");
                MessageBox.Show($"Saved {saved} SWF file(s) to:\n{swfDir}", "Evony RE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No SWF files found in selected sessions.", "Evony RE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════════════
        // QUICK LINK MENU
        // ═══════════════════════════════════════════════════════════════════════════
        
        [QuickLinkMenu("&Evony RE")]
        [QuickLinkItem("CC2 Evony", "https://cc2.evony.com/")]
        [QuickLinkItem("CC1 Evony", "https://cc1.evony.com/")]
        [QuickLinkItem("CC3 Evony", "https://cc3.evony.com/")]
        [QuickLinkItem("---", "")]
        [QuickLinkItem("Evony Wiki", "https://evony.fandom.com/")]
        public static void DoEvonyLinks(string sText, string sAction)
        {
            if (!string.IsNullOrEmpty(sAction))
            {
                Utilities.LaunchHyperlink(sAction);
            }
        }
    }
}
