# 🤖 Advanced Chatbot Interface Design

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Specification Document

---

## 📋 Overview

The Svony Browser chatbot is a **Microsoft Co-pilot style interface** integrated into the right panel, providing:

- Real-time RAG/RTE queries
- File upload and analysis
- Tool execution
- Evony-specific assistance
- Packet decoding and editing

---

## 🎨 UI Design

### Layout Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SVONY BROWSER 2.0                                  │
├───────────────────────────────────────────┬─────────────────────────────────┤
│                                           │                                 │
│  ┌─────────────────┐ ┌─────────────────┐  │  ┌─────────────────────────┐   │
│  │   AutoEvony     │ │  EvonyClient    │  │  │   🤖 EVONY CO-PILOT     │   │
│  │   Panel         │ │  Panel          │  │  ├─────────────────────────┤   │
│  │                 │ │                 │  │  │                         │   │
│  │   [Browser]     │ │   [Browser]     │  │  │  Chat History           │   │
│  │                 │ │                 │  │  │  ┌───────────────────┐  │   │
│  │                 │ │                 │  │  │  │ User: How do I... │  │   │
│  │                 │ │                 │  │  │  └───────────────────┘  │   │
│  │                 │ │                 │  │  │  ┌───────────────────┐  │   │
│  │                 │ │                 │  │  │  │ 🤖 Based on the   │  │   │
│  │                 │ │                 │  │  │  │ knowledge base... │  │   │
│  │                 │ │                 │  │  │  └───────────────────┘  │   │
│  │                 │ │                 │  │  │                         │   │
│  └─────────────────┘ └─────────────────┘  │  ├─────────────────────────┤   │
│                                           │  │  Tools: [📁][📤][🔧][⚙️] │   │
│                                           │  ├─────────────────────────┤   │
│                                           │  │  [Type message...]  [➤] │   │
│                                           │  └─────────────────────────┘   │
├───────────────────────────────────────────┴─────────────────────────────────┤
│  [RAG: 🟢] [RTE: 🟢] [Fiddler: 🟢] │ Traffic: 1,234 │ cc2.evony.com: LIVE   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Color Scheme (Evony Theme)

```css
/* Primary Colors */
--evony-gold: #DAA520;
--evony-gold-light: #FFD700;
--evony-gold-dark: #B8860B;

/* Background Colors */
--bg-primary: #0a0505;
--bg-secondary: #1a1208;
--bg-panel: #1a1a1a;
--bg-input: #2a2a2a;

/* Message Colors */
--msg-user-bg: #1a2a4a;
--msg-user-border: #4a6a9a;
--msg-ai-bg: #2a2008;
--msg-ai-border: #DAA520;
--msg-system-bg: #1a1a1a;
--msg-code-bg: #0d0d0d;

/* Status Colors */
--status-connected: #44ff44;
--status-disconnected: #ff4444;
--status-connecting: #ffaa44;
```

---

## 📦 Component Architecture

### ChatbotPanel.xaml

