#!/usr/bin/env python3
"""Extract all features and their wiring status from Svony Browser"""
import os
import re
import json
from pathlib import Path

BASE_DIR = Path("SvonyBrowser")

def extract_all_features():
    features = {
        "gui_features": [],
        "service_features": [],
        "mcp_tools": [],
        "settings": [],
        "keyboard_shortcuts": [],
        "menu_items": [],
        "status_indicators": [],
        "wiring_issues": []
    }
    
    # 1. Extract GUI features from MainWindow.xaml.cs
    main_window = BASE_DIR / "MainWindow.xaml.cs"
    if main_window.exists():
        content = main_window.read_text(encoding='utf-8', errors='ignore')
        
        # Click handlers
        for match in re.finditer(r'private\s+(?:async\s+)?void\s+(\w+)_Click\s*\(', content):
            features["gui_features"].append({
                "name": match.group(1),
                "type": "button_click",
                "file": "MainWindow.xaml.cs"
            })
        
        # Keyboard shortcuts
        for match in re.finditer(r'case Key\.(\w+):', content):
            features["keyboard_shortcuts"].append(match.group(1))
    
    # 2. Extract service features
    services_dir = BASE_DIR / "Services"
    if services_dir.exists():
        for cs_file in services_dir.glob("*.cs"):
            content = cs_file.read_text(encoding='utf-8', errors='ignore')
            
            # Public async methods (main features)
            for match in re.finditer(r'public\s+async\s+Task[<\w>]*\s+(\w+)Async\s*\(', content):
                features["service_features"].append({
                    "name": match.group(1),
                    "service": cs_file.stem,
                    "type": "async_method"
                })
            
            # MCP tool calls
            for match in re.finditer(r'CallToolAsync\s*\(\s*"([^"]+)"\s*,\s*"([^"]+)"', content):
                features["mcp_tools"].append({
                    "server": match.group(1),
                    "tool": match.group(2),
                    "file": cs_file.stem
                })
    
    # 3. Extract settings from SettingsWindow.xaml
    settings_xaml = BASE_DIR / "SettingsWindow.xaml"
    if settings_xaml.exists():
        content = settings_xaml.read_text(encoding='utf-8', errors='ignore')
        
        for match in re.finditer(r'x:Name="(\w+(?:Text|Check|Combo))"', content):
            features["settings"].append(match.group(1))
    
    # 4. Extract settings from SettingsControlCenter.xaml
    settings_cc = BASE_DIR / "SettingsControlCenter.xaml"
    if settings_cc.exists():
        content = settings_cc.read_text(encoding='utf-8', errors='ignore')
        
        for match in re.finditer(r'x:Name="(\w+)"', content):
            features["settings"].append(match.group(1))
    
    # 5. Extract status indicators
    for xaml in BASE_DIR.rglob("*.xaml"):
        if 'obj' in str(xaml):
            continue
        content = xaml.read_text(encoding='utf-8', errors='ignore')
        
        for match in re.finditer(r'x:Name="(\w*(?:Status|Indicator|Count)\w*)"', content):
            features["status_indicators"].append({
                "name": match.group(1),
                "file": xaml.name
            })
    
    # 6. Check for wiring issues
    # Find handlers declared in XAML but not implemented in code-behind
    for xaml in BASE_DIR.rglob("*.xaml"):
        if 'obj' in str(xaml):
            continue
        
        xaml_content = xaml.read_text(encoding='utf-8', errors='ignore')
        cs_file = xaml.with_suffix('.xaml.cs')
        
        if cs_file.exists():
            cs_content = cs_file.read_text(encoding='utf-8', errors='ignore')
            
            # Find Click handlers in XAML
            for match in re.finditer(r'Click="(\w+)"', xaml_content):
                handler = match.group(1)
                if handler not in cs_content:
                    features["wiring_issues"].append({
                        "type": "missing_handler",
                        "handler": handler,
                        "xaml": xaml.name,
                        "cs": cs_file.name
                    })
    
    return features

def main():
    features = extract_all_features()
    
    # Save to JSON
    with open('features_inventory.json', 'w') as f:
        json.dump(features, f, indent=2)
    
    # Print summary
    print("=== FEATURE INVENTORY ===")
    print(f"GUI Features: {len(features['gui_features'])}")
    print(f"Service Features: {len(features['service_features'])}")
    print(f"MCP Tools: {len(features['mcp_tools'])}")
    print(f"Settings: {len(features['settings'])}")
    print(f"Keyboard Shortcuts: {len(features['keyboard_shortcuts'])}")
    print(f"Status Indicators: {len(features['status_indicators'])}")
    print(f"Wiring Issues: {len(features['wiring_issues'])}")
    
    total = (len(features['gui_features']) + len(features['service_features']) + 
             len(features['mcp_tools']) + len(features['settings']) + 
             len(features['keyboard_shortcuts']) + len(features['status_indicators']))
    print(f"\nTOTAL FEATURES: {total}")
    
    if features['wiring_issues']:
        print("\n=== WIRING ISSUES ===")
        for issue in features['wiring_issues']:
            print(f"  - {issue['type']}: {issue['handler']} in {issue['xaml']}")

if __name__ == '__main__':
    main()
