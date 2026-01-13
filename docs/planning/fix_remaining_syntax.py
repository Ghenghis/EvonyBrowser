#!/usr/bin/env python3
"""
Fix remaining .NET 6+ syntax issues that the first script missed.
"""

import os
import re
from pathlib import Path

def fix_lazy_lambda(content):
    """Fix Lazy<T> with lambda: new(() => ...) -> new Lazy<T>(() => ...)"""
    # Pattern: new(() => new ClassName(), ...)
    # This needs context to know the type, so we look for the variable declaration
    
    # Pattern: Lazy<ClassName> _var = new(() => new ClassName(), ...);
    pattern = r'(Lazy<(\w+)>\s+\w+\s*=\s*)new\(\(\)\s*=>\s*new\s+\2\(\)'
    content = re.sub(pattern, r'\1new Lazy<\2>(() => new \2()', content)
    
    # Also handle: private static readonly Lazy<ClassName> _instance = new(() => new ClassName());
    pattern = r'(Lazy<(\w+)>.*?=\s*)new\(\(\)\s*=>\s*new\s+\2\(\)\)'
    content = re.sub(pattern, r'\1new Lazy<\2>(() => new \2())', content)
    
    return content

def fix_property_new(content):
    """Fix property initializers: Type Prop { get; } = new(); -> = new Type();"""
    lines = content.split('\n')
    new_lines = []
    
    for line in lines:
        # Pattern: public TypeName PropName { get; set; } = new();
        # Or: public TypeName PropName { get; private set; } = new();
        match = re.search(r'public\s+([\w<>\[\],\s]+?)\s+(\w+)\s*\{[^}]+\}\s*=\s*new\(\);', line)
        if match:
            type_name = match.group(1).strip()
            # Get the actual type (last word if multiple)
            type_parts = type_name.split()
            actual_type = type_parts[-1] if type_parts else type_name
            line = line.replace('= new();', f'= new {actual_type}();')
        new_lines.append(line)
    
    return '\n'.join(new_lines)

def process_file(filepath):
    """Process a single file."""
    print(f"Processing: {filepath}")
    
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    original = content
    
    content = fix_lazy_lambda(content)
    content = fix_property_new(content)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Modified")
        return True
    return False

def main():
    base_dir = Path('/home/ubuntu/Svony-Browser/SvonyBrowser')
    
    # Files to fix
    files_to_fix = [
        'Services/AnalyticsDashboard.cs',
        'Services/AutoPilotService.cs',
        'Services/CdpConnectionService.cs',
        'Services/ChatbotService.cs',
        'Services/CombatSimulator.cs',
        'Services/ConnectionPool.cs',
        'Services/DebugService.cs',
        'Services/ErrorHandler.cs',
        'Services/ExportImportManager.cs',
        'Services/FailsafeManager.cs',
        'Services/GameStateEngine.cs',
        'Services/KeyboardShortcutManager.cs',
        'Services/MapScanner.cs',
        'Services/McpConnectionManager.cs',
        'Services/MemoryGuard.cs',
        'Services/MemoryManager.cs',
        'Services/MultiAccountOrchestrator.cs',
        'Services/PromptTemplateEngine.cs',
        'Services/ProtocolHandler.cs',
        'Services/ProxyMonitor.cs',
        'Services/RealDataProvider.cs',
        'Services/SessionManager.cs',
        'Services/SessionRecorder.cs',
        'Services/SettingsManager.cs',
        'Services/StatusBarManager.cs',
        'Services/StrategicAdvisor.cs',
        'Services/ThemeManager.cs',
        'Services/TrafficPipeClient.cs',
        'Services/WebhookHub.cs',
        'Stubs/CefSharpStub.cs',
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
