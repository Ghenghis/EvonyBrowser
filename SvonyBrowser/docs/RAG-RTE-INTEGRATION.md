# 🔗 RAG & RTE Real-Time Integration Guide

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Specification Document

---

## 📋 Overview

This document details the architecture and implementation for integrating **Evony RAG** (Retrieval-Augmented Generation) and **Evony RTE** (Real-Time Engine) MCP servers into Svony Browser for live, always-on knowledge and traffic analysis.

---

## 🏗️ Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                      SVONY BROWSER 2.0                               │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    MCP CONNECTION MANAGER                    │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │    │
│  │  │ RAG Client  │  │ RTE Client  │  │ Connection Monitor  │  │    │
│  │  │             │  │             │  │                     │  │    │
│  │  │ • Query     │  │ • Decode    │  │ • Health Check      │  │    │
│  │  │ • Search    │  │ • Analyze   │  │ • Auto-reconnect    │  │    │
│  │  │ • Index     │  │ • Stream    │  │ • Status Events     │  │    │
│  │  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │    │
│  └─────────┼────────────────┼────────────────────┼─────────────┘    │
│            │                │                    │                   │
└────────────┼────────────────┼────────────────────┼───────────────────┘
             │                │                    │
             ▼                ▼                    ▼
     ┌───────────────┐ ┌─────────────────┐ ┌─────────────────┐
     │  EVONY RAG    │ │   EVONY RTE     │ │ STATUS MONITOR  │
     │  MCP SERVER   │ │   MCP SERVER    │ │                 │
     │               │ │                 │ │ • Heartbeat     │
     │ Port: 3100    │ │ Port: 3101      │ │ • Latency       │
     │               │ │                 │ │ • Queue Depth   │
     │ ┌───────────┐ │ │ ┌───────────┐   │ └─────────────────┘
     │ │ Vector DB │ │ │ │ AMF Codec │   │
     │ │ ChromaDB  │ │ │ │           │   │
     │ └───────────┘ │ │ └───────────┘   │
     │ ┌───────────┐ │ │ ┌───────────┐   │
     │ │ Embeddings│ │ │ │ Protocol  │   │
     │ │ Model     │ │ │ │ Database  │   │
     │ └───────────┘ │ │ └───────────┘   │
     └───────────────┘ └─────────────────┘
```

### Data Flow

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  User    │───▶│ Chatbot  │───▶│ RAG/RTE  │───▶│ Response │
│  Query   │    │ Router   │    │ Process  │    │ Render   │
└──────────┘    └──────────┘    └──────────┘    └──────────┘
                     │
                     ▼
              ┌─────────────┐
              │ Query Type  │
              │ Detection   │
              └─────────────┘
                     │
         ┌──────────┼──────────┐
         ▼          ▼          ▼
    ┌────────┐ ┌────────┐ ┌────────┐
    │  RAG   │ │  RTE   │ │ Hybrid │
    │ Query  │ │ Query  │ │ Query  │
    └────────┘ └────────┘ └────────┘
```

---

## 🔧 RAG MCP Server Integration

### Connection Configuration

```json
{
  "ragServer": {
    "name": "evony-rag",
    "host": "localhost",
    "port": 3100,
    "protocol": "stdio",
    "reconnect": {
      "enabled": true,
      "maxAttempts": 5,
      "delayMs": 2000,
      "backoffMultiplier": 1.5
    },
    "healthCheck": {
      "enabled": true,
      "intervalMs": 30000,
      "timeoutMs": 5000
    }
  }
}
```

### RAG Tools Available

| Tool Name            | Description                           | Parameters                  |
| -------------------- | ------------------------------------- | --------------------------- |
| `evony_search`       | Semantic search across knowledge base | `query`, `limit`, `filters` |
| `evony_query`        | Natural language Q&A                  | `question`, `context`       |
| `evony_index`        | Add new document to KB                | `content`, `metadata`       |
| `evony_list_sources` | List indexed sources                  | `category`                  |
| `evony_get_document` | Retrieve specific document            | `doc_id`                    |

### RAG Query Types

#### 1. Knowledge Queries
```
User: "What are the best hero combinations for rally?"
→ Routes to: evony_query
→ Returns: Strategic advice from knowledge base
```

#### 2. Search Queries
```
User: "Find all documents about march speed buffs"
→ Routes to: evony_search
→ Returns: Relevant document excerpts with sources
```

#### 3. Data Queries
```
User: "What is the attack bonus for level 15 archer tower?"
→ Routes to: evony_query with game_data context
→ Returns: Specific game data values
```

