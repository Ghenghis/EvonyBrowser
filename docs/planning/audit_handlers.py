import os
import re
from collections import defaultdict

def find_xaml_handlers(xaml_file):
    """Extract all event handlers from XAML file"""
    handlers = []
    with open(xaml_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # Find Click, Checked, Unchecked, ValueChanged, SelectionChanged, TextChanged handlers
    patterns = [
        (r'Click="([^"]+)"', 'Click'),
        (r'Checked="([^"]+)"', 'Checked'),
        (r'Unchecked="([^"]+)"', 'Unchecked'),
        (r'ValueChanged="([^"]+)"', 'ValueChanged'),
        (r'SelectionChanged="([^"]+)"', 'SelectionChanged'),
        (r'TextChanged="([^"]+)"', 'TextChanged'),
        (r'KeyDown="([^"]+)"', 'KeyDown'),
        (r'KeyUp="([^"]+)"', 'KeyUp'),
        (r'MouseDown="([^"]+)"', 'MouseDown'),
        (r'MouseUp="([^"]+)"', 'MouseUp'),
        (r'Loaded="([^"]+)"', 'Loaded'),
        (r'Closing="([^"]+)"', 'Closing'),
    ]
    
    for pattern, event_type in patterns:
        for match in re.finditer(pattern, content):
            handler = match.group(1)
            if handler not in ['True', 'False']:  # Skip boolean values
                handlers.append((handler, event_type))
    
    return handlers

def find_cs_handlers(cs_file):
    """Extract all implemented handlers from code-behind"""
    handlers = set()
    with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # Find method definitions that look like event handlers
    pattern = r'(?:private|public|protected)\s+(?:async\s+)?void\s+(\w+)\s*\('
    for match in re.finditer(pattern, content):
        handlers.add(match.group(1))
    
    return handlers

def audit_xaml_cs_pair(xaml_path, cs_path):
    """Audit a XAML/CS pair for missing handlers"""
    xaml_handlers = find_xaml_handlers(xaml_path)
    cs_handlers = find_cs_handlers(cs_path)
    
    missing = []
    implemented = []
    
    for handler, event_type in xaml_handlers:
        if handler in cs_handlers:
            implemented.append((handler, event_type))
        else:
            missing.append((handler, event_type))
    
    return implemented, missing

# Main audit
xaml_files = [
    'SvonyBrowser/MainWindow.xaml',
    'SvonyBrowser/SettingsWindow.xaml',
    'SvonyBrowser/SettingsControlCenter.xaml',
    'SvonyBrowser/Controls/ChatbotPanel.xaml',
    'SvonyBrowser/Controls/TrafficViewer.xaml',
    'SvonyBrowser/Controls/ProtocolExplorer.xaml',
    'SvonyBrowser/Controls/StatusBar.xaml',
    'SvonyBrowser/Controls/StatusBarV4.xaml',
]

print("=" * 80)
print("SVONY BROWSER - COMPREHENSIVE HANDLER AUDIT")
print("=" * 80)

total_implemented = 0
total_missing = 0
all_missing = []

for xaml in xaml_files:
    cs = xaml.replace('.xaml', '.xaml.cs')
    if not os.path.exists(xaml) or not os.path.exists(cs):
        print(f"\nSkipping {xaml} - file not found")
        continue
    
    implemented, missing = audit_xaml_cs_pair(xaml, cs)
    total_implemented += len(implemented)
    total_missing += len(missing)
    
    print(f"\n{xaml}")
    print("-" * 60)
    print(f"  Implemented: {len(implemented)}")
    print(f"  Missing: {len(missing)}")
    
    if missing:
        print(f"  MISSING HANDLERS:")
        for handler, event_type in missing:
            print(f"    - {handler} ({event_type})")
            all_missing.append((xaml, handler, event_type))

print("\n" + "=" * 80)
print(f"SUMMARY: {total_implemented} implemented, {total_missing} missing")
print("=" * 80)

if all_missing:
    print("\nALL MISSING HANDLERS:")
    for xaml, handler, event_type in all_missing:
        print(f"  {os.path.basename(xaml)}: {handler} ({event_type})")