```xaml
<UserControl x:Class="SvonyBrowser.Controls.ChatbotPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:SvonyBrowser.Controls"
             Background="#0a0505">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="50"/>      <!-- Header -->
            <RowDefinition Height="*"/>        <!-- Chat History -->
            <RowDefinition Height="Auto"/>     <!-- Tools Bar -->
            <RowDefinition Height="Auto"/>     <!-- Input Area -->
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#1a1208" BorderBrush="#DAA520" BorderThickness="0,0,0,2">
            <Grid>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="10,0">
                    <TextBlock Text="🤖" FontSize="20" VerticalAlignment="Center"/>
                    <TextBlock Text="EVONY CO-PILOT" FontSize="16" FontWeight="Bold" 
                               Foreground="#FFD700" Margin="10,0,0,0" VerticalAlignment="Center"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="10,0">
                    <ComboBox x:Name="ModelSelector" Width="120" Margin="0,0,10,0">
                        <ComboBoxItem Content="RAG + RTE" IsSelected="True"/>
                        <ComboBoxItem Content="RAG Only"/>
                        <ComboBoxItem Content="RTE Only"/>
                        <ComboBoxItem Content="Local LLM"/>
                    </ComboBox>
                    <Button Content="⚙️" Width="30" Click="Settings_Click"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Chat History -->
        <ScrollViewer Grid.Row="1" x:Name="ChatScroller" 
                      VerticalScrollBarVisibility="Auto"
                      Background="#0a0505">
            <ItemsControl x:Name="ChatMessages" 
                          ItemsSource="{Binding Messages}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <local:ChatMessage Message="{Binding}"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Tools Bar -->
        <Border Grid.Row="2" Background="#1a1a1a" Padding="10,5">
            <StackPanel Orientation="Horizontal">
                <Button x:Name="BtnAttachFile" Content="📁" ToolTip="Attach File" 
                        Width="35" Height="35" Margin="0,0,5,0" Click="AttachFile_Click"/>
                <Button x:Name="BtnUpload" Content="📤" ToolTip="Upload/Drop Zone"
                        Width="35" Height="35" Margin="0,0,5,0" Click="Upload_Click"/>
                <Button x:Name="BtnTools" Content="🔧" ToolTip="Quick Tools"
                        Width="35" Height="35" Margin="0,0,5,0" Click="Tools_Click"/>
                <Button x:Name="BtnDecode" Content="📦" ToolTip="Decode Packet"
                        Width="35" Height="35" Margin="0,0,5,0" Click="DecodePacket_Click"/>
                <Separator Width="1" Background="#3a3a3a" Margin="5,5"/>
                <TextBlock x:Name="AttachedFileLabel" VerticalAlignment="Center" 
                           Foreground="#888" FontSize="11"/>
            </StackPanel>
        </Border>

        <!-- Input Area -->
        <Border Grid.Row="3" Background="#1a1208" Padding="10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <TextBox x:Name="InputBox" Grid.Column="0"
                         Background="#2a2a2a" Foreground="White"
                         BorderBrush="#5a4a3a" BorderThickness="1"
                         Padding="10,8" FontSize="14"
                         AcceptsReturn="True" TextWrapping="Wrap"
                         MaxHeight="100" VerticalScrollBarVisibility="Auto"
                         KeyDown="InputBox_KeyDown"
                         Text="{Binding InputText, UpdateSourceTrigger=PropertyChanged}"/>
                
                <Button Grid.Column="1" Content="➤" 
                        Width="50" Height="40" Margin="10,0,0,0"
                        Background="#DAA520" Foreground="Black"
                        FontSize="18" FontWeight="Bold"
                        Click="Send_Click"
                        IsEnabled="{Binding CanSend}"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### ChatMessage.xaml

```xaml
<UserControl x:Class="SvonyBrowser.Controls.ChatMessage">
    <Border Margin="10,5" Padding="12" CornerRadius="8"
            Background="{Binding Background}"
            BorderBrush="{Binding BorderColor}"
            BorderThickness="1"
            HorizontalAlignment="{Binding Alignment}">
        <Grid MaxWidth="500">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- Header -->
                <RowDefinition Height="*"/>      <!-- Content -->
                <RowDefinition Height="Auto"/>  <!-- Footer -->
            </Grid.RowDefinitions>

            <!-- Message Header -->
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,5">
                <TextBlock Text="{Binding Icon}" FontSize="14"/>
                <TextBlock Text="{Binding SenderName}" FontWeight="Bold" 
                           Foreground="{Binding HeaderColor}" Margin="5,0,0,0"/>
                <TextBlock Text="{Binding Timestamp, StringFormat=HH:mm}" 
                           Foreground="#666" FontSize="10" Margin="10,2,0,0"/>
            </StackPanel>

            <!-- Message Content -->
            <ContentPresenter Grid.Row="1" Content="{Binding Content}">
                <ContentPresenter.Resources>
                    <!-- Text Content -->
                    <DataTemplate DataType="{x:Type local:TextContent}">
                        <TextBlock Text="{Binding Text}" TextWrapping="Wrap" 
                                   Foreground="#ddd" FontSize="13"/>
                    </DataTemplate>
                    
                    <!-- Code Block -->
                    <DataTemplate DataType="{x:Type local:CodeContent}">
                        <Border Background="#0d0d0d" CornerRadius="4" Padding="10" Margin="0,5">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <StackPanel Grid.Row="0" Orientation="Horizontal">
                                    <TextBlock Text="{Binding Language}" Foreground="#888" FontSize="10"/>
                                    <Button Content="📋" ToolTip="Copy" FontSize="10" 
                                            Background="Transparent" BorderThickness="0"
                                            Click="CopyCode_Click" HorizontalAlignment="Right"/>
                                </StackPanel>
                                <TextBlock Grid.Row="1" Text="{Binding Code}" 
                                           FontFamily="Consolas" FontSize="12"
                                           Foreground="#88ff88" TextWrapping="Wrap"/>
                            </Grid>
                        </Border>
                    </DataTemplate>
                    
                    <!-- Packet Display -->
                    <DataTemplate DataType="{x:Type local:PacketContent}">
                        <Expander Header="{Binding ActionName}" IsExpanded="False">
                            <local:AmfTreeView Data="{Binding DecodedData}"/>
                        </Expander>
                    </DataTemplate>
                    
                    <!-- File Attachment -->
                    <DataTemplate DataType="{x:Type local:FileContent}">
                        <Border Background="#1a1a2a" CornerRadius="4" Padding="8">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="📎" FontSize="16"/>
                                <TextBlock Text="{Binding FileName}" Foreground="#88aaff" 
                                           Margin="5,0" VerticalAlignment="Center"/>
                                <TextBlock Text="{Binding FileSize}" Foreground="#666" 
                                           FontSize="10" VerticalAlignment="Center"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ContentPresenter.Resources>
            </ContentPresenter>

            <!-- Message Footer (Sources, Actions) -->
            <StackPanel Grid.Row="2" Margin="0,5,0,0" 
                        Visibility="{Binding HasSources, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="Sources:" Foreground="#888" FontSize="10"/>
                <ItemsControl ItemsSource="{Binding Sources}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Foreground="#DAA520" FontSize="10" Margin="5,2">
                                <Run Text="• "/><Run Text="{Binding Title}"/>
                            </TextBlock>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

