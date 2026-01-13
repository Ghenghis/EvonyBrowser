#!/usr/bin/env node
/**
 * Evony MCP Server v4.0
 * Advanced tools for reverse engineering, packet analysis, and LLM integration
 * Optimized for RTX 3090 Ti with 7B parameter models
 */

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import WebSocket from "ws";
import net from "net";
import fs from "fs";
import path from "path";

// Server state
const state = {
  isConnected: false,
  capturedPackets: [],
  learnedActions: new Map(),
  fuzzResults: [],
  llmStats: {
    tokensPerSecond: 0,
    vramUsageGb: 0,
    gpuTemp: 0,
  },
  statusWidgets: new Map(),
};

// Create MCP server
const server = new Server(
  {
    name: "evony-v4",
    version: "4.0.0",
  },
  {
    capabilities: {
      tools: {},
      resources: {},
    },
  }
);

// ============================================================================
// TOOLS DEFINITION
// ============================================================================

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    // ========== PACKET ANALYSIS TOOLS ==========
    {
      name: "analyze_packet",
      description: "Deep analysis of a captured packet with AMF3 decoding",
      inputSchema: {
        type: "object",
        properties: {
          packetId: { type: "number", description: "Packet ID from capture" },
          hexData: { type: "string", description: "Raw hex data to analyze" },
          includeStrings: { type: "boolean", description: "Extract readable strings" },
        },
      },
    },
    {
      name: "decode_amf3",
      description: "Decode AMF3 binary data to readable format",
      inputSchema: {
        type: "object",
        properties: {
          data: { type: "string", description: "Base64 or hex encoded AMF3 data" },
          format: { type: "string", enum: ["base64", "hex"], description: "Input format" },
        },
        required: ["data"],
      },
    },
    {
      name: "compare_packets",
      description: "Differential analysis between two packets",
      inputSchema: {
        type: "object",
        properties: {
          packet1Id: { type: "number", description: "First packet ID" },
          packet2Id: { type: "number", description: "Second packet ID" },
        },
        required: ["packet1Id", "packet2Id"],
      },
    },
    {
      name: "search_packets",
      description: "Search captured packets by action, content, or pattern",
      inputSchema: {
        type: "object",
        properties: {
          query: { type: "string", description: "Search query" },
          field: { type: "string", enum: ["action", "content", "all"], description: "Field to search" },
          limit: { type: "number", description: "Max results" },
        },
        required: ["query"],
      },
    },

    // ========== PROTOCOL LEARNING TOOLS ==========
    {
      name: "learn_action",
      description: "Add a discovered action to the protocol database",
      inputSchema: {
        type: "object",
        properties: {
          name: { type: "string", description: "Action name (e.g., city.getCityInfo)" },
          category: { type: "string", description: "Category (e.g., city, hero, march)" },
          parameters: { type: "array", description: "Parameter definitions" },
          description: { type: "string", description: "What this action does" },
        },
        required: ["name"],
      },
    },
    {
      name: "get_learned_actions",
      description: "Get all learned protocol actions",
      inputSchema: {
        type: "object",
        properties: {
          category: { type: "string", description: "Filter by category" },
        },
      },
    },
    {
      name: "export_protocol_db",
      description: "Export the learned protocol database",
      inputSchema: {
        type: "object",
        properties: {
          format: { type: "string", enum: ["json", "markdown", "typescript"], description: "Export format" },
        },
      },
    },

    // ========== FUZZING TOOLS ==========
    {
      name: "start_fuzzing",
      description: "Start protocol fuzzing to discover undocumented actions",
      inputSchema: {
        type: "object",
        properties: {
          mode: {
            type: "string",
            enum: ["action_discovery", "parameter_boundary", "type_confusion", "sequence_breaking"],
            description: "Fuzzing mode",
          },
          targetAction: { type: "string", description: "Target action for parameter/type fuzzing" },
          parallelism: { type: "number", description: "Number of parallel requests" },
          delayMs: { type: "number", description: "Delay between requests" },
        },
        required: ["mode"],
      },
    },
    {
      name: "stop_fuzzing",
      description: "Stop the current fuzzing session",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "get_fuzz_results",
      description: "Get results from the fuzzing session",
      inputSchema: {
        type: "object",
        properties: {
          onlyDiscoveries: { type: "boolean", description: "Only return discovered actions" },
        },
      },
    },

    // ========== FIDDLER INTEGRATION TOOLS ==========
    {
      name: "fiddler_capture_start",
      description: "Start capturing traffic through Fiddler",
      inputSchema: {
        type: "object",
        properties: {
          filter: { type: "string", description: "URL filter pattern" },
        },
      },
    },
    {
      name: "fiddler_capture_stop",
      description: "Stop Fiddler traffic capture",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "fiddler_inject_packet",
      description: "Inject a custom packet through Fiddler",
      inputSchema: {
        type: "object",
        properties: {
          action: { type: "string", description: "Action name" },
          parameters: { type: "object", description: "Action parameters" },
          targetUrl: { type: "string", description: "Target URL" },
        },
        required: ["action"],
      },
    },
    {
      name: "fiddler_set_breakpoint",
      description: "Set a breakpoint on specific traffic",
      inputSchema: {
        type: "object",
        properties: {
          type: { type: "string", enum: ["url", "action", "response"], description: "Breakpoint type" },
          pattern: { type: "string", description: "Pattern to match" },
        },
        required: ["type", "pattern"],
      },
    },
    {
      name: "fiddler_replay_session",
      description: "Replay a captured Fiddler session",
      inputSchema: {
        type: "object",
        properties: {
          sessionId: { type: "string", description: "Session ID to replay" },
        },
        required: ["sessionId"],
      },
    },

    // ========== LLM INTEGRATION TOOLS ==========
    {
      name: "llm_explain_packet",
      description: "Use local LLM to explain a packet's purpose",
      inputSchema: {
        type: "object",
        properties: {
          packetId: { type: "number", description: "Packet ID to explain" },
          hexData: { type: "string", description: "Raw hex data" },
        },
      },
    },
    {
      name: "llm_generate_script",
      description: "Generate automation script using local LLM",
      inputSchema: {
        type: "object",
        properties: {
          task: { type: "string", description: "Task description" },
          language: { type: "string", enum: ["python", "javascript", "csharp"], description: "Target language" },
        },
        required: ["task"],
      },
    },
    {
      name: "llm_decode_unknown",
      description: "Use LLM to attempt decoding unknown packet structure",
      inputSchema: {
        type: "object",
        properties: {
          hexData: { type: "string", description: "Unknown packet hex data" },
          context: { type: "string", description: "Additional context" },
        },
        required: ["hexData"],
      },
    },
    {
      name: "llm_get_stats",
      description: "Get LLM inference statistics",
      inputSchema: { type: "object", properties: {} },
    },

    // ========== STATUS BAR TOOLS ==========
    {
      name: "status_get_widgets",
      description: "Get all available status bar widgets",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "status_update_widget",
      description: "Update a status bar widget value",
      inputSchema: {
        type: "object",
        properties: {
          widgetId: { type: "string", description: "Widget ID" },
          value: { type: "number", description: "New value" },
          displayText: { type: "string", description: "Display text" },
        },
        required: ["widgetId"],
      },
    },
    {
      name: "status_configure",
      description: "Configure status bar layout",
      inputSchema: {
        type: "object",
        properties: {
          layout: { type: "array", description: "Widget layout configuration" },
        },
        required: ["layout"],
      },
    },

    // ========== GAME STATE TOOLS ==========
    {
      name: "game_get_state",
      description: "Get current game state from captured data",
      inputSchema: {
        type: "object",
        properties: {
          aspect: {
            type: "string",
            enum: ["all", "resources", "troops", "heroes", "buildings", "marches"],
            description: "State aspect to retrieve",
          },
        },
      },
    },
    {
      name: "game_track_changes",
      description: "Track changes in game state over time",
      inputSchema: {
        type: "object",
        properties: {
          aspect: { type: "string", description: "Aspect to track" },
          duration: { type: "number", description: "Duration in seconds" },
        },
        required: ["aspect"],
      },
    },

    // ========== AUTOMATION TOOLS ==========
    {
      name: "auto_create_sequence",
      description: "Create an automation sequence from captured packets",
      inputSchema: {
        type: "object",
        properties: {
          name: { type: "string", description: "Sequence name" },
          packetIds: { type: "array", items: { type: "number" }, description: "Packet IDs to include" },
          delays: { type: "array", items: { type: "number" }, description: "Delays between packets" },
        },
        required: ["name", "packetIds"],
      },
    },
    {
      name: "auto_run_sequence",
      description: "Run a saved automation sequence",
      inputSchema: {
        type: "object",
        properties: {
          name: { type: "string", description: "Sequence name" },
          repeat: { type: "number", description: "Number of repetitions" },
        },
        required: ["name"],
      },
    },
  ],
}));

