/**
 * Evony Auto-Responder FiddlerScript
 * Provides mock responses for testing and offline development
 * 
 * Features:
 * - Mock server responses
 * - Record and replay traffic
 * - Simulate server errors
 * - Latency injection
 */

// ============================================================================
// Configuration
// ============================================================================

var ENABLE_AUTO_RESPONSE = false;
var RECORD_MODE = false;
var REPLAY_MODE = false;
var INJECT_LATENCY_MS = 0;
var SIMULATE_ERROR_RATE = 0; // 0-100

var RECORDED_RESPONSES = {};
var MOCK_RESPONSES = {};

// ============================================================================
// Mock Response Templates
// ============================================================================

MOCK_RESPONSES["hero.getHeroList"] = {
    "ok": 1,
    "heroes": [
        {
            "heroId": 1001,
            "name": "Test Hero",
            "level": 100,
            "attack": 500,
            "defense": 400,
            "politics": 300,
            "intelligence": 350
        }
    ]
};

MOCK_RESPONSES["castle.getCastleInfo"] = {
    "ok": 1,
    "castle": {
        "cityId": 1,
        "name": "Test Castle",
        "level": 10,
        "resources": {
            "food": 1000000,
            "lumber": 1000000,
            "stone": 1000000,
            "iron": 1000000,
            "gold": 100000
        }
    }
};

MOCK_RESPONSES["troop.getTroopList"] = {
    "ok": 1,
    "troops": {
        "worker": 10000,
        "warrior": 50000,
        "scout": 10000,
        "pikeman": 30000,
        "swordsman": 20000,
        "archer": 100000,
        "cavalry": 50000,
        "cataphract": 10000,
        "transporter": 5000,
        "ballista": 5000,
        "ram": 2000,
        "catapult": 1000
    }
};

MOCK_RESPONSES["tech.getTechList"] = {
    "ok": 1,
    "techs": {
        "agriculture": 10,
        "lumbering": 10,
        "masonry": 10,
        "mining": 10,
        "metalCasting": 10,
        "informatics": 10,
        "militaryScience": 10,
        "militaryTradition": 10
    }
};

// Error responses
var ERROR_RESPONSES = {
    "server_busy": { "ok": 0, "errorCode": 1001, "errorMsg": "Server is busy" },
    "invalid_session": { "ok": 0, "errorCode": 1002, "errorMsg": "Invalid session" },
    "insufficient_resources": { "ok": 0, "errorCode": 2001, "errorMsg": "Insufficient resources" },
    "cooldown": { "ok": 0, "errorCode": 3001, "errorMsg": "Action on cooldown" }
};

// ============================================================================
// Helper Functions
// ============================================================================

function shouldSimulateError() {
    if (SIMULATE_ERROR_RATE <= 0) return false;
    return Math.random() * 100 < SIMULATE_ERROR_RATE;
}

function getRandomError() {
    var keys = Object.keys(ERROR_RESPONSES);
    var randomKey = keys[Math.floor(Math.random() * keys.length)];
    return ERROR_RESPONSES[randomKey];
}

function createMockResponse(action, requestData) {
    // Check for recorded response first
    var recordKey = action + "_" + JSON.stringify(requestData);
    if (RECORDED_RESPONSES[recordKey]) {
        return RECORDED_RESPONSES[recordKey];
    }
    
    // Check for mock template
    if (MOCK_RESPONSES[action]) {
        return MOCK_RESPONSES[action];
    }
    
    // Default success response
    return { "ok": 1, "data": {} };
}

function recordResponse(action, requestData, responseData) {
    var recordKey = action + "_" + JSON.stringify(requestData);
    RECORDED_RESPONSES[recordKey] = responseData;
    FiddlerApplication.Log.LogString("Recorded response for: " + action);
}

// ============================================================================
// FiddlerScript Handlers
// ============================================================================

/**
 * Record responses in record mode
 */
static function OnBeforeResponse(oSession) {
    if (!RECORD_MODE) return;
    if (!isEvonyHost(oSession.hostname)) return;
    
    try {
        // Get request action
        var requestBody = oSession.RequestBody;
        var requestDecoded = decodeAMF3(requestBody);
        var action = requestDecoded ? requestDecoded.action : null;
        
        if (!action) return;
        
        // Decode response
        oSession.utilDecodeResponse();
        var responseBody = oSession.ResponseBody;
        var responseDecoded = decodeAMF3(responseBody);
        
        if (responseDecoded) {
            recordResponse(action, requestDecoded.data, responseDecoded);
        }
        
    } catch (e) {
        FiddlerApplication.Log.LogString("Record error: " + e.message);
    }
}

/**
 * Auto-respond in replay mode
 */