---

## 🔧 Tool Integration

### Quick Tools Menu

```csharp
public class QuickToolsMenu
{
    public List<QuickTool> Tools { get; } = new()
    {
        new QuickTool
        {
            Name = "Decode Packet",
            Icon = "📦",
            Description = "Decode AMF3 packet from hex/base64",
            Action = async (input) => await DecodePacketAsync(input)
        },
        new QuickTool
        {
            Name = "Query RAG",
            Icon = "🔍",
            Description = "Search Evony knowledge base",
            Action = async (input) => await QueryRagAsync(input)
        },
        new QuickTool
        {
            Name = "Analyze Traffic",
            Icon = "📊",
            Description = "Analyze recent captured traffic",
            Action = async (input) => await AnalyzeTrafficAsync(input)
        },
        new QuickTool
        {
            Name = "Protocol Lookup",
            Icon = "📖",
            Description = "Look up action protocol definition",
            Action = async (input) => await LookupProtocolAsync(input)
        },
        new QuickTool
        {
            Name = "Decompile SWF",
            Icon = "🔓",
            Description = "Extract ActionScript from SWF",
            Action = async (input) => await DecompileSwfAsync(input)
        },
        new QuickTool
        {
            Name = "Generate Script",
            Icon = "📝",
            Description = "Generate Fiddler/Bot script",
            Action = async (input) => await GenerateScriptAsync(input)
        }
    };
}
```

### File Upload Handler

