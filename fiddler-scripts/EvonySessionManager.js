/**
 * Evony Session Manager FiddlerScript
 * Manages session tokens, authentication, and auto-reconnection
 * 
 * Features:
 * - Session token extraction and storage
 * - Auto-reconnect on session expiry
 * - Multi-account session management
 * - Session health monitoring
 */

// Session storage
var evonySessions = {};
var sessionHealthInterval = null;

/**
 * Initialize the session manager
 */
function initSessionManager() {
    FiddlerObject.log("Evony Session Manager initialized");
    startSessionHealthMonitor();
}

/**
 * Extract session token from response
 */
function extractSessionToken(session) {
    if (!session.oResponse || !session.oResponse.headers) return null;
    
    // Check for session cookie
    var setCookie = session.oResponse.headers["Set-Cookie"];
    if (setCookie) {
        var match = setCookie.match(/EVONY_SESSION=([^;]+)/);
        if (match) {
            return match[1];
        }
    }
    
    // Check response body for token
    try {
        var body = session.GetResponseBodyAsString();
        if (body) {
            var tokenMatch = body.match(/"sessionToken"\s*:\s*"([^"]+)"/);
            if (tokenMatch) {
                return tokenMatch[1];
            }
        }
    } catch (e) {
        // Ignore parse errors
    }
    
    return null;
}

/**
 * Store session information
 */
function storeSession(server, username, token, timestamp) {
    var key = server + "_" + username;
    evonySessions[key] = {
        server: server,
        username: username,
        token: token,
        timestamp: timestamp || new Date().getTime(),
        lastActivity: new Date().getTime(),
        isValid: true
    };
    
    FiddlerObject.log("Session stored: " + key);
    saveSessionsToFile();
}

/**
 * Get session for server/username
 */
function getSession(server, username) {
    var key = server + "_" + username;
    return evonySessions[key] || null;
}

/**
 * Update session activity timestamp
 */
function updateSessionActivity(server, username) {
    var key = server + "_" + username;
    if (evonySessions[key]) {
        evonySessions[key].lastActivity = new Date().getTime();
    }
}

/**
 * Mark session as invalid
 */
function invalidateSession(server, username) {
    var key = server + "_" + username;
    if (evonySessions[key]) {
        evonySessions[key].isValid = false;
        FiddlerObject.log("Session invalidated: " + key);
    }
}

/**
 * Check if session is expired
 */
function isSessionExpired(session) {
    if (!session || !session.isValid) return true;
    
    var now = new Date().getTime();
    var sessionAge = now - session.timestamp;
    var inactivityTime = now - session.lastActivity;
    
    // Session expires after 30 minutes of inactivity or 24 hours total
    var maxInactivity = 30 * 60 * 1000; // 30 minutes
    var maxAge = 24 * 60 * 60 * 1000; // 24 hours
    
    return inactivityTime > maxInactivity || sessionAge > maxAge;
}

/**
 * Start session health monitor
 */
function startSessionHealthMonitor() {
    if (sessionHealthInterval) return;
    
    sessionHealthInterval = setInterval(function() {
        checkAllSessionsHealth();
    }, 60000); // Check every minute
}

/**
 * Check health of all sessions
 */
function checkAllSessionsHealth() {
    var expiredSessions = [];
    
    for (var key in evonySessions) {
        if (evonySessions.hasOwnProperty(key)) {
            var session = evonySessions[key];
            if (isSessionExpired(session)) {
                expiredSessions.push(key);
            }
        }
    }
    
    // Log expired sessions
    if (expiredSessions.length > 0) {
        FiddlerObject.log("Expired sessions detected: " + expiredSessions.join(", "));
        
        // Mark as invalid
        for (var i = 0; i < expiredSessions.length; i++) {
            evonySessions[expiredSessions[i]].isValid = false;
        }
    }
}

/**
 * Save sessions to file
 */
