# 🔍 Fiddler Scripts Library for Evony

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Production Ready

---

## 📋 Overview

This document contains ready-to-use FiddlerScript snippets and complete scripts for capturing, filtering, decoding, and manipulating Evony game traffic.

---

## 🚀 Quick Start

### Installation

1. Open Fiddler Classic
2. Go to `Rules` → `Customize Rules...`
3. Add scripts to `CustomRules.js`
4. Save and restart Fiddler

---

## 📡 Traffic Filtering Scripts

### Script 1: Evony-Only Traffic Filter

```javascript
// ===========================================================
// EVONY TRAFFIC FILTER
// Only shows traffic to/from Evony servers
// ===========================================================

public static RulesOption("Hide &Non-Evony Traffic")
var m_HideNonEvony: boolean = true;

static function OnBeforeRequest(oSession: Session) {
    // List of Evony domains
    var evonyDomains = [
        "cc1.evony.com",
        "cc2.evony.com", 
        "cc3.evony.com",
        "cc4.evony.com",
        "cc5.evony.com",
        "evony.com",
        "evonycdn.com"
    ];
    
    var isEvony = false;
    for (var i = 0; i < evonyDomains.length; i++) {
        if (oSession.HostnameIs(evonyDomains[i])) {
            isEvony = true;
            break;
        }
    }
    
    if (m_HideNonEvony && !isEvony) {
        oSession["ui-hide"] = "true";
    }
    
    // Highlight Evony traffic
    if (isEvony) {
        oSession["ui-color"] = "gold";
        oSession["ui-bold"] = "true";
    }
}
```

### Script 2: CC2.Evony.com Exclusive Filter

```javascript
// ===========================================================
// CC2.EVONY.COM EXCLUSIVE FILTER
// Only captures traffic from cc2.evony.com
// ===========================================================

public static RulesOption("CC2 Only Mode")
var m_CC2Only: boolean = true;

static function OnBeforeRequest(oSession: Session) {
    if (m_CC2Only) {
        if (!oSession.HostnameIs("cc2.evony.com")) {
            oSession["ui-hide"] = "true";
            return;
        }
        
        // Visual styling for CC2 traffic
        oSession["ui-color"] = "#DAA520"; // Evony gold
        oSession["ui-bold"] = "true";
        
        // Add custom column data
        oSession["ui-comments"] = "CC2 Evony Traffic";
    }
}
```

---

## 📦 AMF3 Packet Scripts

### Script 3: AMF3 Traffic Detector

```javascript
// ===========================================================
// AMF3 TRAFFIC DETECTOR
// Identifies and tags AMF3 packets
// ===========================================================

static function OnBeforeResponse(oSession: Session) {
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    var contentType = oSession.oResponse.headers["Content-Type"];
    
    if (contentType && contentType.indexOf("application/x-amf") >= 0) {
        // Tag as AMF traffic
        oSession["ui-color"] = "#FFD700";
        oSession["ui-comments"] = "AMF3 Response";
        
        // Try to extract action name from URL or body
        var actionName = ExtractActionName(oSession);
        if (actionName) {
            oSession["ui-comments"] = "AMF3: " + actionName;
        }
    }
}

static function ExtractActionName(oSession: Session): String {
    // Check URL path for action hints
    var url = oSession.fullUrl;
    
    // Common patterns
    if (url.indexOf("/gateway.php") >= 0) {
        return "Gateway Request";
    }
    if (url.indexOf("/api/") >= 0) {
        var match = url.match(/\/api\/([^\/\?]+)/);
        if (match) return match[1];
    }
    
    // Try to parse AMF body header
    try {
        var body = oSession.GetResponseBodyAsString();
        // Look for action name pattern in AMF
        var actionMatch = body.match(/([a-zA-Z]+\.[a-zA-Z]+)/);
        if (actionMatch) return actionMatch[1];
    } catch(e) {}
    
    return null;
}
```

### Script 4: AMF3 Packet Logger