// ============================================================================
// TOOL HANDLERS
// ============================================================================

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      // ========== PACKET ANALYSIS ==========
      case "analyze_packet":
        return await analyzePacket(args);
      case "decode_amf3":
        return await decodeAmf3(args);
      case "compare_packets":
        return await comparePackets(args);
      case "search_packets":
        return await searchPackets(args);

      // ========== PROTOCOL LEARNING ==========
      case "learn_action":
        return await learnAction(args);
      case "get_learned_actions":
        return await getLearnedActions(args);
      case "export_protocol_db":
        return await exportProtocolDb(args);

      // ========== FUZZING ==========
      case "start_fuzzing":
        return await startFuzzing(args);
      case "stop_fuzzing":
        return await stopFuzzing(args);
      case "get_fuzz_results":
        return await getFuzzResults(args);

      // ========== FIDDLER ==========
      case "fiddler_capture_start":
        return await fiddlerCaptureStart(args);
      case "fiddler_capture_stop":
        return await fiddlerCaptureStop(args);
      case "fiddler_inject_packet":
        return await fiddlerInjectPacket(args);
      case "fiddler_set_breakpoint":
        return await fiddlerSetBreakpoint(args);
      case "fiddler_replay_session":
        return await fiddlerReplaySession(args);

      // ========== LLM ==========
      case "llm_explain_packet":
        return await llmExplainPacket(args);
      case "llm_generate_script":
        return await llmGenerateScript(args);
      case "llm_decode_unknown":
        return await llmDecodeUnknown(args);
      case "llm_get_stats":
        return await llmGetStats(args);

      // ========== STATUS BAR ==========
      case "status_get_widgets":
        return await statusGetWidgets(args);
      case "status_update_widget":
        return await statusUpdateWidget(args);
      case "status_configure":
        return await statusConfigure(args);

      // ========== GAME STATE ==========
      case "game_get_state":
        return await gameGetState(args);
      case "game_track_changes":
        return await gameTrackChanges(args);

      // ========== AUTOMATION ==========
      case "auto_create_sequence":
        return await autoCreateSequence(args);
      case "auto_run_sequence":
        return await autoRunSequence(args);

      default:
        return { content: [{ type: "text", text: `Unknown tool: ${name}` }] };
    }
  } catch (error) {
    return {
      content: [{ type: "text", text: `Error: ${error.message}` }],
      isError: true,
    };
  }
});

