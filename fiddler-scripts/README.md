# Evony Fiddler Scripts

This directory contains FiddlerScript files for capturing, analyzing, and manipulating Evony game traffic.

## Scripts Overview

| Script | Description |
|--------|-------------|
| `EvonyTrafficCapture.js` | Main traffic capture and logging script |
| `EvonyAMFDecoder.js` | AMF3 binary protocol decoder |
| `EvonyRequestModifier.js` | Request modification and injection |
| `EvonyAutoResponder.js` | Automated response generation |
| `EvonySessionManager.js` | Session token management |
| `EvonyPerformanceMonitor.js` | Latency and throughput monitoring |

## Installation

### Method 1: Copy to Fiddler Scripts folder

1. Locate your Fiddler Scripts folder:
   ```
   %USERPROFILE%\Documents\Fiddler2\Scripts\
   ```

2. Copy the desired script files to this folder

3. Restart Fiddler

### Method 2: Import via Fiddler

1. Open Fiddler
2. Go to **Rules > Customize Rules...**
3. Copy the script content into the appropriate section
4. Save and close

## Script Details

### EvonyTrafficCapture.js

Main traffic capture script with features:
- Evony-only traffic filtering
- Packet type detection and highlighting
- Auto-logging to timestamped files
- SWF file extraction
- Bot detection

**Configuration:**
```javascript
var config = {
    targetDomain: "cc2.evony.com",
    enableLogging: true,
    logPath: "C:\\EvonyCaptures\\",
    extractSwf: true
};
```

### EvonyAMFDecoder.js

Decodes AMF3 binary protocol data:
- Automatic AMF3 detection
- JSON output formatting
- Nested object support
- Array handling

**Usage:**
```javascript
var decoded = decodeAMF3(hexData);
console.log(JSON.stringify(decoded, null, 2));
```

### EvonyRequestModifier.js

Modifies outgoing requests:
- Parameter injection
- Header modification
- Request body transformation
- Timing manipulation

**Example:**
```javascript
// Modify march speed
modifyRequest("march.startMarch", {
    speedMultiplier: 2.0
});
```

### EvonyAutoResponder.js

Generates automated responses:
- Mock server responses
- Testing scenarios
- Offline mode support
- Response templates

**Configuration:**
```javascript
var autoResponses = {
    "hero.getHeroList": {
        "ok": true,
        "data": { "heroes": [] }
    }
};
```

### EvonySessionManager.js

Manages session tokens:
- Token extraction and storage
- Auto-reconnect on expiry
- Multi-account support
- Session health monitoring

**Features:**
- Automatic session persistence
- Expiry detection
- Token injection

### EvonyPerformanceMonitor.js

Monitors performance metrics:
- Request/response latency
- Throughput tracking
- Error rate monitoring
- Performance alerts

**Metrics:**
- Average latency
- P50/P90/P95/P99 percentiles
- Requests per second
- Error rate percentage

## Packet Types

The scripts detect and highlight these packet types:

| Type | Color | Description |
|------|-------|-------------|
| LOGIN | Green | Authentication requests |
| MARCH | Orange | Troop movements |
| BATTLE | Red | Combat actions |
| SWF | Blue | Flash files |
| CHAT | Yellow | Chat messages |
| BUILD | Purple | Building operations |
| RESEARCH | Pink | Technology research |
| AMF | Amber | Binary AMF packets |

## Logging

Traffic logs are saved to:
```
%USERPROFILE%\Documents\Fiddler2\EvonyLogs\
```

Log format:
```
[2025-01-11 10:30:45] POST https://cc2.evony.com/amf/gateway.php
Type: AMF | Size: 1234 bytes | Latency: 150ms
Action: hero.hireHero
Request: { "heroId": 123, "cityId": 456 }
Response: { "ok": true, "data": { ... } }
---
```

## Integration with Svony Browser

These scripts integrate with the Svony Browser application:

1. **Named Pipe Communication**: Scripts send decoded traffic to the browser via named pipes
2. **Real-time Display**: Traffic appears in the Traffic Viewer panel
3. **Protocol Lookup**: Click on traffic to see protocol documentation
4. **Export**: Export captured traffic for analysis

## Troubleshooting

### Scripts not loading
- Check that Fiddler has permission to run scripts
- Verify script syntax is correct
- Look for errors in Fiddler's Log tab

### Traffic not captured
- Ensure proxy is configured correctly
- Check that HTTPS decryption is enabled
- Verify target domain filter

### AMF decoding fails
- Some packets may use custom encoding
- Check for compression (gzip/deflate)
- Verify packet boundaries

## License

MIT License - See LICENSE file for details.
