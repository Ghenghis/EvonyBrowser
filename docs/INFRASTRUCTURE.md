# Infrastructure Implementation Guide

**Current Compliance: 75%**  
**Target: 100%**  
**Gap: 25% (4 tasks)**

---

## Overview

The Infrastructure layer handles configuration, logging, authentication, and caching. The current implementation is 75% complete with gaps in environment variables, configuration validation, and log rotation verification.

## Gap Analysis

| Component | Status | Gap |
|-----------|--------|-----|
| Settings File | ✅ Complete | - |
| Environment Variables | ❌ Missing | Need implementation |
| Config Validation | ⚠️ Partial | Need completion |
| Logging | ✅ Complete | - |
| Log Rotation | ⚠️ Unverified | Need verification |
| Session Management | ✅ Complete | - |
| Caching | ✅ Complete | - |

---

## Task 1: Environment Variable Support

Add support for environment variables to override settings, useful for deployment and CI/CD.

### 1.1 Supported Variables

| Variable | Setting | Default |
|----------|---------|---------|
| SVONY_LOG_LEVEL | LogLevel | Information |
| SVONY_MCP_HOST | McpHost | localhost |
| SVONY_MCP_PORT | McpPort | 8080 |
| SVONY_DEBUG_MODE | DebugMode | false |
| SVONY_CACHE_PATH | CachePath | ./Cache |
| SVONY_LOG_PATH | LogPath | ./Logs |

### 1.2 Implementation

**File: SvonyBrowser/Services/SettingsManager.cs**

```csharp
public async Task LoadAsync()
{
    // Load from file first
    await LoadFromFileAsync();
    
    // Override with environment variables
    ApplyEnvironmentOverrides();
}

private void ApplyEnvironmentOverrides()
{
    var logLevel = Environment.GetEnvironmentVariable("SVONY_LOG_LEVEL");
    if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse<LogLevel>(logLevel, out var level))
    {
        Settings.LogLevel = level;
    }
    
    var mcpHost = Environment.GetEnvironmentVariable("SVONY_MCP_HOST");
    if (!string.IsNullOrEmpty(mcpHost))
    {
        Settings.McpHost = mcpHost;
    }
    
    var mcpPort = Environment.GetEnvironmentVariable("SVONY_MCP_PORT");
    if (!string.IsNullOrEmpty(mcpPort) && int.TryParse(mcpPort, out var port))
    {
        Settings.McpPort = port;
    }
    
    var debugMode = Environment.GetEnvironmentVariable("SVONY_DEBUG_MODE");
    if (!string.IsNullOrEmpty(debugMode) && bool.TryParse(debugMode, out var debug))
    {
        Settings.DebugMode = debug;
    }
    
    var cachePath = Environment.GetEnvironmentVariable("SVONY_CACHE_PATH");
    if (!string.IsNullOrEmpty(cachePath))
    {
        Settings.CachePath = cachePath;
    }
    
    var logPath = Environment.GetEnvironmentVariable("SVONY_LOG_PATH");
    if (!string.IsNullOrEmpty(logPath))
    {
        Settings.LogPath = logPath;
    }
}
```

---

## Task 2: Configuration Validation

Add comprehensive validation when loading settings to prevent runtime errors.

### 2.1 Validation Rules

| Setting | Validation | Error Message |
|---------|------------|---------------|
| McpPort | 1-65535 | "MCP port must be between 1 and 65535" |
| RagPort | 1-65535 | "RAG port must be between 1 and 65535" |
| RtePort | 1-65535 | "RTE port must be between 1 and 65535" |
| CachePath | Directory exists or can be created | "Cache path is invalid" |
| LogPath | Directory exists or can be created | "Log path is invalid" |
| LlmTemperature | 0.0-2.0 | "Temperature must be between 0 and 2" |
| MaxTokens | 1-32000 | "Max tokens must be between 1 and 32000" |
| LogRetentionDays | 1-365 | "Log retention must be between 1 and 365 days" |
| CacheSizeMb | 50-10000 | "Cache size must be between 50 and 10000 MB" |

### 2.2 Implementation

**File: SvonyBrowser/Services/SettingsManager.cs**

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