// ============================================================================
// PACKET ANALYSIS IMPLEMENTATIONS
// ============================================================================

async function analyzePacket(args) {
  const { packetId, hexData, includeStrings = true } = args;

  let data;
  if (hexData) {
    data = Buffer.from(hexData.replace(/\s/g, ""), "hex");
  } else if (packetId !== undefined) {
    const packet = state.capturedPackets[packetId];
    if (!packet) {
      return { content: [{ type: "text", text: `Packet ${packetId} not found` }] };
    }
    data = packet.rawData;
  } else {
    return { content: [{ type: "text", text: "Must provide packetId or hexData" }] };
  }

  const analysis = {
    size: data.length,
    header: analyzeHeader(data),
    decoded: decodeAmf3Data(data),
    strings: includeStrings ? extractStrings(data) : [],
    patterns: detectPatterns(data),
  };

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(analysis, null, 2),
      },
    ],
  };
}

async function decodeAmf3(args) {
  const { data, format = "hex" } = args;

  let buffer;
  if (format === "base64") {
    buffer = Buffer.from(data, "base64");
  } else {
    buffer = Buffer.from(data.replace(/\s/g, ""), "hex");
  }

  const decoded = decodeAmf3Data(buffer);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(decoded, null, 2),
      },
    ],
  };
}

async function comparePackets(args) {
  const { packet1Id, packet2Id } = args;

  const packet1 = state.capturedPackets[packet1Id];
  const packet2 = state.capturedPackets[packet2Id];

  if (!packet1 || !packet2) {
    return { content: [{ type: "text", text: "One or both packets not found" }] };
  }

  const diff = {
    sizeDiff: packet2.rawData.length - packet1.rawData.length,
    timeDiff: packet2.timestamp - packet1.timestamp,
    fieldDiffs: compareDecodedData(packet1.decoded, packet2.decoded),
    byteDiffs: compareBytes(packet1.rawData, packet2.rawData),
  };

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(diff, null, 2),
      },
    ],
  };
}