### RAG Client Implementation

```csharp
public class EvonyRagClient : IEvonyRagClient, IDisposable
{
    private readonly McpClient _mcpClient;
    private readonly ILogger _logger;
    private bool _isConnected;
    private CancellationTokenSource _healthCheckCts;

    public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
    public bool IsConnected => _isConnected;

    public async Task<RagResponse> QueryAsync(string question, QueryOptions options = null)
    {
        EnsureConnected();
        
        var request = new McpToolRequest
        {
            Tool = "evony_query",
            Parameters = new
            {
                question = question,
                context = options?.Context,
                include_sources = options?.IncludeSources ?? true,
                max_tokens = options?.MaxTokens ?? 1000
            }
        };

        var response = await _mcpClient.CallToolAsync(request);
        return ParseRagResponse(response);
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int limit = 10)
    {
        EnsureConnected();
        
        var request = new McpToolRequest
        {
            Tool = "evony_search",
            Parameters = new
            {
                query = query,
                limit = limit,
                include_content = true
            }
        };

        var response = await _mcpClient.CallToolAsync(request);
        return ParseSearchResults(response);
    }

    public async Task IndexTrafficAsync(CapturedPacket packet)
    {
        // Index decoded traffic for future RAG queries
        var document = new
        {
            content = packet.DecodedContent,
            metadata = new
            {
                action = packet.ActionName,
                timestamp = packet.Timestamp,
                direction = packet.Direction,
                server = "cc2.evony.com"
            }
        };

        await _mcpClient.CallToolAsync(new McpToolRequest
        {
            Tool = "evony_index",
            Parameters = document
        });
    }
}
```

---

## ⚡ RTE MCP Server Integration

### Connection Configuration

```json
{
  "rteServer": {
    "name": "evony-rte",
    "host": "localhost", 
    "port": 3101,
    "protocol": "stdio",
    "trafficPipe": "\\\\.\\pipe\\SvonyFiddlerTraffic",
    "features": {
      "amfDecode": true,
      "protocolLookup": true,
      "trafficStream": true,
      "packetEdit": true
    }
  }
}
```

### RTE Tools Available

| Tool Name               | Description                    | Parameters                  |
| ----------------------- | ------------------------------ | --------------------------- |
| `evony_decode_amf`      | Decode AMF3 binary data        | `hex_data` or `base64_data` |
| `evony_encode_amf`      | Encode data to AMF3            | `data`, `format`            |
| `evony_get_protocol`    | Get action protocol definition | `action_name`               |
| `evony_list_actions`    | List all known actions         | `category`, `search`        |
| `evony_analyze_traffic` | Analyze traffic patterns       | `session_id`, `timerange`   |
| `evony_inject_packet`   | Inject packet into stream      | `packet_data`, `direction`  |

### RTE Client Implementation

```csharp
public class EvonyRteClient : IEvonyRteClient, IDisposable
{
    private readonly McpClient _mcpClient;
    private readonly NamedPipeClientStream _trafficPipe;
    private readonly ILogger _logger;
    private bool _isStreaming;

    public event EventHandler<TrafficEventArgs> OnTrafficCaptured;
    public event EventHandler<DecodedPacketEventArgs> OnPacketDecoded;
    public bool IsConnected => _mcpClient.IsConnected;

    public async Task<DecodedPacket> DecodeAmfAsync(byte[] rawData)
    {
        var hexData = BitConverter.ToString(rawData).Replace("-", "");
        
        var response = await _mcpClient.CallToolAsync(new McpToolRequest
        {
            Tool = "evony_decode_amf",
            Parameters = new { hex_data = hexData }
        });

        return new DecodedPacket
        {
            Raw = rawData,
            Decoded = response.Result,
            ActionName = ExtractActionName(response.Result),
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<ProtocolInfo> GetProtocolInfoAsync(string actionName)
    {
        var response = await _mcpClient.CallToolAsync(new McpToolRequest
        {
            Tool = "evony_get_protocol",
            Parameters = new { action_name = actionName }
        });

        return ParseProtocolInfo(response);
    }

    public async Task StartTrafficStreamAsync(CancellationToken ct)
    {
        _isStreaming = true;
        
        // Connect to Fiddler traffic pipe
        await _trafficPipe.ConnectAsync(ct);
        
        // Start reading traffic
        _ = Task.Run(async () =>
        {
            var buffer = new byte[65536];
            while (_isStreaming && !ct.IsCancellationRequested)
            {
                var bytesRead = await _trafficPipe.ReadAsync(buffer, ct);
                if (bytesRead > 0)
                {
                    var packet = new CapturedPacket(buffer.Take(bytesRead).ToArray());
                    OnTrafficCaptured?.Invoke(this, new TrafficEventArgs(packet));
                    
                    // Auto-decode AMF packets
                    if (packet.IsAmf)
                    {
                        var decoded = await DecodeAmfAsync(packet.Body);
                        OnPacketDecoded?.Invoke(this, new DecodedPacketEventArgs(decoded));
                    }
                }
            }
        }, ct);
    }

    public async Task<InjectionResult> InjectPacketAsync(AmfPacket packet, Direction direction)
    {
        var response = await _mcpClient.CallToolAsync(new McpToolRequest
        {
            Tool = "evony_inject_packet",
            Parameters = new
            {
                packet_data = packet.Encode(),
                direction = direction.ToString().ToLower(),
                target_session = packet.SessionId
            }
        });

        return ParseInjectionResult(response);
    }
}
```

