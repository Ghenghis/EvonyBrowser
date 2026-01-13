#!/usr/bin/env python3
"""
Fix .NET 6+ API calls to use .NET Framework 4.6.2 compatible alternatives.
"""

import os
import re
from pathlib import Path

def fix_file_async_methods(content):
    """Replace File.WriteAllTextAsync/ReadAllTextAsync with FileEx versions."""
    # Add using statement if needed
    if 'File.WriteAllTextAsync' in content or 'File.ReadAllTextAsync' in content:
        if 'using SvonyBrowser.Helpers;' not in content:
            # Add using after the last using statement
            lines = content.split('\n')
            last_using_idx = -1
            for i, line in enumerate(lines):
                if line.strip().startswith('using ') and line.strip().endswith(';'):
                    last_using_idx = i
            if last_using_idx >= 0:
                lines.insert(last_using_idx + 1, '    using SvonyBrowser.Helpers;')
                content = '\n'.join(lines)
    
    # Replace the method calls
    content = content.replace('File.WriteAllTextAsync', 'FileEx.WriteAllTextAsync')
    content = content.replace('File.ReadAllTextAsync', 'FileEx.ReadAllTextAsync')
    
    return content

def fix_math_clamp(content):
    """Replace Math.Clamp with MathEx.Clamp."""
    if 'Math.Clamp' in content:
        if 'using SvonyBrowser.Helpers;' not in content:
            lines = content.split('\n')
            last_using_idx = -1
            for i, line in enumerate(lines):
                if line.strip().startswith('using ') and line.strip().endswith(';'):
                    last_using_idx = i
            if last_using_idx >= 0:
                lines.insert(last_using_idx + 1, '    using SvonyBrowser.Helpers;')
                content = '\n'.join(lines)
    
    content = content.replace('Math.Clamp', 'MathEx.Clamp')
    return content

def fix_sha256_hashdata(content):
    """Replace SHA256.HashData with HashEx.SHA256."""
    if 'SHA256.HashData' in content:
        if 'using SvonyBrowser.Helpers;' not in content:
            lines = content.split('\n')
            last_using_idx = -1
            for i, line in enumerate(lines):
                if line.strip().startswith('using ') and line.strip().endswith(';'):
                    last_using_idx = i
            if last_using_idx >= 0:
                lines.insert(last_using_idx + 1, '    using SvonyBrowser.Helpers;')
                content = '\n'.join(lines)
    
    content = content.replace('SHA256.HashData', 'HashEx.SHA256')
    return content

def fix_sockets_http_handler(content):
    """Replace SocketsHttpHandler with HttpClientHandler."""
    content = content.replace('SocketsHttpHandler', 'HttpClientHandler')
    # Remove PooledConnectionLifetime as it doesn't exist in HttpClientHandler
    content = re.sub(r',?\s*PooledConnectionLifetime\s*=\s*[^,}]+', '', content)
    return content

def fix_wait_async(content):
    """Replace task.WaitAsync with extension method."""
    # This is trickier - WaitAsync is an extension method in our helpers
    # The pattern is: await task.WaitAsync(timeout) or await task.WaitAsync(token)
    # Our extension handles this, so we just need the using statement
    if '.WaitAsync(' in content:
        if 'using SvonyBrowser.Helpers;' not in content:
            lines = content.split('\n')
            last_using_idx = -1
            for i, line in enumerate(lines):
                if line.strip().startswith('using ') and line.strip().endswith(';'):
                    last_using_idx = i
            if last_using_idx >= 0:
                lines.insert(last_using_idx + 1, '    using SvonyBrowser.Helpers;')
                content = '\n'.join(lines)
    return content

def fix_get_value_or_default(content):
    """Ensure GetValueOrDefault extension is available."""
    if '.GetValueOrDefault(' in content:
        if 'using SvonyBrowser.Helpers;' not in content:
            lines = content.split('\n')
            last_using_idx = -1
            for i, line in enumerate(lines):
                if line.strip().startswith('using ') and line.strip().endswith(';'):
                    last_using_idx = i
            if last_using_idx >= 0:
                lines.insert(last_using_idx + 1, '    using SvonyBrowser.Helpers;')
                content = '\n'.join(lines)
    return content

def fix_take_last_skip_last(content):
    """Ensure TakeLast/SkipLast extensions are available."""
    if '.TakeLast(' in content or '.SkipLast(' in content:
        if 'using SvonyBrowser.Helpers;' not in content:
            lines = content.split('\n')
            last_using_idx = -1
            for i, line in enumerate(lines):
                if line.strip().startswith('using ') and line.strip().endswith(';'):
                    last_using_idx = i
            if last_using_idx >= 0:
                lines.insert(last_using_idx + 1, '    using SvonyBrowser.Helpers;')
                content = '\n'.join(lines)
    return content

def process_file(filepath):
    """Process a single file."""
    print(f"Processing: {filepath}")
    
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    original = content
    
    content = fix_file_async_methods(content)
    content = fix_math_clamp(content)
    content = fix_sha256_hashdata(content)
    content = fix_sockets_http_handler(content)
    content = fix_wait_async(content)
    content = fix_get_value_or_default(content)
    content = fix_take_last_skip_last(content)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Modified")
        return True
    print(f"  No changes")
    return False

def main():
    base_dir = Path('/home/ubuntu/Svony-Browser/SvonyBrowser')
    
    # Files that need API fixes based on grep results
    files_to_fix = [
        'Services/AnalyticsDashboard.cs',
        'Services/AutoPilotService.cs',
        'Services/CombatSimulator.cs',
        'Services/ExportImportManager.cs',
        'Services/FailsafeManager.cs',
        'Services/GameStateEngine.cs',
        'Services/LlmIntegrationService.cs',
        'Services/McpConnectionManager.cs',
        'Services/PacketAnalysisEngine.cs',
        'Services/PromptTemplateEngine.cs',
        'Services/ProtocolFuzzer.cs',
        'Services/ProtocolHandler.cs',
        'Services/RealDataProvider.cs',
        'Services/SessionRecorder.cs',
        'Services/SettingsManager.cs',
        'Services/StrategicAdvisor.cs',
        'Controls/StatusBarV4.xaml.cs',
        'Services/ConnectionPool.cs',
        'Services/DebugService.cs',
        'Services/ErrorHandler.cs',
    ]
    
    modified = 0
    for f in files_to_fix:
        filepath = base_dir / f
        if filepath.exists():
            if process_file(filepath):
                modified += 1
    
    print(f"\nModified {modified} files")

if __name__ == '__main__':
    main()