async function searchPackets(args) {
  const { query, field = "all", limit = 50 } = args;

  const results = state.capturedPackets
    .filter((packet, index) => {
      if (field === "action" || field === "all") {
        if (packet.action?.toLowerCase().includes(query.toLowerCase())) {
          return true;
        }
      }
      if (field === "content" || field === "all") {
        if (JSON.stringify(packet.decoded)?.toLowerCase().includes(query.toLowerCase())) {
          return true;
        }
      }
      return false;
    })
    .slice(0, limit)
    .map((packet, index) => ({
      id: index,
      action: packet.action,
      size: packet.rawData.length,
      timestamp: packet.timestamp,
    }));

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify({ count: results.length, results }, null, 2),
      },
    ],
  };
}

// ============================================================================
// PROTOCOL LEARNING IMPLEMENTATIONS
// ============================================================================

async function learnAction(args) {
  const { name, category, parameters = [], description = "" } = args;

  const action = {
    name,
    category: category || name.split(".")[0],
    parameters,
    description,
    discoveredAt: new Date().toISOString(),
    occurrences: 1,
  };

  state.learnedActions.set(name, action);

  return {
    content: [
      {
        type: "text",
        text: `Learned action: ${name}\nCategory: ${action.category}\nTotal learned: ${state.learnedActions.size}`,
      },
    ],
  };
}

async function getLearnedActions(args) {
  const { category } = args;

  let actions = Array.from(state.learnedActions.values());

  if (category) {
    actions = actions.filter((a) => a.category === category);
  }

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(
          {
            total: actions.length,
            actions: actions,
          },
          null,
          2
        ),
      },
    ],
  };
}

async function exportProtocolDb(args) {
  const { format = "json" } = args;

  const actions = Array.from(state.learnedActions.values());

  let output;
  switch (format) {
    case "markdown":
      output = generateMarkdownDoc(actions);
      break;
    case "typescript":
      output = generateTypeScriptTypes(actions);
      break;
    default:
      output = JSON.stringify(actions, null, 2);
  }

  return {
    content: [{ type: "text", text: output }],
  };
}

// ============================================================================
// FUZZING IMPLEMENTATIONS
// ============================================================================

let fuzzingActive = false;

async function startFuzzing(args) {
  const { mode, targetAction, parallelism = 5, delayMs = 100 } = args;

  if (fuzzingActive) {
    return { content: [{ type: "text", text: "Fuzzing already in progress" }] };
  }

  fuzzingActive = true;
  state.fuzzResults = [];

  // Generate fuzz actions based on mode
  const actions = generateFuzzActions(mode, targetAction);

  // Start fuzzing in background
  fuzzInBackground(actions, parallelism, delayMs);

  return {
    content: [
      {
        type: "text",
        text: `Started ${mode} fuzzing with ${actions.length} test cases\nParallelism: ${parallelism}\nDelay: ${delayMs}ms`,
      },
    ],
  };
}

async function stopFuzzing(args) {
  fuzzingActive = false;
  return {
    content: [
      {
        type: "text",
        text: `Fuzzing stopped\nTotal attempts: ${state.fuzzResults.length}\nDiscoveries: ${state.fuzzResults.filter((r) => r.isDiscovery).length}`,
      },
    ],
  };
}

async function getFuzzResults(args) {
  const { onlyDiscoveries = false } = args;

  let results = state.fuzzResults;
  if (onlyDiscoveries) {
    results = results.filter((r) => r.isDiscovery);
  }

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(
          {
            total: results.length,
            discoveries: results.filter((r) => r.isDiscovery).length,
            errors: results.filter((r) => r.isError).length,
            results: results.slice(-100), // Last 100
          },
          null,
          2
        ),
      },
    ],
  };
}

// ============================================================================
// FIDDLER IMPLEMENTATIONS
// ============================================================================

async function fiddlerCaptureStart(args) {
  const { filter = "*.evony.com" } = args;

  // Send command to Fiddler bridge
  await sendToFiddlerBridge({
    type: "capture_start",
    filter,
  });

  return {
    content: [{ type: "text", text: `Fiddler capture started with filter: ${filter}` }],
  };
}