```javascript
// ===========================================================
// AMF3 PACKET LOGGER
// Logs all AMF3 packets to file with timestamps
// ===========================================================

public static RulesOption("Log AMF3 Packets")
var m_LogAmf: boolean = true;

var logPath = "D:\\Fiddler-FlashBrowser\\logs\\amf-traffic.log";

static function OnBeforeResponse(oSession: Session) {
    if (!m_LogAmf) return;
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    var contentType = oSession.oResponse.headers["Content-Type"];
    if (!contentType || contentType.indexOf("application/x-amf") < 0) return;
    
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    var requestUrl = oSession.fullUrl;
    var responseSize = oSession.responseBodyBytes.Length;
    var requestSize = oSession.requestBodyBytes ? oSession.requestBodyBytes.Length : 0;
    
    var logEntry = String.Format(
        "[{0}] {1}\n" +
        "  Request Size: {2} bytes\n" +
        "  Response Size: {3} bytes\n" +
        "  Request Body (hex): {4}\n" +
        "  Response Body (hex): {5}\n" +
        "---\n",
        timestamp,
        requestUrl,
        requestSize,
        responseSize,
        BytesToHex(oSession.requestBodyBytes),
        BytesToHex(oSession.responseBodyBytes)
    );
    
    // Append to log file
    System.IO.File.AppendAllText(logPath, logEntry);
}

static function BytesToHex(bytes: byte[]): String {
    if (!bytes || bytes.Length == 0) return "(empty)";
    if (bytes.Length > 500) {
        return BitConverter.ToString(bytes, 0, 500).Replace("-", "") + "... (truncated)";
    }
    return BitConverter.ToString(bytes).Replace("-", "");
}
```

---

## 🔧 Packet Modification Scripts

### Script 5: Request Modifier

```javascript
// ===========================================================
// EVONY REQUEST MODIFIER
// Modify outgoing requests before they're sent
// ===========================================================

public static RulesOption("Enable Request Modification")
var m_ModifyRequests: boolean = false;

// Modification rules - customize as needed
var modifications = {
    // Example: Add header to all Evony requests
    addHeaders: {
        "X-Svony-Capture": "true"
    },
    
    // Example: Modify specific actions
    actionModifiers: {
        "hero.getAllHeroLevel": function(data) {
            // Modify request data
            return data;
        }
    }
};

static function OnBeforeRequest(oSession: Session) {
    if (!m_ModifyRequests) return;
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    // Add custom headers
    for (var header in modifications.addHeaders) {
        oSession.oRequest.headers.Add(header, modifications.addHeaders[header]);
    }
    
    // Check for action-specific modifications
    var actionName = ExtractRequestAction(oSession);
    if (actionName && modifications.actionModifiers[actionName]) {
        try {
            var requestBody = oSession.GetRequestBodyAsString();
            var modifiedBody = modifications.actionModifiers[actionName](requestBody);
            oSession.utilSetRequestBody(modifiedBody);
            oSession["ui-comments"] = "MODIFIED: " + actionName;
            oSession["ui-color"] = "orange";
        } catch(e) {
            FiddlerObject.log("Modification error: " + e.message);
        }
    }
}

static function ExtractRequestAction(oSession: Session): String {
    // Extract action name from request
    var url = oSession.fullUrl;
    var actionMatch = url.match(/action=([^&]+)/);
    if (actionMatch) return decodeURIComponent(actionMatch[1]);
    return null;
}
```

### Script 6: Response Modifier

```javascript
// ===========================================================
// EVONY RESPONSE MODIFIER
// Modify server responses before they reach the client
// ===========================================================

public static RulesOption("Enable Response Modification")
var m_ModifyResponses: boolean = false;

static function OnBeforeResponse(oSession: Session) {
    if (!m_ModifyResponses) return;
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    var contentType = oSession.oResponse.headers["Content-Type"];
    if (!contentType || contentType.indexOf("application/x-amf") < 0) return;
    
    // Example: Log response for specific actions
    var actionName = ExtractActionFromResponse(oSession);
    
    // Custom response modifications
    if (actionName == "player.getPlayerInfo") {
        // Example: Modify player info response
        // WARNING: This may cause game issues, use carefully
        // ModifyPlayerInfo(oSession);
        oSession["ui-comments"] = "Response captured: " + actionName;
    }
}

static function ModifyPlayerInfo(oSession: Session) {
    // Example modification (disabled by default)
    // This is for educational purposes only
    /*
    var body = oSession.GetResponseBodyAsString();
    // Modify body...
    oSession.utilSetResponseBody(body);
    */
}
```

---

## 📊 Traffic Analysis Scripts

### Script 7: Action Statistics