public ValidationResult ValidateSettings()
{
    var result = new ValidationResult { IsValid = true };
    
    // Port validation
    if (Settings.McpPort < 1 || Settings.McpPort > 65535)
    {
        result.Errors.Add("MCP port must be between 1 and 65535");
        result.IsValid = false;
    }
    
    if (Settings.RagPort < 1 || Settings.RagPort > 65535)
    {
        result.Errors.Add("RAG port must be between 1 and 65535");
        result.IsValid = false;
    }
    
    if (Settings.RtePort < 1 || Settings.RtePort > 65535)
    {
        result.Errors.Add("RTE port must be between 1 and 65535");
        result.IsValid = false;
    }
    
    // Path validation
    if (!IsValidPath(Settings.CachePath))
    {
        result.Errors.Add("Cache path is invalid or cannot be created");
        result.IsValid = false;
    }
    
    if (!IsValidPath(Settings.LogPath))
    {
        result.Errors.Add("Log path is invalid or cannot be created");
        result.IsValid = false;
    }
    
    // LLM settings
    if (Settings.LlmTemperature < 0 || Settings.LlmTemperature > 2)
    {
        result.Errors.Add("Temperature must be between 0 and 2");
        result.IsValid = false;
    }
    
    if (Settings.MaxTokens < 1 || Settings.MaxTokens > 32000)
    {
        result.Errors.Add("Max tokens must be between 1 and 32000");
        result.IsValid = false;
    }
    
    // Retention settings
    if (Settings.LogRetentionDays < 1 || Settings.LogRetentionDays > 365)
    {
        result.Errors.Add("Log retention must be between 1 and 365 days");
        result.IsValid = false;
    }
    
    if (Settings.CacheSizeMb < 50 || Settings.CacheSizeMb > 10000)
    {
        result.Errors.Add("Cache size must be between 50 and 10000 MB");
        result.IsValid = false;
    }
    
    return result;
}

private bool IsValidPath(string path)
{
    try
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
            
        var fullPath = Path.GetFullPath(path);
        
        if (Directory.Exists(fullPath))
            return true;
            
        Directory.CreateDirectory(fullPath);
        return true;
    }
    catch
    {
        return false;
    }
}
```

---

## Task 3: Log Rotation Configuration

Verify and configure Serilog log rotation settings.

### 3.1 Current Configuration

**File: SvonyBrowser/App.xaml.cs**

```csharp
Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(
        path: "Logs/svony-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,           // Keep 30 days
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB max per file
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

### 3.2 Verification Steps

1. Check that log files are created in the Logs directory
2. Verify files rotate daily
3. Confirm old files are deleted after retention period
4. Test file size limit triggers rotation

---

## Task 4: Structured Logging

Add consistent structured logging format across all services.

### 4.1 Logging Standards

| Log Level | Usage |
|-----------|-------|
| Debug | Detailed diagnostic information |
| Information | Normal operation events |
| Warning | Unexpected but handled situations |
| Error | Errors that don't stop the application |
| Fatal | Critical errors that stop the application |

### 4.2 Logging Template

```csharp
// Good: Structured logging with context
App.Logger.Information("User {UserId} connected to server {ServerName}", userId, serverName);

// Good: Error with exception
App.Logger.Error(ex, "Failed to load settings from {Path}", settingsPath);

// Bad: String concatenation
App.Logger.Information("User " + userId + " connected"); // Don't do this
```

### 4.3 Service Logging Pattern

Each service should log:
- Initialization start/complete
- Key operations with timing
- Errors with full context
- Disposal/cleanup

```csharp
public class ServiceName
{
    public async Task InitializeAsync()
    {
        App.Logger.Information("Initializing {ServiceName}", nameof(ServiceName));
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Initialization logic
            
            sw.Stop();
            App.Logger.Information("{ServiceName} initialized in {ElapsedMs}ms", 
                nameof(ServiceName), sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to initialize {ServiceName}", nameof(ServiceName));
            throw;
        }
    }
}
```

---

## Implementation Checklist

- [ ] INFRA-001: Add environment variable support
- [ ] INFRA-002: Add configuration validation on load
- [ ] INFRA-003: Verify and configure log rotation
- [ ] INFRA-004: Add structured logging to all services

---

## Verification

After implementation:

1. Environment variables override file settings
2. Invalid settings show clear error messages
3. Log files rotate correctly
4. All services log consistently
5. Logs are searchable and parseable
