# CefFlashBrowser CDP Integration Guide

## Overview

This guide explains how to automate the Flash-based Evony game using CefFlashBrowser and Chrome DevTools Protocol (CDP). This integration enables Claude Desktop, Windsurf IDE, and LM Studio to control the game through coordinate-based and visual automation.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     AI Control Layer                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │   Claude    │  │  Windsurf   │  │  LM Studio  │              │
│  │   Desktop   │  │    IDE      │  │  (RTX 3090) │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
└─────────┼────────────────┼────────────────┼─────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    evony-cdp MCP Server                          │
│                    (35+ automation tools)                        │
└───────────────────────────┬─────────────────────────────────────┘
                            │ Playwright / CDP
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CefFlashBrowser                               │
│                    --remote-debugging-port=9222                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   Flash Player (SWF)                     │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │    │
│  │  │  Game UI    │  │  AMF3 Comm  │  │  Graphics   │      │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘      │    │
│  └─────────────────────────────────────────────────────────┘    │
└───────────────────────────┬─────────────────────────────────────┘
                            │ AMF3 Protocol
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    cc2.evony.com                                 │
│                    (Game Server)                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Quick Start

### Step 1: Launch CefFlashBrowser with CDP

```powershell
# Windows PowerShell
.\CefFlashBrowser.exe --remote-debugging-port=9222 --remote-allow-origins=* https://cc2.evony.com
```

**Important flags:**
- `--remote-debugging-port=9222`: Opens the CDP control port
- `--remote-allow-origins=*`: Allows WebSocket connections from external tools

### Step 2: Connect from Claude Desktop

1. Copy `cli/claude-desktop-cdp-config.json` to your Claude Desktop config directory
2. Restart Claude Desktop
3. Say: "Connect to the browser on port 9222"

### Step 3: Automate the Game

```
You: "Go to city view and upgrade the barracks"

Claude: I'll help you upgrade the barracks. Let me:
1. Connect to the browser
2. Navigate to city view
3. Click on the barracks
4. Click the upgrade button
5. Confirm the upgrade

[Executes cdp_connect, evony_go_to_city, evony_upgrade_building]

Done! The barracks upgrade has been initiated.
```

## Available Tools (35+)

### Connection Tools

| Tool | Description |
|------|-------------|
| `cdp_launch` | Launch CefFlashBrowser with CDP enabled |
| `cdp_connect` | Connect to running browser |
| `cdp_disconnect` | Disconnect from browser |
| `cdp_status` | Get connection status |

### Navigation Tools

| Tool | Description |
|------|-------------|
| `cdp_navigate` | Navigate to URL |
| `cdp_reload` | Reload page |
| `cdp_back` | Go back in history |
| `cdp_forward` | Go forward in history |
| `cdp_get_url` | Get current URL and title |

### Click Tools

| Tool | Description |
|------|-------------|
| `cdp_click` | Click at coordinates |
| `cdp_double_click` | Double-click at coordinates |
| `cdp_click_element` | Click named UI element |
| `cdp_click_relative` | Click at percentage position |

### Drag & Scroll Tools

| Tool | Description |
|------|-------------|
| `cdp_drag` | Drag from one point to another |
| `cdp_scroll` | Scroll at coordinates |
| `cdp_pan_map` | Pan world map in direction |

### Keyboard Tools

| Tool | Description |
|------|-------------|
| `cdp_type` | Type text |
| `cdp_press_key` | Press a key |

### Screenshot Tools

| Tool | Description |
|------|-------------|
| `cdp_screenshot` | Take full screenshot |
| `cdp_screenshot_region` | Screenshot a region |

### Game Action Tools

| Tool | Description |
|------|-------------|
| `evony_go_to_city` | Navigate to city view |
| `evony_go_to_world` | Navigate to world map |
| `evony_click_building` | Click on a building |
| `evony_upgrade_building` | Upgrade a building |
| `evony_train_troops` | Train troops |
| `evony_close_dialog` | Close current dialog |
| `evony_confirm` | Click confirm button |
| `evony_cancel` | Click cancel button |

### UI Map Tools

| Tool | Description |
|------|-------------|
| `cdp_list_elements` | List all known UI elements |
| `cdp_get_element` | Get element coordinates |
| `cdp_add_element` | Add custom element |

### Utility Tools

| Tool | Description |
|------|-------------|
| `cdp_wait` | Wait for duration |
| `cdp_execute_script` | Execute JavaScript |
| `cdp_get_viewport` | Get viewport size |
| `cdp_get_action_log` | Get recent actions |

## UI Element Map

Pre-defined coordinates for common Evony UI elements (based on 1920x1080):

### Navigation Bar

| Element | X | Y | Description |
|---------|---|---|-------------|
| cityView | 100 | 50 | City view button |
| worldMap | 200 | 50 | World map button |
| alliance | 300 | 50 | Alliance button |
| mail | 400 | 50 | Mail button |
| events | 500 | 50 | Events button |

### Bottom Menu

| Element | X | Y | Description |
|---------|---|---|-------------|
| buildMenu | 150 | 600 | Build menu |
| research | 250 | 600 | Research menu |
| heroes | 350 | 600 | Heroes menu |
| march | 450 | 600 | March panel |
| items | 550 | 600 | Items menu |

