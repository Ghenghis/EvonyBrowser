/**
 * Evony Traffic Capture FiddlerScript
 * Captures and forwards Evony game traffic to Svony Browser via named pipe
 * 
 * Installation:
 * 1. Open Fiddler → Rules → Customize Rules
 * 2. Add this script to the Handlers class
 * 3. Restart Fiddler
 */

// ============================================================================
// Configuration
// ============================================================================

var EVONY_HOSTS = [
    "evony.com",
    "evonyserver.com",
    "evonygame.com",
    "enoagames.com",
    "topgamesinc.com",
    "eve.gtarcade.com"
];

var PIPE_NAME = "\\\\.\\pipe\\EvonyTraffic";
var LOG_FILE = "C:\\Svony\\logs\\fiddler-traffic.log";
var ENABLE_LOGGING = true;
var CAPTURE_ALL_TRAFFIC = false;

// ============================================================================
// Pipe Communication
// ============================================================================

class NamedPipeClient {
    constructor(pipeName) {
        this.pipeName = pipeName;
        this.pipe = null;
        this.connected = false;
    }
    
    connect() {
        try {
            // Note: In actual FiddlerScript, use .NET interop
            // This is a conceptual implementation
            this.pipe = System.IO.Pipes.NamedPipeClientStream(".", this.pipeName.replace("\\\\.\\pipe\\", ""), System.IO.Pipes.PipeDirection.Out);
            this.pipe.Connect(1000);
            this.connected = true;
            return true;
        } catch (e) {
            this.connected = false;
            return false;
        }
    }
    
    send(data) {
        if (!this.connected) {
            if (!this.connect()) return false;
        }
        
        try {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data + "\n");
            this.pipe.Write(bytes, 0, bytes.Length);
            this.pipe.Flush();
            return true;
        } catch (e) {
            this.connected = false;
            return false;
        }
    }
    
    close() {
        if (this.pipe) {
            try {
                this.pipe.Close();
            } catch (e) {}
        }
        this.connected = false;
    }
}

var pipeClient = new NamedPipeClient(PIPE_NAME);

// ============================================================================
// Helper Functions
// ============================================================================

function isEvonyHost(hostname) {
    if (CAPTURE_ALL_TRAFFIC) return true;
    
    hostname = hostname.toLowerCase();
    for (var i = 0; i < EVONY_HOSTS.length; i++) {
        if (hostname.indexOf(EVONY_HOSTS[i]) >= 0) {
            return true;
        }
    }
    return false;
}

function toHexString(bytes) {
    var hex = "";
    for (var i = 0; i < bytes.Length; i++) {
        hex += bytes[i].toString(16).padStart(2, "0");
    }
    return hex;
}

function logMessage(message) {
    if (!ENABLE_LOGGING) return;
    
    try {
        var timestamp = new Date().toISOString();
        var logEntry = timestamp + " - " + message + "\n";
        System.IO.File.AppendAllText(LOG_FILE, logEntry);
    } catch (e) {}
}

function createTrafficEntry(session, direction, body) {
    return {
        id: session.id.toString(),
        direction: direction,
        url: session.fullUrl,
        method: session.RequestMethod,
        host: session.hostname,
        path: session.PathAndQuery,
        contentType: direction === "request" 
            ? session.RequestHeaders["Content-Type"] 
            : session.ResponseHeaders["Content-Type"],
        contentLength: body ? body.Length : 0,
        hexData: body ? toHexString(body) : "",
        timestamp: Date.now(),
        statusCode: direction === "response" ? session.responseCode : null,
        headers: direction === "request" 
            ? getHeadersDict(session.RequestHeaders)
            : getHeadersDict(session.ResponseHeaders)
    };
}

function getHeadersDict(headers) {
    var dict = {};
    for (var i = 0; i < headers.Count(); i++) {
        var header = headers[i];
        dict[header.Name] = header.Value;
    }
    return dict;
}

// ============================================================================
// FiddlerScript Handlers
// ============================================================================

/**
 * Called before a request is sent to the server
 */
static function OnBeforeRequest(oSession) {
    if (!isEvonyHost(oSession.hostname)) return;
    
    try {
        // Mark session for response capture
        oSession["X-Evony-Capture"] = "true";
        
        // Get request body
        var body = oSession.RequestBody;
        
        // Create traffic entry
        var entry = createTrafficEntry(oSession, "request", body);
        
        // Send to pipe
        var json = Fiddler.WebFormats.JSON.JsonEncode(entry);
        pipeClient.send(json);
        
        logMessage("Captured request: " + oSession.fullUrl);
        
        // Add visual indicator
        oSession["ui-color"] = "blue";
        oSession["ui-comments"] = "Evony Request Captured";
        
    } catch (e) {
        logMessage("Error capturing request: " + e.message);
    }
}

/**
 * Called after a response is received from the server
 */
static function OnBeforeResponse(oSession) {
    if (oSession["X-Evony-Capture"] !== "true") return;
    
    try {
        // Decompress response if needed
        oSession.utilDecodeResponse();
        
        // Get response body
        var body = oSession.ResponseBody;
        
        // Create traffic entry
        var entry = createTrafficEntry(oSession, "response", body);
        
        // Send to pipe
        var json = Fiddler.WebFormats.JSON.JsonEncode(entry);
        pipeClient.send(json);
        
        logMessage("Captured response: " + oSession.fullUrl + " (" + body.Length + " bytes)");
        
        // Update visual indicator
        oSession["ui-color"] = "green";
        oSession["ui-comments"] = "Evony Response Captured (" + body.Length + " bytes)";
        
    } catch (e) {
        logMessage("Error capturing response: " + e.message);
    }
}

/**
 * Called when Fiddler is shutting down
 */
static function OnShutdown() {
    pipeClient.close();
    logMessage("Fiddler shutdown - pipe closed");
}

// ============================================================================
// Menu Items
// ============================================================================

/**
 * Add custom menu items to Fiddler
 */
public static ToolsAction("Evony: Start Capture") {
    pipeClient.connect();
    FiddlerApplication.Log.LogString("Evony capture started");
}

public static ToolsAction("Evony: Stop Capture") {
    pipeClient.close();
    FiddlerApplication.Log.LogString("Evony capture stopped");
}

public static ToolsAction("Evony: Clear Log") {
    try {
        System.IO.File.WriteAllText(LOG_FILE, "");
        FiddlerApplication.Log.LogString("Log file cleared");
    } catch (e) {
        FiddlerApplication.Log.LogString("Error clearing log: " + e.message);
    }
}

public static ToolsAction("Evony: Toggle All Traffic") {
    CAPTURE_ALL_TRAFFIC = !CAPTURE_ALL_TRAFFIC;
    FiddlerApplication.Log.LogString("Capture all traffic: " + CAPTURE_ALL_TRAFFIC);
}
