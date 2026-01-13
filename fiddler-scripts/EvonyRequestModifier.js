/**
 * Evony Request Modifier FiddlerScript
 * Allows modification of outgoing requests for testing and analysis
 * 
 * Features:
 * - Modify request parameters
 * - Inject custom headers
 * - Log specific actions
 * - Rate limiting bypass
 */

// ============================================================================
// Configuration
// ============================================================================

var ENABLE_MODIFICATIONS = false;
var LOG_ACTIONS = ["hero.hireHero", "army.marchArmy", "castle.upgradeBuilding"];
var INJECT_HEADERS = {
    "X-Svony-Debug": "true",
    "X-Svony-Version": "1.0.0"
};

// Modification rules
var MODIFICATION_RULES = [];

// ============================================================================
// Modification Rule Class
// ============================================================================

class ModificationRule {
    constructor(options) {
        this.name = options.name || "Unnamed Rule";
        this.enabled = options.enabled !== false;
        this.actionPattern = options.actionPattern || null;
        this.urlPattern = options.urlPattern || null;
        this.modifications = options.modifications || {};
        this.headerMods = options.headerMods || {};
    }
    
    matches(session, action) {
        if (!this.enabled) return false;
        
        if (this.actionPattern && action) {
            var regex = new RegExp(this.actionPattern, "i");
            if (!regex.test(action)) return false;
        }
        
        if (this.urlPattern) {
            var regex = new RegExp(this.urlPattern, "i");
            if (!regex.test(session.fullUrl)) return false;
        }
        
        return true;
    }
    
    apply(session, decodedData) {
        // Apply data modifications
        for (var path in this.modifications) {
            var value = this.modifications[path];
            setNestedValue(decodedData, path, value);
        }
        
        // Apply header modifications
        for (var header in this.headerMods) {
            session.RequestHeaders[header] = this.headerMods[header];
        }
        
        return decodedData;
    }
}

// ============================================================================
// Helper Functions
// ============================================================================

function setNestedValue(obj, path, value) {
    var parts = path.split(".");
    var current = obj;
    
    for (var i = 0; i < parts.length - 1; i++) {
        if (!current[parts[i]]) {
            current[parts[i]] = {};
        }
        current = current[parts[i]];
    }
    
    current[parts[parts.length - 1]] = value;
}

function getNestedValue(obj, path) {
    var parts = path.split(".");
    var current = obj;
    
    for (var i = 0; i < parts.length; i++) {
        if (!current || current[parts[i]] === undefined) {
            return undefined;
        }
        current = current[parts[i]];
    }
    
    return current;
}

function extractAction(decodedData) {
    if (decodedData && decodedData.action) {
        return decodedData.action;
    }
    return null;
}

// ============================================================================
// Default Rules
// ============================================================================

// Add some default modification rules
MODIFICATION_RULES.push(new ModificationRule({
    name: "Debug Mode",
    enabled: false,
    actionPattern: ".*",
    modifications: {},
    headerMods: INJECT_HEADERS
}));

MODIFICATION_RULES.push(new ModificationRule({
    name: "Speed Up Training",
    enabled: false,
    actionPattern: "troop\\.produceTroop",
    modifications: {
        "data.speedUp": true
    }
}));

MODIFICATION_RULES.push(new ModificationRule({
    name: "Max Resources",
    enabled: false,
    actionPattern: "castle\\.upgradeBuilding",
    modifications: {
        "data.useGold": false
    }
}));

// ============================================================================
// FiddlerScript Handlers
// ============================================================================

/**
 * Modify outgoing requests
 */
static function OnBeforeRequest(oSession) {
    if (!ENABLE_MODIFICATIONS) return;
    if (!isEvonyHost(oSession.hostname)) return;
    
    try {
        var body = oSession.RequestBody;
        if (!body || body.Length === 0) return;
        
        // Decode AMF3
        var decoded = decodeAMF3(body);
        if (!decoded) return;
        
        var action = extractAction(decoded);
        var modified = false;
        
        // Apply matching rules
        for (var i = 0; i < MODIFICATION_RULES.length; i++) {
            var rule = MODIFICATION_RULES[i];
            if (rule.matches(oSession, action)) {
                decoded = rule.apply(oSession, decoded);
                modified = true;
                FiddlerApplication.Log.LogString("Applied rule: " + rule.name);
            }
        }
        
        // Re-encode if modified
        if (modified) {
            var encoded = encodeAMF3(decoded);
            oSession.RequestBody = encoded;
            oSession["ui-color"] = "orange";
            oSession["ui-comments"] = "Modified by Svony";
        }
        
        // Log specific actions
        if (LOG_ACTIONS.indexOf(action) >= 0) {
            FiddlerApplication.Log.LogString("Action: " + action + " - " + JSON.stringify(decoded));
        }
        
    } catch (e) {
        FiddlerApplication.Log.LogString("Modification error: " + e.message);
    }
}

// ============================================================================
// Menu Items
// ============================================================================

public static ToolsAction("Evony: Toggle Modifications") {
    ENABLE_MODIFICATIONS = !ENABLE_MODIFICATIONS;
    FiddlerApplication.Log.LogString("Modifications: " + (ENABLE_MODIFICATIONS ? "Enabled" : "Disabled"));
}

public static ToolsAction("Evony: Add Modification Rule") {
    // Show dialog to add new rule
    var ruleName = FiddlerApplication.UI.GetUserInput("Rule Name", "New Rule");
    var actionPattern = FiddlerApplication.UI.GetUserInput("Action Pattern (regex)", ".*");
    
    MODIFICATION_RULES.push(new ModificationRule({
        name: ruleName,
        enabled: true,
        actionPattern: actionPattern,
        modifications: {}
    }));
    
    FiddlerApplication.Log.LogString("Added rule: " + ruleName);
}

public static ToolsAction("Evony: List Rules") {
    var ruleList = "Modification Rules:\n";
    for (var i = 0; i < MODIFICATION_RULES.length; i++) {
        var rule = MODIFICATION_RULES[i];
        ruleList += (i + 1) + ". " + rule.name + " (" + (rule.enabled ? "Enabled" : "Disabled") + ")\n";
    }
    FiddlerApplication.UI.ShowMessageBox(ruleList, "Modification Rules");
}

public static ToolsAction("Evony: Clear Rules") {
    MODIFICATION_RULES = [];
    FiddlerApplication.Log.LogString("All rules cleared");
}