---

## 🔄 Real-Time Traffic Pipeline

### Fiddler → Svony Browser → MCP Pipeline

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   FIDDLER   │────▶│ Named Pipe  │────▶│   SVONY     │────▶│  MCP RTE    │
│   Proxy     │     │ Transport   │     │  Browser    │     │  Server     │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                                       │                    │
       │                                       ▼                    │
       │                              ┌─────────────┐               │
       │                              │  Chatbot    │◀──────────────┘
       │                              │  Display    │
       │                              └─────────────┘
       │                                       │
       │                                       ▼
       │                              ┌─────────────┐
       └─────────────────────────────▶│  RAG Index  │
                                      │  (Optional) │
                                      └─────────────┘
```

### Traffic Filter Implementation

```csharp
public class TrafficFilter
{
    private readonly HashSet<string> _allowedHosts = new()
    {
        "cc2.evony.com",
        "cc1.evony.com",
        "cc3.evony.com",
        "cc4.evony.com",
        "cc5.evony.com"
    };

    public bool ShouldCapture(string hostname)
    {
        return _allowedHosts.Contains(hostname.ToLower());
    }

    public TrafficType ClassifyTraffic(CapturedPacket packet)
    {
        if (packet.ContentType?.Contains("application/x-amf") == true)
            return TrafficType.AMF3;
        if (packet.Url?.EndsWith(".swf") == true)
            return TrafficType.SWF;
        if (packet.ContentType?.Contains("application/json") == true)
            return TrafficType.JSON;
        return TrafficType.Other;
    }
}
```

---

## 🎯 Query Router

### Automatic Query Classification

```csharp
public class QueryRouter
{
    private readonly IEvonyRagClient _ragClient;
    private readonly IEvonyRteClient _rteClient;

    public async Task<QueryResult> RouteQueryAsync(string userQuery)
    {
        var queryType = ClassifyQuery(userQuery);
        
        return queryType switch
        {
            QueryType.Knowledge => await HandleKnowledgeQuery(userQuery),
            QueryType.Traffic => await HandleTrafficQuery(userQuery),
            QueryType.Protocol => await HandleProtocolQuery(userQuery),
            QueryType.Hybrid => await HandleHybridQuery(userQuery),
            _ => await HandleGeneralQuery(userQuery)
        };
    }

    private QueryType ClassifyQuery(string query)
    {
        var lowerQuery = query.ToLower();
        
        // Traffic/Protocol queries
        if (lowerQuery.Contains("decode") || 
            lowerQuery.Contains("packet") ||
            lowerQuery.Contains("amf") ||
            lowerQuery.Contains("traffic"))
            return QueryType.Traffic;
        
        // Protocol documentation queries
        if (lowerQuery.Contains("action") ||
            lowerQuery.Contains("protocol") ||
            lowerQuery.Contains("api"))
            return QueryType.Protocol;
        
        // Knowledge queries
        if (lowerQuery.Contains("how to") ||
            lowerQuery.Contains("what is") ||
            lowerQuery.Contains("best") ||
            lowerQuery.Contains("strategy"))
            return QueryType.Knowledge;
        
        // Default to hybrid for complex queries
        return QueryType.Hybrid;
    }