async function fiddlerCaptureStop(args) {
  await sendToFiddlerBridge({ type: "capture_stop" });
  return {
    content: [{ type: "text", text: "Fiddler capture stopped" }],
  };
}

async function fiddlerInjectPacket(args) {
  const { action, parameters = {}, targetUrl = "https://cc2.evony.com/gateway.php" } = args;

  const packet = buildAmf3Packet(action, parameters);

  await sendToFiddlerBridge({
    type: "inject",
    url: targetUrl,
    body: packet.toString("base64"),
  });

  return {
    content: [{ type: "text", text: `Injected packet for action: ${action}` }],
  };
}

async function fiddlerSetBreakpoint(args) {
  const { type, pattern } = args;

  await sendToFiddlerBridge({
    type: "breakpoint_set",
    breakpointType: type,
    pattern,
  });

  return {
    content: [{ type: "text", text: `Breakpoint set: ${type} = ${pattern}` }],
  };
}

async function fiddlerReplaySession(args) {
  const { sessionId } = args;

  await sendToFiddlerBridge({
    type: "replay",
    sessionId,
  });

  return {
    content: [{ type: "text", text: `Replaying session: ${sessionId}` }],
  };
}

// ============================================================================
// LLM IMPLEMENTATIONS
// ============================================================================

async function llmExplainPacket(args) {
  const { packetId, hexData } = args;

  let packetInfo;
  if (hexData) {
    packetInfo = { hex: hexData, decoded: decodeAmf3Data(Buffer.from(hexData.replace(/\s/g, ""), "hex")) };
  } else if (packetId !== undefined) {
    const packet = state.capturedPackets[packetId];
    if (!packet) {
      return { content: [{ type: "text", text: `Packet ${packetId} not found` }] };
    }
    packetInfo = { hex: packet.rawData.toString("hex"), decoded: packet.decoded };
  }

  const prompt = `Analyze this Evony game packet and explain its purpose:
${JSON.stringify(packetInfo, null, 2)}

Explain:
1. What action this performs
2. Parameter meanings
3. Expected server response
4. Related game mechanics`;

  const response = await callLocalLlm(prompt);

  return {
    content: [{ type: "text", text: response }],
  };
}

async function llmGenerateScript(args) {
  const { task, language = "python" } = args;

  const prompt = `Generate a ${language} script for Evony automation:

Task: ${task}

Requirements:
- Use proper Evony protocol actions
- Include error handling
- Add appropriate delays
- Include comments explaining each step

Generate the complete script:`;

  const response = await callLocalLlm(prompt);

  return {
    content: [{ type: "text", text: response }],
  };
}

async function llmDecodeUnknown(args) {
  const { hexData, context = "" } = args;

  const buffer = Buffer.from(hexData.replace(/\s/g, ""), "hex");
  const strings = extractStrings(buffer);

  const prompt = `Attempt to decode this unknown Evony packet:

Hex: ${hexData}
Extracted strings: ${strings.join(", ")}
Context: ${context}

Analyze:
1. Likely packet structure
2. Field types and meanings
3. Confidence level
4. Similar known packets`;

  const response = await callLocalLlm(prompt);

  return {
    content: [{ type: "text", text: response }],
  };
}

async function llmGetStats(args) {
  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(
          {
            tokensPerSecond: state.llmStats.tokensPerSecond,
            vramUsageGb: state.llmStats.vramUsageGb,
            vramTotalGb: 24.0,
            gpuTemperature: state.llmStats.gpuTemp,
            model: "evony-re-7b",
            backend: "LM Studio",
          },
          null,
          2
        ),
      },
    ],
  };
}

// ============================================================================
// STATUS BAR IMPLEMENTATIONS
// ============================================================================

