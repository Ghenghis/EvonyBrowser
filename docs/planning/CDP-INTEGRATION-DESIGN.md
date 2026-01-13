# CefFlashBrowser CDP Integration Design

## Overview

This document describes the architecture for integrating CefFlashBrowser automation with Svony Browser using Chrome DevTools Protocol (CDP), enabling full control from Playwright, Claude Desktop, and Windsurf IDE.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Control Layer                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐             │
│  │  Claude Desktop │  │  Windsurf IDE   │  │   LM Studio     │             │
│  │  (MCP Client)   │  │  (Scripts)      │  │   (MCP Client)  │             │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘             │
└───────────┼─────────────────────┼─────────────────────┼─────────────────────┘
            │                     │                     │
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MCP Server Layer                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    evony-cdp MCP Server                              │   │
│  │  • CDP connection management                                         │   │
│  │  • Playwright integration                                            │   │
│  │  • Visual automation tools                                           │   │
│  │  • Coordinate-based clicking                                         │   │
│  │  • Screenshot analysis                                               │   │
│  └────────────────────────────────┬────────────────────────────────────┘   │
└───────────────────────────────────┼─────────────────────────────────────────┘
                                    │ WebSocket (CDP)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Browser Layer                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    CefFlashBrowser                                   │   │
│  │  --remote-debugging-port=9222                                        │   │
│  │  --remote-allow-origins=*                                            │   │
│  │                                                                       │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │                    Flash Player (SWF)                        │    │   │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │    │   │
│  │  │  │  Game UI    │  │  AMF3 Comm  │  │  Graphics   │         │    │   │
│  │  │  │  Elements   │  │  Layer      │  │  Renderer   │         │    │   │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘         │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Game Server                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    cc2.evony.com                                     │   │
│  │  • AMF3 Protocol                                                     │   │
│  │  • Game State                                                        │   │
│  │  • Actions                                                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. CefFlashBrowser Launcher

Launches CefFlashBrowser with CDP enabled:

```powershell
.\CefFlashBrowser.exe --remote-debugging-port=9222 --remote-allow-origins=*
```

### 2. CDP Connection Manager

Manages WebSocket connections to the CDP endpoint:

- Connect to `http://localhost:9222`
- Handle reconnection on disconnect
- Manage multiple browser contexts
- Track page lifecycle

### 3. Playwright CDP Bridge

Connects Playwright to the running CefFlashBrowser:

```typescript
const browser = await chromium.connectOverCDP('http://localhost:9222');
const context = browser.contexts()[0];
const page = context.pages()[0];
```

### 4. Visual Automation Engine

Since Flash content is a "black box" to DOM selectors:

- **Coordinate-based clicking**: `page.mouse.click(x, y)`
- **Screenshot analysis**: Capture and analyze screenshots
- **Template matching**: Find UI elements by image
- **OCR integration**: Read text from screenshots

### 5. UI Element Map

Pre-defined coordinates for common Evony UI elements:

| Element    | X   | Y   | Description      |
| ---------- | --- | --- | ---------------- |
| City View  | 100 | 50  | Main city button |
| World Map  | 200 | 50  | World map button |
| Alliance   | 300 | 50  | Alliance button  |
| Build Menu | 150 | 600 | Building menu    |
| Research   | 250 | 600 | Research menu    |
| Heroes     | 350 | 600 | Heroes menu      |
| March      | 450 | 600 | March button     |
| ...        | ... | ... | ...              |

## MCP Tools for CDP Control

### Browser Connection Tools

| Tool             | Description                        |
| ---------------- | ---------------------------------- |
| `cdp_connect`    | Connect to CefFlashBrowser via CDP |
| `cdp_disconnect` | Disconnect from CDP                |
| `cdp_status`     | Get CDP connection status          |
| `cdp_launch`     | Launch CefFlashBrowser with CDP    |

### Navigation Tools

| Tool           | Description     |
| -------------- | --------------- |
| `cdp_navigate` | Navigate to URL |
| `cdp_reload`   | Reload page     |
| `cdp_back`     | Go back         |
| `cdp_forward`  | Go forward      |

### Visual Automation Tools