```javascript
// ===========================================================
// EVONY ACTION STATISTICS
// Track frequency of different actions
// ===========================================================

var actionStats = {};
var statsFile = "D:\\Fiddler-FlashBrowser\\logs\\action-stats.json";

static function OnBeforeResponse(oSession: Session) {
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    var actionName = ExtractActionName(oSession) || "unknown";
    
    if (!actionStats[actionName]) {
        actionStats[actionName] = {
            count: 0,
            totalRequestBytes: 0,
            totalResponseBytes: 0,
            firstSeen: DateTime.Now.ToString(),
            lastSeen: null
        };
    }
    
    actionStats[actionName].count++;
    actionStats[actionName].totalRequestBytes += oSession.requestBodyBytes ? oSession.requestBodyBytes.Length : 0;
    actionStats[actionName].totalResponseBytes += oSession.responseBodyBytes.Length;
    actionStats[actionName].lastSeen = DateTime.Now.ToString();
    
    // Save stats periodically
    if (actionStats[actionName].count % 10 == 0) {
        SaveStats();
    }
}

static function SaveStats() {
    try {
        var json = Fiddler.WebFormats.JSON.JsonEncode(actionStats);
        System.IO.File.WriteAllText(statsFile, json);
    } catch(e) {
        FiddlerObject.log("Stats save error: " + e.message);
    }
}

// Menu item to show stats
public static ToolsAction("Show Evony Action Stats")
function ShowStats() {
    var output = "=== Evony Action Statistics ===\n\n";
    
    var sortedActions = [];
    for (var action in actionStats) {
        sortedActions.push({ name: action, stats: actionStats[action] });
    }
    sortedActions.sort(function(a, b) { return b.stats.count - a.stats.count; });
    
    for (var i = 0; i < sortedActions.length; i++) {
        var a = sortedActions[i];
        output += String.Format("{0}: {1} calls ({2} KB sent, {3} KB received)\n",
            a.name,
            a.stats.count,
            (a.stats.totalRequestBytes / 1024).toFixed(2),
            (a.stats.totalResponseBytes / 1024).toFixed(2)
        );
    }
    
    FiddlerObject.alert(output);
}
```

### Script 8: Session Recording

```javascript
// ===========================================================
// EVONY SESSION RECORDER
// Record complete game sessions for replay/analysis
// ===========================================================

public static RulesOption("Record Evony Session")
var m_Recording: boolean = false;

var sessionData = [];
var sessionFile = "";

public static ToolsAction("Start Recording")
function StartRecording() {
    sessionData = [];
    sessionFile = "D:\\Fiddler-FlashBrowser\\sessions\\session-" + 
                  DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
    m_Recording = true;
    FiddlerObject.StatusText = "Recording Evony session...";
}

public static ToolsAction("Stop Recording")
function StopRecording() {
    m_Recording = false;
    SaveSession();
    FiddlerObject.StatusText = "Session saved to: " + sessionFile;
}

static function OnBeforeResponse(oSession: Session) {
    if (!m_Recording) return;
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    var packet = {
        timestamp: DateTime.Now.ToString("o"),
        url: oSession.fullUrl,
        method: oSession.oRequest.headers.HTTPMethod,
        requestHeaders: GetHeaders(oSession.oRequest.headers),
        requestBody: BytesToBase64(oSession.requestBodyBytes),
        responseCode: oSession.responseCode,
        responseHeaders: GetHeaders(oSession.oResponse.headers),
        responseBody: BytesToBase64(oSession.responseBodyBytes)
    };
    
    sessionData.push(packet);
}

static function SaveSession() {
    var json = Fiddler.WebFormats.JSON.JsonEncode({
        recordedAt: DateTime.Now.ToString("o"),
        server: "cc2.evony.com",
        packetCount: sessionData.length,
        packets: sessionData
    });
    
    System.IO.File.WriteAllText(sessionFile, json);
}

static function GetHeaders(headers): Object {
    var result = {};
    for (var i = 0; i < headers.Count(); i++) {
        var header = headers[i];
        result[header.Name] = header.Value;
    }
    return result;
}

static function BytesToBase64(bytes: byte[]): String {
    if (!bytes || bytes.Length == 0) return "";
    return Convert.ToBase64String(bytes);
}
```

---

## 🔗 Svony Browser Integration

### Script 9: Named Pipe Forwarder