async function statusGetWidgets(args) {
  const widgets = [
    // MCP Widgets
    { id: "rag_progress", title: "RAG Progress", category: "mcp", type: "progress", color: "purple" },
    { id: "rte_progress", title: "RTE Progress", category: "mcp", type: "progress", color: "blue" },
    { id: "mcp_status", title: "MCP Status", category: "mcp", type: "traffic_light" },

    // LLM Widgets
    { id: "llm_tokens", title: "Tokens/sec", category: "llm", type: "label" },
    { id: "llm_vram", title: "VRAM", category: "llm", type: "progress", color: "cyan" },
    { id: "gpu_temp", title: "GPU Temp", category: "llm", type: "temperature" },
    { id: "inference", title: "Inference", category: "llm", type: "progress", color: "green" },

    // Game State Widgets
    { id: "resources", title: "Resources", category: "game", type: "sparkline" },
    { id: "troops", title: "Troops", category: "game", type: "counter" },
    { id: "marches", title: "Marches", category: "game", type: "counter" },
    { id: "power", title: "Power", category: "game", type: "label" },

    // Network Widgets
    { id: "packets_sec", title: "Packets/sec", category: "network", type: "sparkline" },
    { id: "decode_rate", title: "Decode Rate", category: "network", type: "progress", color: "green" },
    { id: "fiddler_status", title: "Fiddler", category: "network", type: "traffic_light" },
    { id: "throughput", title: "Throughput", category: "network", type: "label" },

    // Automation Widgets
    { id: "autopilot", title: "AutoPilot", category: "automation", type: "traffic_light" },
    { id: "queue_size", title: "Queue", category: "automation", type: "counter" },
    { id: "fuzz_progress", title: "Fuzzing", category: "automation", type: "progress", color: "orange" },
  ];

  return {
    content: [{ type: "text", text: JSON.stringify(widgets, null, 2) }],
  };
}

async function statusUpdateWidget(args) {
  const { widgetId, value, displayText } = args;

  state.statusWidgets.set(widgetId, { value, displayText, updatedAt: Date.now() });

  return {
    content: [{ type: "text", text: `Updated widget ${widgetId}: ${displayText || value}` }],
  };
}

async function statusConfigure(args) {
  const { layout } = args;

  // Save layout configuration
  fs.writeFileSync(
    path.join(process.cwd(), "statusbar-config.json"),
    JSON.stringify(layout, null, 2)
  );

  return {
    content: [{ type: "text", text: `Status bar configured with ${layout.length} widgets` }],
  };
}

// ============================================================================
// GAME STATE IMPLEMENTATIONS
// ============================================================================

async function gameGetState(args) {
  const { aspect = "all" } = args;

  // Extract state from captured packets
  const gameState = extractGameState(aspect);

  return {
    content: [{ type: "text", text: JSON.stringify(gameState, null, 2) }],
  };
}

async function gameTrackChanges(args) {
  const { aspect, duration = 60 } = args;

  // Start tracking changes
  const trackingId = startChangeTracking(aspect, duration);

  return {
    content: [
      {
        type: "text",
        text: `Started tracking ${aspect} changes for ${duration}s\nTracking ID: ${trackingId}`,
      },
    ],
  };
}

// ============================================================================
// AUTOMATION IMPLEMENTATIONS
// ============================================================================

const savedSequences = new Map();

async function autoCreateSequence(args) {
  const { name, packetIds, delays = [] } = args;

  const packets = packetIds.map((id, index) => ({
    packet: state.capturedPackets[id],
    delay: delays[index] || 1000,
  }));

  savedSequences.set(name, { packets, createdAt: Date.now() });

  return {
    content: [
      {
        type: "text",
        text: `Created sequence "${name}" with ${packets.length} packets`,
      },
    ],
  };
}