| Tool               | Description                    |
| ------------------ | ------------------------------ |
| `cdp_click`        | Click at coordinates           |
| `cdp_double_click` | Double-click at coordinates    |
| `cdp_right_click`  | Right-click at coordinates     |
| `cdp_drag`         | Drag from one point to another |
| `cdp_scroll`       | Scroll at coordinates          |
| `cdp_type`         | Type text                      |
| `cdp_key`          | Press key                      |

### Screenshot Tools

| Tool                 | Description                    |
| -------------------- | ------------------------------ |
| `cdp_screenshot`     | Take screenshot                |
| `cdp_find_element`   | Find element by template image |
| `cdp_ocr`            | Extract text from region       |
| `cdp_wait_for_image` | Wait for image to appear       |

### Game-Specific Tools

| Tool                      | Description            |
| ------------------------- | ---------------------- |
| `evony_click_city`        | Click city view        |
| `evony_click_world`       | Click world map        |
| `evony_click_building`    | Click building by name |
| `evony_upgrade_building`  | Upgrade building       |
| `evony_train_troops`      | Train troops           |
| `evony_send_march`        | Send march             |
| `evony_collect_resources` | Collect resources      |

## Visual Automation Strategies

### 1. Coordinate-Based Clicking

For static UI elements with known positions:

```typescript
// Click the "Build" button at fixed coordinates
await page.mouse.click(150, 600);
```

### 2. Relative Positioning

For elements relative to screen size:

```typescript
const viewport = page.viewportSize();
const x = viewport.width * 0.5;  // 50% from left
const y = viewport.height * 0.8; // 80% from top
await page.mouse.click(x, y);
```

### 3. Template Matching

For dynamic UI elements:

```typescript
// Find button by template image
const location = await findTemplate(screenshot, 'build_button.png');
if (location) {
  await page.mouse.click(location.x, location.y);
}
```

### 4. OCR-Based Detection

For text-based elements:

```typescript
// Find and click text
const textLocation = await findText(screenshot, 'Upgrade');
if (textLocation) {
  await page.mouse.click(textLocation.x, textLocation.y);
}
```

## Configuration

### CefFlashBrowser Launch Config

```json
{
  "executable": "C:\\Path\\To\\CefFlashBrowser.exe",
  "args": [
    "--remote-debugging-port=9222",
    "--remote-allow-origins=*",
    "--disable-gpu-sandbox",
    "--enable-logging"
  ],
  "startUrl": "https://cc2.evony.com",
  "windowSize": {
    "width": 1920,
    "height": 1080
  }
}
```

### CDP Connection Config

```json
{
  "cdpEndpoint": "http://localhost:9222",
  "reconnectInterval": 5000,
  "maxReconnectAttempts": 10,
  "timeout": 30000
}
```

### Visual Automation Config

```json
{
  "screenshotFormat": "png",
  "templateMatchThreshold": 0.8,
  "ocrLanguage": "eng",
  "clickDelay": 100,
  "typeDelay": 50
}
```

## Integration with Existing Svony Browser

### Dual Mode Operation

1. **CefSharp Mode**: Standard browser panels with Flash support (current implementation)
2. **CDP Mode**: CefFlashBrowser automation (new)

### Shared Services

- Protocol database
- Packet analysis
- Game state tracking
- Combat simulation
- Map intelligence

### Data Flow

```
CefFlashBrowser (CDP) ──► Screenshot ──► Visual Analysis ──► Game State
                      ──► Network    ──► Fiddler       ──► Packet Analysis
```

## Limitations

| Limitation             | Mitigation                                    |
| ---------------------- | --------------------------------------------- |
| Flash is a "black box" | Use visual automation                         |
| No DOM selectors       | Use coordinate-based clicking                 |
| Cannot run headless    | Keep window visible                           |
| Coordinates may shift  | Use relative positioning or template matching |
| Performance overhead   | Optimize screenshot frequency                 |

## Security Considerations

- CDP endpoint is local only (localhost:9222)
- `--remote-allow-origins=*` allows any WebSocket connection
- Consider firewall rules to block external access
- Credentials should not be stored in automation scripts

## Implementation Phases

### Phase 1: CDP Connection
- CefFlashBrowser launcher
- CDP connection manager
- Basic navigation

### Phase 2: Visual Automation
- Screenshot capture
- Coordinate-based clicking
- Template matching

### Phase 3: Game Integration
- UI element map
- Game-specific tools
- State synchronization

### Phase 4: MCP Integration
- evony-cdp MCP server
- Claude Desktop config
- Windsurf integration