```javascript
// ===========================================================
// SVONY BROWSER PIPE FORWARDER
// Sends traffic to Svony Browser via named pipe
// ===========================================================

import System.IO.Pipes;

var pipeName = "SvonyFiddlerTraffic";
var pipeClient: NamedPipeClientStream = null;
var pipeWriter: System.IO.StreamWriter = null;

public static RulesOption("Forward to Svony Browser")
var m_ForwardToSvony: boolean = true;

static function Main() {
    // Initialize pipe connection on startup
    ConnectToPipe();
}

static function ConnectToPipe() {
    try {
        pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        pipeClient.Connect(1000); // 1 second timeout
        pipeWriter = new System.IO.StreamWriter(pipeClient);
        pipeWriter.AutoFlush = true;
        FiddlerObject.log("Connected to Svony Browser pipe");
    } catch(e) {
        FiddlerObject.log("Svony pipe not available: " + e.message);
        pipeClient = null;
        pipeWriter = null;
    }
}

static function OnBeforeResponse(oSession: Session) {
    if (!m_ForwardToSvony) return;
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    // Reconnect if needed
    if (pipeClient == null || !pipeClient.IsConnected) {
        ConnectToPipe();
    }
    
    if (pipeWriter == null) return;
    
    try {
        var packet = {
            type: "traffic",
            timestamp: DateTime.Now.ToString("o"),
            url: oSession.fullUrl,
            requestHex: BytesToHex(oSession.requestBodyBytes),
            responseHex: BytesToHex(oSession.responseBodyBytes),
            contentType: oSession.oResponse.headers["Content-Type"]
        };
        
        var json = Fiddler.WebFormats.JSON.JsonEncode(packet);
        pipeWriter.WriteLine(json);
    } catch(e) {
        FiddlerObject.log("Pipe write error: " + e.message);
        pipeClient = null;
        pipeWriter = null;
    }
}

static function BytesToHex(bytes: byte[]): String {
    if (!bytes || bytes.Length == 0) return "";
    return BitConverter.ToString(bytes).Replace("-", "");
}
```

### Script 10: Real-Time Notification

```javascript
// ===========================================================
// EVONY EVENT NOTIFICATIONS
// Send desktop notifications for important events
// ===========================================================

import System.Windows.Forms;

public static RulesOption("Enable Evony Notifications")
var m_Notifications: boolean = true;

// Events to notify about
var notifyEvents = [
    "alliance.war",
    "battle.report",
    "mail.new",
    "event.start",
    "monster.spawn"
];

static function OnBeforeResponse(oSession: Session) {
    if (!m_Notifications) return;
    if (!oSession.HostnameIs("cc2.evony.com")) return;
    
    var actionName = ExtractActionName(oSession);
    if (!actionName) return;
    
    // Check if this is a notable event
    for (var i = 0; i < notifyEvents.length; i++) {
        if (actionName.toLowerCase().indexOf(notifyEvents[i]) >= 0) {
            ShowNotification("Evony Event", actionName);
            break;
        }
    }
}

static function ShowNotification(title: String, message: String) {
    try {
        var notify = new NotifyIcon();
        notify.Icon = SystemIcons.Information;
        notify.Visible = true;
        notify.BalloonTipTitle = title;
        notify.BalloonTipText = message;
        notify.ShowBalloonTip(3000);
        
        // Cleanup after display
        System.Threading.Thread.Sleep(3500);
        notify.Dispose();
    } catch(e) {
        FiddlerObject.log("Notification error: " + e.message);
    }
}
```

---

## 🛡️ Security Scripts

### Script 11: Sensitive Data Filter

```javascript
// ===========================================================
// SENSITIVE DATA FILTER
// Prevents logging of sensitive information
// ===========================================================

var sensitivePatterns = [
    /password/i,
    /token/i,
    /session_?id/i,
    /auth/i,
    /secret/i,
    /key/i
];

static function SanitizeForLog(data: String): String {
    var sanitized = data;
    
    for (var i = 0; i < sensitivePatterns.length; i++) {
        var pattern = sensitivePatterns[i];
        // Replace sensitive values with [REDACTED]
        sanitized = sanitized.replace(
            new RegExp('("' + pattern.source + '"\\s*:\\s*)"[^"]*"', 'gi'),
            '$1"[REDACTED]"'
        );
    }
    
    return sanitized;
}
```

---

## 📋 Complete CustomRules.js Template