static function OnBeforeRequest(oSession) {
    if (!ENABLE_AUTO_RESPONSE && !REPLAY_MODE) return;
    if (!isEvonyHost(oSession.hostname)) return;
    
    try {
        var body = oSession.RequestBody;
        if (!body || body.Length === 0) return;
        
        var decoded = decodeAMF3(body);
        if (!decoded || !decoded.action) return;
        
        var action = decoded.action;
        
        // Check if we should auto-respond
        if (REPLAY_MODE || MOCK_RESPONSES[action]) {
            // Inject latency if configured
            if (INJECT_LATENCY_MS > 0) {
                System.Threading.Thread.Sleep(INJECT_LATENCY_MS);
            }
            
            // Simulate error if configured
            var responseData;
            if (shouldSimulateError()) {
                responseData = getRandomError();
                oSession["ui-color"] = "red";
            } else {
                responseData = createMockResponse(action, decoded.data);
                oSession["ui-color"] = "purple";
            }
            
            // Encode response
            var responseBody = encodeAMF3(responseData);
            
            // Create fake response
            oSession.utilCreateResponseAndBypassServer();
            oSession.ResponseHeaders["Content-Type"] = "application/x-amf";
            oSession.ResponseBody = responseBody;
            oSession.responseCode = 200;
            
            oSession["ui-comments"] = "Auto-Response: " + action;
            FiddlerApplication.Log.LogString("Auto-responded to: " + action);
        }
        
    } catch (e) {
        FiddlerApplication.Log.LogString("Auto-response error: " + e.message);
    }
}

// ============================================================================
// Menu Items
// ============================================================================

public static ToolsAction("Evony: Toggle Auto-Response") {
    ENABLE_AUTO_RESPONSE = !ENABLE_AUTO_RESPONSE;
    FiddlerApplication.Log.LogString("Auto-Response: " + (ENABLE_AUTO_RESPONSE ? "Enabled" : "Disabled"));
}

public static ToolsAction("Evony: Toggle Record Mode") {
    RECORD_MODE = !RECORD_MODE;
    FiddlerApplication.Log.LogString("Record Mode: " + (RECORD_MODE ? "Enabled" : "Disabled"));
}

public static ToolsAction("Evony: Toggle Replay Mode") {
    REPLAY_MODE = !REPLAY_MODE;
    FiddlerApplication.Log.LogString("Replay Mode: " + (REPLAY_MODE ? "Enabled" : "Disabled"));
}

public static ToolsAction("Evony: Set Latency") {
    var input = FiddlerApplication.UI.GetUserInput("Latency (ms)", INJECT_LATENCY_MS.toString());
    INJECT_LATENCY_MS = parseInt(input) || 0;
    FiddlerApplication.Log.LogString("Latency set to: " + INJECT_LATENCY_MS + "ms");
}

public static ToolsAction("Evony: Set Error Rate") {
    var input = FiddlerApplication.UI.GetUserInput("Error Rate (0-100)", SIMULATE_ERROR_RATE.toString());
    SIMULATE_ERROR_RATE = Math.min(100, Math.max(0, parseInt(input) || 0));
    FiddlerApplication.Log.LogString("Error rate set to: " + SIMULATE_ERROR_RATE + "%");
}

public static ToolsAction("Evony: Clear Recorded Responses") {
    RECORDED_RESPONSES = {};
    FiddlerApplication.Log.LogString("Recorded responses cleared");
}

public static ToolsAction("Evony: Export Recorded Responses") {
    var json = JSON.stringify(RECORDED_RESPONSES, null, 2);
    var path = "C:\\Svony\\data\\recorded-responses.json";
    System.IO.File.WriteAllText(path, json);
    FiddlerApplication.Log.LogString("Exported to: " + path);
}

public static ToolsAction("Evony: Import Recorded Responses") {
    var path = "C:\\Svony\\data\\recorded-responses.json";
    if (System.IO.File.Exists(path)) {
        var json = System.IO.File.ReadAllText(path);
        RECORDED_RESPONSES = JSON.parse(json);
        FiddlerApplication.Log.LogString("Imported " + Object.keys(RECORDED_RESPONSES).length + " responses");
    } else {
        FiddlerApplication.Log.LogString("File not found: " + path);
    }
}

public static ToolsAction("Evony: Add Mock Response") {
    var action = FiddlerApplication.UI.GetUserInput("Action Name", "hero.getHeroList");
    var response = FiddlerApplication.UI.GetUserInput("Response JSON", '{"ok":1}');
    
    try {
        MOCK_RESPONSES[action] = JSON.parse(response);
        FiddlerApplication.Log.LogString("Added mock for: " + action);
    } catch (e) {
        FiddlerApplication.Log.LogString("Invalid JSON: " + e.message);
    }
}
