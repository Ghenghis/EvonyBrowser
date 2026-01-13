/**
 * Evony Performance Monitor FiddlerScript
 * Tracks latency, throughput, and performance metrics
 * 
 * Features:
 * - Request/response latency tracking
 * - Throughput monitoring
 * - Error rate tracking
 * - Performance alerts
 */

// Performance metrics storage
var performanceMetrics = {
    requests: [],
    errors: [],
    latencySum: 0,
    requestCount: 0,
    errorCount: 0,
    startTime: new Date().getTime(),
    lastReportTime: new Date().getTime()
};

// Configuration
var config = {
    maxStoredRequests: 1000,
    reportInterval: 60000, // 1 minute
    latencyThreshold: 2000, // 2 seconds
    errorRateThreshold: 0.1 // 10%
};

/**
 * Initialize performance monitor
 */
function initPerformanceMonitor() {
    FiddlerObject.log("Evony Performance Monitor initialized");
    startPeriodicReport();
}

/**
 * Record request start
 */
function recordRequestStart(session) {
    if (!isEvonyRequest(session)) return;
    
    session["X-Evony-StartTime"] = new Date().getTime();
}

/**
 * Record request completion
 */
function recordRequestComplete(session) {
    if (!isEvonyRequest(session)) return;
    
    var startTime = session["X-Evony-StartTime"];
    if (!startTime) return;
    
    var endTime = new Date().getTime();
    var latency = endTime - startTime;
    
    // Extract request info
    var requestInfo = {
        url: session.fullUrl,
        method: session.oRequest.headers.HTTPMethod,
        action: extractActionFromUrl(session.fullUrl),
        latency: latency,
        statusCode: session.oResponse ? session.oResponse.headers.HTTPResponseCode : 0,
        timestamp: endTime,
        size: session.oResponse ? session.oResponse.headers["Content-Length"] : 0
    };
    
    // Store request
    performanceMetrics.requests.push(requestInfo);
    performanceMetrics.latencySum += latency;
    performanceMetrics.requestCount++;
    
    // Check for errors
    if (requestInfo.statusCode >= 400 || requestInfo.statusCode === 0) {
        performanceMetrics.errors.push(requestInfo);
        performanceMetrics.errorCount++;
    }
    
    // Trim old requests
    if (performanceMetrics.requests.length > config.maxStoredRequests) {
        performanceMetrics.requests.shift();
    }
    if (performanceMetrics.errors.length > config.maxStoredRequests / 10) {
        performanceMetrics.errors.shift();
    }
    
    // Check for high latency
    if (latency > config.latencyThreshold) {
        FiddlerObject.log("High latency detected: " + latency + "ms for " + requestInfo.action);
    }
}

/**
 * Check if request is to Evony
 */
function isEvonyRequest(session) {
    return session.fullUrl && session.fullUrl.indexOf("evony.com") !== -1;
}

/**
 * Extract action from URL
 */
function extractActionFromUrl(url) {
    var match = url.match(/\/([^\/\?]+)(?:\?|$)/);
    return match ? match[1] : "unknown";
}

/**
 * Get current performance summary
 */
function getPerformanceSummary() {
    var now = new Date().getTime();
    var duration = (now - performanceMetrics.startTime) / 1000; // seconds
    
    var avgLatency = performanceMetrics.requestCount > 0 
        ? performanceMetrics.latencySum / performanceMetrics.requestCount 
        : 0;
    
    var errorRate = performanceMetrics.requestCount > 0 
        ? performanceMetrics.errorCount / performanceMetrics.requestCount 
        : 0;
    
    var throughput = duration > 0 
        ? performanceMetrics.requestCount / duration 
        : 0;
    
    return {
        duration: Math.round(duration),
        totalRequests: performanceMetrics.requestCount,
        totalErrors: performanceMetrics.errorCount,
        averageLatency: Math.round(avgLatency),
        errorRate: (errorRate * 100).toFixed(2) + "%",
        throughput: throughput.toFixed(2) + " req/s",
        recentRequests: performanceMetrics.requests.slice(-10),
        recentErrors: performanceMetrics.errors.slice(-5)
    };
}

/**
 * Get latency percentiles
 */
