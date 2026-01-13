#!/usr/bin/env python3
"""Analyze wiring issues and incomplete features in Svony Browser"""
import os
import re
import json
from pathlib import Path
from collections import defaultdict

BASE_DIR = Path("SvonyBrowser")

def analyze_wiring():
    issues = {
        "missing_implementations": [],
        "disconnected_handlers": [],
        "uninitialized_services": [],
        "missing_mcp_connections": [],
        "incomplete_settings": [],
        "status_bar_issues": [],
        "fiddler_issues": [],
        "chatbot_issues": [],
        "browser_issues": []
    }
    
    # 1. Check for services not being initialized in App.xaml.cs
    app_cs = BASE_DIR / "App.xaml.cs"
    if app_cs.exists():
        app_content = app_cs.read_text(encoding='utf-8', errors='ignore')
        
        services_dir = BASE_DIR / "Services"
        for cs_file in services_dir.glob("*.cs"):
            service_name = cs_file.stem
            # Check if service has Instance property
            content = cs_file.read_text(encoding='utf-8', errors='ignore')
            if 'Instance =>' in content or '_lazyInstance' in content:
                # Check if it's referenced in App.xaml.cs
                if service_name not in app_content:
                    issues["uninitialized_services"].append({
                        "service": service_name,
                        "reason": "Not referenced in App.xaml.cs"
                    })
    
    # 2. Check MCP connections
    mcp_manager = BASE_DIR / "Services" / "McpConnectionManager.cs"
    if mcp_manager.exists():
        content = mcp_manager.read_text(encoding='utf-8', errors='ignore')
        
        # Find configured MCP servers
        mcp_servers = re.findall(r'"(evony-\w+)"', content)
        unique_servers = list(set(mcp_servers))
        
        # Check if connection logic is complete
        if 'WebSocket' not in content and 'TcpClient' not in content and 'HttpClient' not in content:
            issues["missing_mcp_connections"].append({
                "issue": "No actual network connection implementation found",
                "servers": unique_servers
            })
    
    # 3. Check FiddlerBridge implementation
    fiddler = BASE_DIR / "Services" / "FiddlerBridge.cs"
    if fiddler.exists():
        content = fiddler.read_text(encoding='utf-8', errors='ignore')
        
        if 'NamedPipeServerStream' in content:
            # Check if pipe name matches Fiddler extension
            if 'SvonyBrowserPipe' not in content:
                issues["fiddler_issues"].append({
                    "issue": "Pipe name may not match Fiddler extension",
                    "suggestion": "Verify pipe name matches FiddlerScript"
                })
        
        if 'FiddlerApplication' not in content:
            issues["fiddler_issues"].append({
                "issue": "No FiddlerCore integration found",
                "suggestion": "Consider using FiddlerCore for direct integration"
            })
    
    # 4. Check ChatbotService RAG integration
    chatbot = BASE_DIR / "Services" / "ChatbotService.cs"
    if chatbot.exists():
        content = chatbot.read_text(encoding='utf-8', errors='ignore')
        
        if 'evony-rag' in content:
            # Check if RAG queries are properly handled
            if 'evony_query' not in content and 'evony_search' not in content:
                issues["chatbot_issues"].append({
                    "issue": "RAG tool calls may not be properly configured",
                    "suggestion": "Verify evony-rag tool names match MCP server"
                })
    
    # 5. Check settings wiring
    settings_cs = BASE_DIR / "SettingsWindow.xaml.cs"
    settings_cc_cs = BASE_DIR / "SettingsControlCenter.xaml.cs"
    
    if settings_cs.exists():
        content = settings_cs.read_text(encoding='utf-8', errors='ignore')
        
        # Check if settings are being saved
        if 'SettingsManager' not in content:
            issues["incomplete_settings"].append({
                "file": "SettingsWindow.xaml.cs",
                "issue": "SettingsManager not used for persistence"
            })
    
    if settings_cc_cs.exists():
        content = settings_cc_cs.read_text(encoding='utf-8', errors='ignore')
        
        # Count toggle bindings vs actual save operations
        toggles = len(re.findall(r'Toggle_Checked|Toggle_Unchecked', content))
        saves = len(re.findall(r'SaveSettings|Save\(', content))
        
        if toggles > 0 and saves == 0:
            issues["incomplete_settings"].append({
                "file": "SettingsControlCenter.xaml.cs",
                "issue": f"Found {toggles} toggle handlers but no save operations"
            })
    
    # 6. Check StatusBar wiring
    for status_file in ['StatusBar.xaml.cs', 'StatusBarV4.xaml.cs']:
        status_cs = BASE_DIR / "Controls" / status_file
        if status_cs.exists():
            content = status_cs.read_text(encoding='utf-8', errors='ignore')
            
            # Check if status updates are wired
            if 'McpConnectionManager' not in content:
                issues["status_bar_issues"].append({
                    "file": status_file,
                    "issue": "Not connected to McpConnectionManager"
                })
            
            if 'FiddlerBridge' not in content:
                issues["status_bar_issues"].append({
                    "file": status_file,
                    "issue": "Not connected to FiddlerBridge"
                })
    
    # 7. Check browser initialization
    main_window = BASE_DIR / "MainWindow.xaml.cs"
    if main_window.exists():
        content = main_window.read_text(encoding='utf-8', errors='ignore')
        
        if 'CefSharp' in content:
            if 'Cef.Initialize' not in content and 'CefSettings' not in content:
                issues["browser_issues"].append({
                    "issue": "CefSharp initialization may be incomplete",
                    "suggestion": "Check Program.cs for Cef.Initialize"
                })
    
    return issues

def main():
    issues = analyze_wiring()
    
    # Save to JSON
    with open('wiring_issues.json', 'w') as f:
        json.dump(issues, f, indent=2)
    
    # Print summary
    print("=== WIRING ANALYSIS ===\n")
    
    total_issues = 0
    for category, items in issues.items():
        if items:
            print(f"\n{category.upper().replace('_', ' ')}:")
            for item in items:
                print(f"  - {item}")
                total_issues += 1
    
    print(f"\n\nTOTAL WIRING ISSUES: {total_issues}")

if __name__ == '__main__':
    main()