```csharp
public class FileUploadHandler
{
    private readonly Dictionary<string, IFileProcessor> _processors = new()
    {
        { ".txt", new TextFileProcessor() },
        { ".md", new MarkdownProcessor() },
        { ".json", new JsonProcessor() },
        { ".amf", new AmfFileProcessor() },
        { ".swf", new SwfFileProcessor() },
        { ".har", new HarFileProcessor() },
        { ".fiddler", new FiddlerArchiveProcessor() }
    };

    public async Task<ProcessedFile> ProcessFileAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        
        if (!_processors.TryGetValue(extension, out var processor))
        {
            return new ProcessedFile
            {
                Success = false,
                Error = $"Unsupported file type: {extension}"
            };
        }

        return await processor.ProcessAsync(filePath);
    }
}

public interface IFileProcessor
{
    Task<ProcessedFile> ProcessAsync(string filePath);
    string[] SupportedExtensions { get; }
    string Description { get; }
}

public class AmfFileProcessor : IFileProcessor
{
    public string[] SupportedExtensions => new[] { ".amf", ".bin" };
    public string Description => "AMF3 Binary Files";

    public async Task<ProcessedFile> ProcessAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        var decoded = await AmfDecoder.DecodeAsync(bytes);
        
        return new ProcessedFile
        {
            Success = true,
            FileName = Path.GetFileName(filePath),
            Content = new PacketContent
            {
                RawData = bytes,
                DecodedData = decoded,
                ActionName = ExtractActionName(decoded)
            },
            Preview = GeneratePreview(decoded)
        };
    }
}
```

---

## 💬 Message Types

### Message Models

```csharp
public abstract class ChatMessageBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public MessageRole Role { get; set; }
    public string SenderName { get; set; }
    public string Icon { get; set; }
}

public class UserMessage : ChatMessageBase
{
    public string Text { get; set; }
    public List<AttachedFile> Attachments { get; set; } = new();
    
    public UserMessage()
    {
        Role = MessageRole.User;
        SenderName = "You";
        Icon = "👤";
    }
}

public class AssistantMessage : ChatMessageBase
{
    public string Text { get; set; }
    public List<CodeBlock> CodeBlocks { get; set; } = new();
    public List<Source> Sources { get; set; } = new();
    public DecodedPacket PacketData { get; set; }
    public bool IsStreaming { get; set; }
    
    public AssistantMessage()
    {
        Role = MessageRole.Assistant;
        SenderName = "Evony Co-Pilot";
        Icon = "🤖";
    }
}

public class SystemMessage : ChatMessageBase
{
    public string Text { get; set; }
    public SystemMessageType Type { get; set; }
    
    public SystemMessage()
    {
        Role = MessageRole.System;
        SenderName = "System";
        Icon = "ℹ️";
    }
}

public class TrafficMessage : ChatMessageBase
{
    public CapturedPacket Packet { get; set; }
    public DecodedPacket Decoded { get; set; }
    public string ActionName { get; set; }
    public TrafficDirection Direction { get; set; }
    
    public TrafficMessage()
    {
        Role = MessageRole.Traffic;
        SenderName = "Traffic Capture";
        Icon = "📡";
    }
}
```

---

## 🔄 Chat Service

### ChatbotService Implementation