```javascript
// ===========================================================
// SVONY BROWSER - COMPLETE FIDDLER RULES
// Copy this entire file to your CustomRules.js
// ===========================================================

import System;
import System.Windows.Forms;
import Fiddler;
import System.IO.Pipes;

class Handlers
{
    // =====================
    // OPTIONS
    // =====================
    
    public static RulesOption("Hide Non-Evony Traffic")
    var m_HideNonEvony: boolean = true;
    
    public static RulesOption("CC2 Only Mode")
    var m_CC2Only: boolean = false;
    
    public static RulesOption("Log AMF3 Packets")
    var m_LogAmf: boolean = true;
    
    public static RulesOption("Forward to Svony Browser")
    var m_ForwardToSvony: boolean = true;
    
    // =====================
    // VARIABLES
    // =====================
    
    static var logPath = "D:\\Fiddler-FlashBrowser\\logs\\evony-traffic.log";
    static var pipeName = "SvonyFiddlerTraffic";
    static var pipeClient: NamedPipeClientStream = null;
    static var pipeWriter: System.IO.StreamWriter = null;
    
    static var evonyDomains = [
        "cc1.evony.com", "cc2.evony.com", "cc3.evony.com",
        "cc4.evony.com", "cc5.evony.com"
    ];
    
    // =====================
    // MAIN HANDLERS
    // =====================
    
    static function OnBeforeRequest(oSession: Session) {
        var isEvony = IsEvonyHost(oSession.hostname);
        
        // Filter non-Evony traffic
        if (m_HideNonEvony && !isEvony) {
            oSession["ui-hide"] = "true";
            return;
        }
        
        // CC2 only mode
        if (m_CC2Only && !oSession.HostnameIs("cc2.evony.com")) {
            oSession["ui-hide"] = "true";
            return;
        }
        
        // Style Evony traffic
        if (isEvony) {
            oSession["ui-color"] = "#DAA520";
            oSession["ui-bold"] = "true";
        }
    }
    
    static function OnBeforeResponse(oSession: Session) {
        if (!IsEvonyHost(oSession.hostname)) return;
        
        var contentType = oSession.oResponse.headers["Content-Type"];
        var isAmf = contentType && contentType.indexOf("application/x-amf") >= 0;
        
        if (isAmf) {
            // Tag AMF traffic
            oSession["ui-comments"] = "AMF3";
            
            // Log if enabled
            if (m_LogAmf) {
                LogPacket(oSession);
            }
            
            // Forward to Svony Browser
            if (m_ForwardToSvony) {
                ForwardToSvony(oSession);
            }
        }
    }
    
    // =====================
    // HELPER FUNCTIONS
    // =====================
    
    static function IsEvonyHost(hostname: String): boolean {
        hostname = hostname.toLowerCase();
        for (var i = 0; i < evonyDomains.length; i++) {
            if (hostname == evonyDomains[i]) return true;
        }
        return false;
    }
    
    static function LogPacket(oSession: Session) {
        try {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var entry = String.Format(
                "[{0}] {1} - {2} bytes\n",
                timestamp,
                oSession.fullUrl,
                oSession.responseBodyBytes.Length
            );
            System.IO.File.AppendAllText(logPath, entry);
        } catch(e) {}
    }
    
    static function ForwardToSvony(oSession: Session) {
        // Reconnect if needed
        if (pipeClient == null || !pipeClient.IsConnected) {
            try {
                pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                pipeClient.Connect(100);
                pipeWriter = new System.IO.StreamWriter(pipeClient);
                pipeWriter.AutoFlush = true;
            } catch(e) {
                return;
            }
        }
        
        try {
            var hex = BitConverter.ToString(oSession.responseBodyBytes).Replace("-", "");
            pipeWriter.WriteLine(hex);
        } catch(e) {
            pipeClient = null;
        }
    }
    
    // =====================
    // MENU ACTIONS
    // =====================
    
    public static ToolsAction("Clear Evony Log")
    function ClearLog() {
        try {
            System.IO.File.WriteAllText(logPath, "");
            FiddlerObject.StatusText = "Evony log cleared";
        } catch(e) {
            FiddlerObject.alert("Error: " + e.message);
        }
    }
    
    public static ToolsAction("Open Log Folder")
    function OpenLogFolder() {
        System.Diagnostics.Process.Start("D:\\Fiddler-FlashBrowser\\logs");
    }
}
```

---

## 📚 Related Documentation

- [FEATURE-ROADMAP.md](./FEATURE-ROADMAP.md) - Overall feature roadmap
- [RAG-RTE-INTEGRATION.md](./RAG-RTE-INTEGRATION.md) - RAG/RTE backend details
- [CLI-TOOLS.md](./CLI-TOOLS.md) - CLI tools documentation