function getLatencyPercentiles() {
    if (performanceMetrics.requests.length === 0) {
        return { p50: 0, p90: 0, p95: 0, p99: 0 };
    }
    
    var latencies = performanceMetrics.requests.map(function(r) { return r.latency; });
    latencies.sort(function(a, b) { return a - b; });
    
    var p50Index = Math.floor(latencies.length * 0.5);
    var p90Index = Math.floor(latencies.length * 0.9);
    var p95Index = Math.floor(latencies.length * 0.95);
    var p99Index = Math.floor(latencies.length * 0.99);
    
    return {
        p50: latencies[p50Index] || 0,
        p90: latencies[p90Index] || 0,
        p95: latencies[p95Index] || 0,
        p99: latencies[p99Index] || 0
    };
}

/**
 * Get action-specific metrics
 */
function getActionMetrics() {
    var actionMetrics = {};
    
    performanceMetrics.requests.forEach(function(request) {
        var action = request.action;
        if (!actionMetrics[action]) {
            actionMetrics[action] = {
                count: 0,
                totalLatency: 0,
                errors: 0
            };
        }
        
        actionMetrics[action].count++;
        actionMetrics[action].totalLatency += request.latency;
        
        if (request.statusCode >= 400) {
            actionMetrics[action].errors++;
        }
    });
    
    // Calculate averages
    var result = [];
    for (var action in actionMetrics) {
        if (actionMetrics.hasOwnProperty(action)) {
            var metrics = actionMetrics[action];
            result.push({
                action: action,
                count: metrics.count,
                avgLatency: Math.round(metrics.totalLatency / metrics.count),
                errorRate: ((metrics.errors / metrics.count) * 100).toFixed(2) + "%"
            });
        }
    }
    
    // Sort by count descending
    result.sort(function(a, b) { return b.count - a.count; });
    
    return result;
}

/**
 * Start periodic reporting
 */
function startPeriodicReport() {
    setInterval(function() {
        var summary = getPerformanceSummary();
        var percentiles = getLatencyPercentiles();
        
        FiddlerObject.log("=== Performance Report ===");
        FiddlerObject.log("Requests: " + summary.totalRequests + " | Errors: " + summary.totalErrors);
        FiddlerObject.log("Avg Latency: " + summary.averageLatency + "ms | Throughput: " + summary.throughput);
        FiddlerObject.log("Latency P50: " + percentiles.p50 + "ms | P95: " + percentiles.p95 + "ms | P99: " + percentiles.p99 + "ms");
        
        // Check for alerts
        var errorRate = parseFloat(summary.errorRate);
        if (errorRate > config.errorRateThreshold * 100) {
            FiddlerObject.log("ALERT: High error rate detected: " + summary.errorRate);
        }
        
        if (percentiles.p95 > config.latencyThreshold) {
            FiddlerObject.log("ALERT: High P95 latency: " + percentiles.p95 + "ms");
        }
        
        performanceMetrics.lastReportTime = new Date().getTime();
    }, config.reportInterval);
}

/**
 * Reset metrics
 */
function resetMetrics() {
    performanceMetrics = {
        requests: [],
        errors: [],
        latencySum: 0,
        requestCount: 0,
        errorCount: 0,
        startTime: new Date().getTime(),
        lastReportTime: new Date().getTime()
    };
    
    FiddlerObject.log("Performance metrics reset");
}

/**
 * Export metrics to JSON
 */
function exportMetrics() {
    var data = {
        summary: getPerformanceSummary(),
        percentiles: getLatencyPercentiles(),
        actionMetrics: getActionMetrics(),
        exportTime: new Date().toISOString()
    };
    
    try {
        var filePath = FiddlerObject.GetPath("Root") + "\\EvonyPerformanceMetrics.json";
        var content = JSON.stringify(data, null, 2);
        FiddlerObject.WriteFile(filePath, content);
        FiddlerObject.log("Metrics exported to: " + filePath);
    } catch (e) {
        FiddlerObject.log("Error exporting metrics: " + e.message);
    }
    
    return data;
}

// Initialize on load
initPerformanceMonitor();

// Export functions
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        recordRequestStart: recordRequestStart,
        recordRequestComplete: recordRequestComplete,
        getPerformanceSummary: getPerformanceSummary,
        getLatencyPercentiles: getLatencyPercentiles,
        getActionMetrics: getActionMetrics,
        resetMetrics: resetMetrics,
        exportMetrics: exportMetrics
    };
}