```csharp
public class ChatbotService : IDisposable
{
    private readonly IEvonyRagClient _ragClient;
    private readonly IEvonyRteClient _rteClient;
    private readonly ILlmClient _llmClient;
    private readonly ObservableCollection<ChatMessageBase> _messages;
    private readonly QueryRouter _queryRouter;

    public event EventHandler<MessageEventArgs> MessageReceived;
    public event EventHandler<StreamingEventArgs> StreamingUpdate;

    public async Task<AssistantMessage> SendMessageAsync(UserMessage userMessage)
    {
        // Add user message to history
        _messages.Add(userMessage);

        // Process any attachments
        var attachmentContext = await ProcessAttachmentsAsync(userMessage.Attachments);

        // Route query to appropriate handler
        var routedQuery = await _queryRouter.RouteQueryAsync(userMessage.Text);

        // Build context
        var context = new ChatContext
        {
            ConversationHistory = _messages.TakeLast(10).ToList(),
            AttachmentContext = attachmentContext,
            RecentTraffic = await _rteClient.GetRecentTrafficAsync(TimeSpan.FromMinutes(5)),
            QueryType = routedQuery.Type
        };

        // Create streaming response
        var response = new AssistantMessage { IsStreaming = true };
        _messages.Add(response);

        // Stream response based on query type
        await foreach (var chunk in GenerateResponseAsync(userMessage.Text, context))
        {
            response.Text += chunk;
            StreamingUpdate?.Invoke(this, new StreamingEventArgs(chunk));
        }

        response.IsStreaming = false;
        
        // Add sources if from RAG
        if (routedQuery.RagResponse?.Sources != null)
        {
            response.Sources = routedQuery.RagResponse.Sources;
        }

        MessageReceived?.Invoke(this, new MessageEventArgs(response));
        return response;
    }

    private async IAsyncEnumerable<string> GenerateResponseAsync(string query, ChatContext context)
    {
        // Determine backend based on query type
        switch (context.QueryType)
        {
            case QueryType.Knowledge:
                await foreach (var chunk in _ragClient.QueryStreamAsync(query))
                    yield return chunk;
                break;

            case QueryType.Traffic:
                var decoded = await _rteClient.AnalyzeAsync(query, context.RecentTraffic);
                yield return FormatTrafficAnalysis(decoded);
                break;

            case QueryType.Hybrid:
                // Combine RAG + RTE + LLM
                var ragContext = await _ragClient.QueryAsync(query);
                var trafficContext = FormatTrafficContext(context.RecentTraffic);
                
                var prompt = BuildHybridPrompt(query, ragContext, trafficContext);
                await foreach (var chunk in _llmClient.StreamAsync(prompt))
                    yield return chunk;
                break;

            default:
                await foreach (var chunk in _llmClient.StreamAsync(query))
                    yield return chunk;
                break;
        }
    }
}
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut      | Action                  |
| ------------- | ----------------------- |
| `Enter`       | Send message            |
| `Shift+Enter` | New line in input       |
| `Ctrl+V`      | Paste (supports files)  |
| `Ctrl+L`      | Clear chat              |
| `Ctrl+S`      | Save chat history       |
| `Ctrl+U`      | Open file upload        |
| `Ctrl+D`      | Decode clipboard as AMF |
| `Escape`      | Cancel streaming        |

---

## 📱 Responsive Behavior

### Panel Resize

```csharp
public class ChatbotPanelResizer
{
    private const double MinWidth = 300;
    private const double MaxWidth = 600;
    private const double DefaultWidth = 400;

    public void OnWindowResize(double totalWidth)
    {
        // Auto-collapse on narrow windows
        if (totalWidth < 1200)
        {
            CollapseChatbot();
        }
        else
        {
            ExpandChatbot(Math.Min(MaxWidth, totalWidth * 0.3));
        }
    }
}
```

---

## 🎯 Context-Aware Features

### Auto-Suggestions

```csharp
public class AutoSuggestionService
{
    private readonly List<Suggestion> _suggestions = new()
    {
        // Traffic-based suggestions
        new Suggestion
        {
            Trigger = TriggerType.TrafficCaptured,
            Template = "I captured a {action} packet. Would you like me to decode it?",
            Action = "decode_last_packet"
        },
        
        // Error-based suggestions
        new Suggestion
        {
            Trigger = TriggerType.ErrorDetected,
            Template = "I detected an error in the game. Want me to analyze it?",
            Action = "analyze_error"
        },
        
        // Time-based suggestions
        new Suggestion
        {
            Trigger = TriggerType.IdleTime,
            Template = "Ready to help! Try asking about hero builds or march strategies.",
            Action = null
        }
    };

    public Suggestion GetSuggestion(ChatContext context)
    {
        // Return appropriate suggestion based on current context
        if (context.LastTrafficPacket != null && context.TimeSinceLastMessage > TimeSpan.FromSeconds(30))
        {
            return _suggestions.First(s => s.Trigger == TriggerType.TrafficCaptured);
        }
        
        return null;
    }
}
```

---

## 📚 Related Documentation

- [FEATURE-ROADMAP.md](./FEATURE-ROADMAP.md) - Overall feature roadmap
- [RAG-RTE-INTEGRATION.md](./RAG-RTE-INTEGRATION.md) - RAG/RTE backend details
- [CLI-TOOLS.md](./CLI-TOOLS.md) - CLI integration guide