### Buildings

| Element | X | Y | Description |
|---------|---|---|-------------|
| castle | 960 | 400 | Main castle |
| barracks | 800 | 500 | Train infantry |
| stables | 1100 | 500 | Train cavalry |
| workshop | 700 | 400 | Train siege |
| academy | 1200 | 400 | Research |

### Dialog Buttons

| Element | X | Y | Description |
|---------|---|---|-------------|
| confirmButton | 960 | 600 | Confirm action |
| cancelButton | 860 | 600 | Cancel action |
| closeButton | 1100 | 200 | Close dialog |
| upgradeButton | 960 | 550 | Upgrade button |
| trainButton | 960 | 550 | Train button |

## Coordinate Scaling

Coordinates are automatically scaled based on viewport size:

```
scaledX = baseX * (viewportWidth / 1920)
scaledY = baseY * (viewportHeight / 1080)
```

This ensures automation works on different screen resolutions.

## Visual Automation Strategies

### 1. Named Elements (Recommended)

Use pre-defined UI elements:

```
cdp_click_element { element: "cityView" }
```

### 2. Absolute Coordinates

Click at specific pixel position:

```
cdp_click { x: 500, y: 300 }
```

### 3. Relative Coordinates

Click at percentage of viewport:

```
cdp_click_relative { xPercent: 0.5, yPercent: 0.8 }
```

### 4. Finding New Coordinates

1. Take a screenshot: `cdp_screenshot`
2. Analyze the image to estimate coordinates
3. Test with `cdp_click`
4. Save with `cdp_add_element`

## Example Automation Scripts

### Upgrade Building

```javascript
// Connect to browser
await cdp_connect({ host: 'localhost', port: 9222 });

// Navigate to city
await evony_go_to_city();
await cdp_wait({ ms: 1000 });

// Click building
await cdp_click_element({ element: 'barracks' });
await cdp_wait({ ms: 500 });

// Click upgrade
await cdp_click_element({ element: 'upgradeButton' });
await cdp_wait({ ms: 500 });

// Confirm
await cdp_click_element({ element: 'confirmButton' });
```

### Train Troops

```javascript
// Use the high-level tool
await evony_train_troops({
  troopType: 'cavalry',
  quantity: 1000
});
```

### Pan World Map

```javascript
// Pan map to explore
await evony_go_to_world();
await cdp_wait({ ms: 1000 });

await cdp_pan_map({ direction: 'right', distance: 300 });
await cdp_wait({ ms: 500 });

await cdp_pan_map({ direction: 'up', distance: 200 });
```

## Playwright Integration

For advanced automation, use Playwright directly:

```typescript
import { chromium } from 'playwright';

async function main() {
  // Connect to CefFlashBrowser
  const browser = await chromium.connectOverCDP('http://localhost:9222');
  const context = browser.contexts()[0];
  const page = context.pages()[0];

  // Navigate
  await page.goto('https://cc2.evony.com');

  // Click at coordinates (Flash content)
  await page.mouse.click(100, 50); // City view

  // Take screenshot
  await page.screenshot({ path: 'screenshot.png' });

  // Drag to pan map
  await page.mouse.move(960, 540);
  await page.mouse.down();
  await page.mouse.move(760, 540, { steps: 10 });
  await page.mouse.up();
}

main();
```

## IDE Configuration

### Claude Desktop

Copy `cli/claude-desktop-cdp-config.json` to:
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

### Windsurf IDE

Copy `cli/windsurf-cdp-config.json` to `.windsurf/mcp.json` in your workspace.

### LM Studio

Copy `cli/lm-studio-cdp-config.json` to LM Studio's config directory.

## Limitations

| Limitation | Mitigation |
|------------|------------|
| Flash is a "black box" | Use coordinate-based clicking |
| No DOM selectors | Use pre-defined UI element map |
| Cannot run headless | Keep browser window visible |
| Coordinates may shift | Use relative positioning or scaling |
| Performance overhead | Optimize screenshot frequency |

## Troubleshooting

### "CDP endpoint not ready"

- Ensure CefFlashBrowser is launched with `--remote-debugging-port=9222`
- Check if port 9222 is available
- Wait a few seconds for browser to initialize

### "Not connected to browser"

- Use `cdp_connect` before other tools
- Check `cdp_status` for connection state

### "Unknown element"

- Use `cdp_list_elements` to see available elements
- Add custom elements with `cdp_add_element`

### Clicks not working

- Take a screenshot to verify game state
- Check if coordinates need adjustment
- Use `cdp_wait` between actions

## Files Created

| File | Description |
|------|-------------|
| `SvonyBrowser/Services/CdpConnectionService.cs` | C# CDP connection service |
| `SvonyBrowser/Services/VisualAutomationService.cs` | C# visual automation service |
| `cli/playwright-cdp/index.js` | Playwright CDP bridge |
| `mcp-servers/evony-cdp/index.js` | CDP MCP server (35+ tools) |
| `cli/claude-desktop-cdp-config.json` | Claude Desktop configuration |
| `cli/windsurf-cdp-config.json` | Windsurf IDE configuration |
| `cli/lm-studio-cdp-config.json` | LM Studio configuration |

## Version History

- **v1.0.0**: Initial CDP integration with 35+ tools