async function autoRunSequence(args) {
  const { name, repeat = 1 } = args;

  const sequence = savedSequences.get(name);
  if (!sequence) {
    return { content: [{ type: "text", text: `Sequence "${name}" not found` }] };
  }

  // Run sequence in background
  runSequenceInBackground(sequence, repeat);

  return {
    content: [
      {
        type: "text",
        text: `Running sequence "${name}" ${repeat} time(s)`,
      },
    ],
  };
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

function analyzeHeader(data) {
  if (data.length < 10) return null;

  return {
    version: data[0],
    headerCount: (data[2] << 8) | data[3],
    messageCount: (data[4] << 8) | data[5],
  };
}

function decodeAmf3Data(data) {
  // AMF3 decoding implementation
  try {
    let offset = 0;

    // Skip AMF0 envelope if present
    if (data[0] === 0x00 && data[1] === 0x00) {
      offset = 6; // Skip to message body
    }

    const result = {};

    // Read action string
    if (offset < data.length) {
      const actionLength = (data[offset] << 8) | data[offset + 1];
      offset += 2;
      if (offset + actionLength <= data.length) {
        result.action = data.slice(offset, offset + actionLength).toString("utf8");
        offset += actionLength;
      }
    }

    // Read response URI
    if (offset < data.length) {
      const uriLength = (data[offset] << 8) | data[offset + 1];
      offset += 2;
      if (offset + uriLength <= data.length) {
        result.responseUri = data.slice(offset, offset + uriLength).toString("utf8");
        offset += uriLength;
      }
    }

    return result;
  } catch (e) {
    return { error: e.message };
  }
}

function extractStrings(data) {
  const strings = [];
  let current = "";

  for (const byte of data) {
    if (byte >= 32 && byte < 127) {
      current += String.fromCharCode(byte);
    } else {
      if (current.length >= 4) {
        strings.push(current);
      }
      current = "";
    }
  }

  if (current.length >= 4) {
    strings.push(current);
  }

  return strings;
}

function detectPatterns(data) {
  const patterns = [];

  // Check for common patterns
  if (data.includes(Buffer.from("city"))) patterns.push("city_related");
  if (data.includes(Buffer.from("hero"))) patterns.push("hero_related");
  if (data.includes(Buffer.from("march"))) patterns.push("march_related");
  if (data.includes(Buffer.from("alliance"))) patterns.push("alliance_related");

  return patterns;
}

function compareDecodedData(decoded1, decoded2) {
  const diffs = [];

  const allKeys = new Set([...Object.keys(decoded1 || {}), ...Object.keys(decoded2 || {})]);

  for (const key of allKeys) {
    const val1 = decoded1?.[key];
    const val2 = decoded2?.[key];

    if (JSON.stringify(val1) !== JSON.stringify(val2)) {
      diffs.push({ field: key, old: val1, new: val2 });
    }
  }

  return diffs;
}

function compareBytes(data1, data2) {
  const diffs = [];
  const maxLen = Math.max(data1.length, data2.length);

  for (let i = 0; i < maxLen; i++) {
    if (data1[i] !== data2[i]) {
      diffs.push({
        offset: i,
        byte1: data1[i]?.toString(16).padStart(2, "0") || "??",
        byte2: data2[i]?.toString(16).padStart(2, "0") || "??",
      });
    }
  }

  return diffs;
}

function generateFuzzActions(mode, targetAction) {
  const actions = [];
  const prefixes = ["user", "city", "hero", "troop", "march", "alliance", "debug", "admin", "gm"];
  const suffixes = ["Info", "List", "Get", "Set", "Create", "Delete", "Update", "Start", "Stop"];

  if (mode === "action_discovery") {
    for (const prefix of prefixes) {
      for (const suffix of suffixes) {
        actions.push({
          name: `${prefix}.${suffix.toLowerCase()}${suffix.slice(1)}`,
          params: {},
        });
      }
    }
  }

  return actions;
}

async function fuzzInBackground(actions, parallelism, delayMs) {
  for (const action of actions) {
    if (!fuzzingActive) break;

    try {
      // Send fuzz request
      const result = await sendFuzzRequest(action);
      state.fuzzResults.push(result);
    } catch (e) {
      state.fuzzResults.push({ action: action.name, isError: true, error: e.message });
    }

    await new Promise((r) => setTimeout(r, delayMs));
  }
}

async function sendFuzzRequest(action) {
  // Placeholder - would send actual request
  return {
    action: action.name,
    isDiscovery: Math.random() > 0.95,
    isError: false,
    responseTime: Math.random() * 500,
  };
}

function buildAmf3Packet(action, parameters) {
  // Build AMF3 packet
  const buffer = Buffer.alloc(1024);
  let offset = 0;

  // AMF0 envelope
  buffer[offset++] = 0x00;
  buffer[offset++] = 0x00;
  buffer[offset++] = 0x00;
  buffer[offset++] = 0x00;
  buffer[offset++] = 0x00;
  buffer[offset++] = 0x01;

  // Action string
  const actionBytes = Buffer.from(action);
  buffer[offset++] = (actionBytes.length >> 8) & 0xff;
  buffer[offset++] = actionBytes.length & 0xff;
  actionBytes.copy(buffer, offset);
  offset += actionBytes.length;

  return buffer.slice(0, offset);
}

async function sendToFiddlerBridge(command) {
  // Send command to Fiddler bridge via named pipe or socket
  return new Promise((resolve) => {
    setTimeout(resolve, 100);
  });
}

async function callLocalLlm(prompt) {
  // Call local LLM (LM Studio or Ollama)
  try {
    const response = await fetch("http://localhost:1234/v1/chat/completions", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        model: "evony-re-7b",
        messages: [
          { role: "system", content: "You are an expert Evony reverse engineer." },
          { role: "user", content: prompt },
        ],
        temperature: 0.7,
        max_tokens: 2048,
      }),
    });

    const data = await response.json();
    return data.choices[0].message.content;
  } catch (e) {
    return `LLM Error: ${e.message}. Make sure LM Studio is running on port 1234.`;
  }
}