function saveSessionsToFile() {
    try {
        var filePath = FiddlerObject.GetPath("Root") + "\\EvonySessions.json";
        var content = JSON.stringify(evonySessions, null, 2);
        FiddlerObject.WriteFile(filePath, content);
    } catch (e) {
        FiddlerObject.log("Error saving sessions: " + e.message);
    }
}

/**
 * Load sessions from file
 */
function loadSessionsFromFile() {
    try {
        var filePath = FiddlerObject.GetPath("Root") + "\\EvonySessions.json";
        var content = FiddlerObject.ReadFile(filePath);
        if (content) {
            evonySessions = JSON.parse(content);
            FiddlerObject.log("Loaded " + Object.keys(evonySessions).length + " sessions");
        }
    } catch (e) {
        FiddlerObject.log("Error loading sessions: " + e.message);
    }
}

/**
 * Process login response
 */
function processLoginResponse(session) {
    if (!session.oResponse) return;
    
    try {
        var body = session.GetResponseBodyAsString();
        if (!body) return;
        
        // Parse login response
        var data = JSON.parse(body);
        if (data && data.ok && data.data) {
            var server = extractServerFromUrl(session.fullUrl);
            var username = data.data.username || data.data.player?.name;
            var token = data.data.sessionToken || extractSessionToken(session);
            
            if (server && username && token) {
                storeSession(server, username, token, new Date().getTime());
            }
        }
    } catch (e) {
        // Ignore parse errors
    }
}

/**
 * Process logout response
 */
function processLogoutResponse(session) {
    try {
        var server = extractServerFromUrl(session.fullUrl);
        // Invalidate all sessions for this server
        for (var key in evonySessions) {
            if (evonySessions.hasOwnProperty(key) && key.startsWith(server + "_")) {
                evonySessions[key].isValid = false;
            }
        }
    } catch (e) {
        // Ignore errors
    }
}

/**
 * Extract server from URL
 */
function extractServerFromUrl(url) {
    var match = url.match(/https?:\/\/(cc\d+)\.evony\.com/);
    return match ? match[1] : null;
}

/**
 * Inject session token into request
 */
function injectSessionToken(session) {
    var server = extractServerFromUrl(session.fullUrl);
    if (!server) return;
    
    // Find valid session for this server
    for (var key in evonySessions) {
        if (evonySessions.hasOwnProperty(key) && key.startsWith(server + "_")) {
            var storedSession = evonySessions[key];
            if (storedSession.isValid && !isSessionExpired(storedSession)) {
                // Inject token
                session.oRequest.headers["X-Evony-Session"] = storedSession.token;
                updateSessionActivity(storedSession.server, storedSession.username);
                return;
            }
        }
    }
}

/**
 * Get session summary
 */
function getSessionSummary() {
    var summary = {
        total: 0,
        valid: 0,
        expired: 0,
        sessions: []
    };
    
    for (var key in evonySessions) {
        if (evonySessions.hasOwnProperty(key)) {
            summary.total++;
            var session = evonySessions[key];
            
            if (session.isValid && !isSessionExpired(session)) {
                summary.valid++;
            } else {
                summary.expired++;
            }
            
            summary.sessions.push({
                key: key,
                server: session.server,
                username: session.username,
                isValid: session.isValid,
                isExpired: isSessionExpired(session),
                lastActivity: new Date(session.lastActivity).toISOString()
            });
        }
    }
    
    return summary;
}

// Initialize on load
loadSessionsFromFile();
initSessionManager();

// Export functions for FiddlerScript
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        extractSessionToken: extractSessionToken,
        storeSession: storeSession,
        getSession: getSession,
        invalidateSession: invalidateSession,
        isSessionExpired: isSessionExpired,
        processLoginResponse: processLoginResponse,
        processLogoutResponse: processLogoutResponse,
        injectSessionToken: injectSessionToken,
        getSessionSummary: getSessionSummary
    };
}