    private async Task<QueryResult> HandleHybridQuery(string query)
    {
        // Execute both RAG and RTE in parallel
        var ragTask = _ragClient.QueryAsync(query);
        var recentTraffic = await _rteClient.GetRecentTrafficAsync(TimeSpan.FromMinutes(5));
        
        var ragResult = await ragTask;
        
        // Combine results with context
        return new QueryResult
        {
            Type = QueryType.Hybrid,
            RagResponse = ragResult,
            TrafficContext = recentTraffic,
            CombinedAnswer = BuildCombinedAnswer(ragResult, recentTraffic, query)
        };
    }
}
```

---

## 📡 Connection Lifecycle

### Startup Sequence

```csharp
public class McpConnectionManager : IDisposable
{
    private readonly IEvonyRagClient _ragClient;
    private readonly IEvonyRteClient _rteClient;
    private readonly ILogger _logger;

    public async Task InitializeAsync()
    {
        _logger.Information("Initializing MCP connections...");
        
        // Connect to RAG server
        try
        {
            await _ragClient.ConnectAsync();
            _logger.Information("RAG MCP connected");
            OnRagConnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "RAG MCP connection failed, will retry");
            StartReconnectTimer(_ragClient);
        }

        // Connect to RTE server
        try
        {
            await _rteClient.ConnectAsync();
            _logger.Information("RTE MCP connected");
            OnRteConnected?.Invoke(this, EventArgs.Empty);
            
            // Start traffic streaming
            await _rteClient.StartTrafficStreamAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "RTE MCP connection failed, will retry");
            StartReconnectTimer(_rteClient);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ragClient?.Dispose();
        _rteClient?.Dispose();
    }
}
```

### Status Indicators

```xaml
<!-- Status bar indicators -->
<StackPanel Orientation="Horizontal">
    <Ellipse Width="10" Height="10" 
             Fill="{Binding RagStatus, Converter={StaticResource StatusToColorConverter}}"
             ToolTip="RAG MCP Status"/>
    <TextBlock Text="RAG" Margin="5,0,15,0"/>
    
    <Ellipse Width="10" Height="10"
             Fill="{Binding RteStatus, Converter={StaticResource StatusToColorConverter}}"
             ToolTip="RTE MCP Status"/>
    <TextBlock Text="RTE" Margin="5,0,15,0"/>
    
    <Ellipse Width="10" Height="10"
             Fill="{Binding FiddlerStatus, Converter={StaticResource StatusToColorConverter}}"
             ToolTip="Fiddler Proxy Status"/>
    <TextBlock Text="Fiddler" Margin="5,0,15,0"/>
    
    <TextBlock Text="cc2.evony.com:" Margin="10,0,5,0"/>
    <TextBlock Text="{Binding TrafficCount}" Foreground="Gold"/>
    <TextBlock Text=" packets" Foreground="Gray"/>
</StackPanel>
```

---

## 🔐 Security Considerations

### Traffic Encryption
- All MCP communication uses stdio (local only)
- Named pipes for traffic use Windows security descriptors
- No external network exposure

### Data Handling
- Traffic data stored in memory only by default
- Optional encrypted local cache for session replay
- RAG indexing can be disabled for privacy

---

## 📊 Performance Optimization

### Caching Strategy

```csharp
public class RagCache
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(15);

    public async Task<RagResponse> GetOrQueryAsync(string query, Func<Task<RagResponse>> queryFunc)
    {
        var cacheKey = ComputeHash(query);
        
        if (_cache.TryGetValue(cacheKey, out RagResponse cached))
        {
            return cached;
        }

        var result = await queryFunc();
        _cache.Set(cacheKey, result, _defaultExpiration);
        return result;
    }
}
```

### Batch Processing

```csharp
public class TrafficBatcher
{
    private readonly Channel<CapturedPacket> _packetChannel;
    private readonly int _batchSize = 10;
    private readonly TimeSpan _batchTimeout = TimeSpan.FromMilliseconds(100);

    public async Task ProcessBatchAsync()
    {
        var batch = new List<CapturedPacket>();
        var deadline = DateTime.UtcNow + _batchTimeout;

        while (batch.Count < _batchSize && DateTime.UtcNow < deadline)
        {
            if (_packetChannel.Reader.TryRead(out var packet))
            {
                batch.Add(packet);
            }
            else
            {
                await Task.Delay(10);
            }
        }

        if (batch.Any())
        {
            await ProcessPacketsAsync(batch);
        }
    }
}
```

---

## 📚 Related Documentation

- [FEATURE-ROADMAP.md](./FEATURE-ROADMAP.md) - Overall feature roadmap
- [CHATBOT-DESIGN.md](./CHATBOT-DESIGN.md) - Chatbot integration details
- [MCP-INTEGRATION.md](./MCP-INTEGRATION.md) - MCP server setup guide
- [FIDDLER-SCRIPTS.md](./FIDDLER-SCRIPTS.md) - Fiddler script library