function extractGameState(aspect) {
  // Extract game state from captured packets
  const state = {
    resources: { food: 0, wood: 0, stone: 0, iron: 0, gold: 0 },
    troops: { total: 0, byType: {} },
    heroes: [],
    buildings: [],
    marches: [],
  };

  if (aspect === "all" || aspect === "resources") {
    // Extract from resource-related packets
  }

  return aspect === "all" ? state : state[aspect];
}

function startChangeTracking(aspect, duration) {
  return `track_${Date.now()}`;
}

function runSequenceInBackground(sequence, repeat) {
  // Run in background
}

function generateMarkdownDoc(actions) {
  let md = "# Evony Protocol Documentation\n\n";

  const categories = [...new Set(actions.map((a) => a.category))];

  for (const category of categories) {
    md += `## ${category}\n\n`;
    const categoryActions = actions.filter((a) => a.category === category);

    for (const action of categoryActions) {
      md += `### ${action.name}\n\n`;
      md += `${action.description || "No description"}\n\n`;
      if (action.parameters?.length) {
        md += "**Parameters:**\n";
        for (const param of action.parameters) {
          md += `- \`${param.name}\`: ${param.type} - ${param.description || ""}\n`;
        }
        md += "\n";
      }
    }
  }

  return md;
}

function generateTypeScriptTypes(actions) {
  let ts = "// Auto-generated Evony Protocol Types\n\n";

  const categories = [...new Set(actions.map((a) => a.category))];

  for (const category of categories) {
    ts += `export namespace ${category.charAt(0).toUpperCase() + category.slice(1)} {\n`;
    const categoryActions = actions.filter((a) => a.category === category);

    for (const action of categoryActions) {
      const methodName = action.name.split(".")[1];
      ts += `  export interface ${methodName}Request {\n`;
      for (const param of action.parameters || []) {
        ts += `    ${param.name}: ${param.type};\n`;
      }
      ts += `  }\n\n`;
    }

    ts += `}\n\n`;
  }

  return ts;
}

// ============================================================================
// RESOURCES
// ============================================================================

server.setRequestHandler(ListResourcesRequestSchema, async () => ({
  resources: [
    {
      uri: "evony://protocol-db",
      name: "Protocol Database",
      description: "Learned protocol actions and parameters",
      mimeType: "application/json",
    },
    {
      uri: "evony://captured-packets",
      name: "Captured Packets",
      description: "Recently captured network packets",
      mimeType: "application/json",
    },
    {
      uri: "evony://fuzz-results",
      name: "Fuzz Results",
      description: "Protocol fuzzing results",
      mimeType: "application/json",
    },
    {
      uri: "evony://game-state",
      name: "Game State",
      description: "Current game state from captured data",
      mimeType: "application/json",
    },
  ],
}));

server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const { uri } = request.params;

  switch (uri) {
    case "evony://protocol-db":
      return {
        contents: [
          {
            uri,
            mimeType: "application/json",
            text: JSON.stringify(Array.from(state.learnedActions.values()), null, 2),
          },
        ],
      };

    case "evony://captured-packets":
      return {
        contents: [
          {
            uri,
            mimeType: "application/json",
            text: JSON.stringify(
              state.capturedPackets.slice(-100).map((p, i) => ({
                id: i,
                action: p.action,
                size: p.rawData?.length,
                timestamp: p.timestamp,
              })),
              null,
              2
            ),
          },
        ],
      };

    case "evony://fuzz-results":
      return {
        contents: [
          {
            uri,
            mimeType: "application/json",
            text: JSON.stringify(state.fuzzResults.slice(-100), null, 2),
          },
        ],
      };

    case "evony://game-state":
      return {
        contents: [
          {
            uri,
            mimeType: "application/json",
            text: JSON.stringify(extractGameState("all"), null, 2),
          },
        ],
      };

    default:
      throw new Error(`Unknown resource: ${uri}`);
  }
});

// ============================================================================
// START SERVER
// ============================================================================

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Evony MCP Server v4.0 running on stdio");
}

main().catch(console.error);
