#!/usr/bin/env python3
"""
Comprehensive Best Practices Audit for Svony Browser
Checks 1:1 compliance with UI, State, Events, Validation, Error Handling,
Backend, Infrastructure, and QA best practices
"""
import os
import re
import json
from pathlib import Path
from collections import defaultdict

BASE_DIR = Path("SvonyBrowser")

def read_file(path):
    try:
        return path.read_text(encoding='utf-8', errors='ignore')
    except:
        return ""

def audit_ui_components():
    """Audit UI Components - Buttons, forms, displays, modals"""
    results = {
        "buttons": {"total": 0, "with_handlers": 0, "missing_handlers": []},
        "forms": {"total": 0, "with_validation": 0, "missing_validation": []},
        "displays": {"total": 0, "with_binding": 0, "missing_binding": []},
        "modals": {"total": 0, "with_close": 0, "missing_close": []},
        "textboxes": {"total": 0, "with_handlers": 0, "missing_handlers": []},
        "comboboxes": {"total": 0, "with_handlers": 0, "missing_handlers": []},
        "toggles": {"total": 0, "with_handlers": 0, "missing_handlers": []},
        "sliders": {"total": 0, "with_handlers": 0, "missing_handlers": []}
    }
    
    for xaml in BASE_DIR.rglob("*.xaml"):
        if 'obj' in str(xaml) or 'bin' in str(xaml):
            continue
        
        content = read_file(xaml)
        cs_file = xaml.with_suffix('.xaml.cs')
        cs_content = read_file(cs_file) if cs_file.exists() else ""
        
        # Buttons
        buttons = re.findall(r'<Button[^>]*(?:x:Name="(\w+)")?[^>]*(?:Click="(\w+)")?[^>]*>', content)
        for name, handler in buttons:
            results["buttons"]["total"] += 1
            if handler and handler in cs_content:
                results["buttons"]["with_handlers"] += 1
            else:
                results["buttons"]["missing_handlers"].append({
                    "file": xaml.name,
                    "name": name or "unnamed",
                    "handler": handler or "none"
                })
        
        # TextBoxes
        textboxes = re.findall(r'<TextBox[^>]*x:Name="(\w+)"[^>]*(?:TextChanged="(\w+)")?', content)
        for name, handler in textboxes:
            results["textboxes"]["total"] += 1
            if handler or f"{name}_TextChanged" in cs_content or f"PropertyChanged" in cs_content:
                results["textboxes"]["with_handlers"] += 1
            else:
                results["textboxes"]["missing_handlers"].append({
                    "file": xaml.name,
                    "name": name
                })
        
        # ComboBoxes
        combos = re.findall(r'<ComboBox[^>]*x:Name="(\w+)"[^>]*(?:SelectionChanged="(\w+)")?', content)
        for name, handler in combos:
            results["comboboxes"]["total"] += 1
            if handler or f"{name}_SelectionChanged" in cs_content:
                results["comboboxes"]["with_handlers"] += 1
            else:
                results["comboboxes"]["missing_handlers"].append({
                    "file": xaml.name,
                    "name": name
                })
        
        # ToggleSwitches/CheckBoxes
        toggles = re.findall(r'<(?:ToggleSwitch|CheckBox)[^>]*x:Name="(\w+)"[^>]*(?:(?:Checked|Toggled)="(\w+)")?', content)
        for name, handler in toggles:
            results["toggles"]["total"] += 1
            if handler or f"{name}_" in cs_content:
                results["toggles"]["with_handlers"] += 1
            else:
                results["toggles"]["missing_handlers"].append({
                    "file": xaml.name,
                    "name": name
                })
        
        # Sliders
        sliders = re.findall(r'<Slider[^>]*x:Name="(\w+)"[^>]*(?:ValueChanged="(\w+)")?', content)
        for name, handler in sliders:
            results["sliders"]["total"] += 1
            if handler or f"{name}_" in cs_content:
                results["sliders"]["with_handlers"] += 1
            else:
                results["sliders"]["missing_handlers"].append({
                    "file": xaml.name,
                    "name": name
                })
        
        # Modals/Windows
        if 'Window' in content and xaml.name != 'App.xaml':
            results["modals"]["total"] += 1
            if 'DialogResult' in cs_content or 'Close()' in cs_content:
                results["modals"]["with_close"] += 1
            else:
                results["modals"]["missing_close"].append(xaml.name)
    
    return results

def audit_state_management():
    """Audit State Management - Local/global state, caching"""
    results = {
        "singletons": {"total": 0, "properly_initialized": 0, "issues": []},
        "observable_properties": {"total": 0, "with_notify": 0, "missing_notify": []},
        "caching": {"implemented": False, "cache_services": []},
        "settings_persistence": {"implemented": False, "issues": []}
    }
    
    services_dir = BASE_DIR / "Services"
    if services_dir.exists():
        for cs_file in services_dir.glob("*.cs"):
            content = read_file(cs_file)
            
            # Check singleton pattern
            if 'Instance =>' in content or '_lazyInstance' in content:
                results["singletons"]["total"] += 1
                if 'Lazy<' in content or 'LazyThreadSafetyMode' in content:
                    results["singletons"]["properly_initialized"] += 1
                else:
                    results["singletons"]["issues"].append({
                        "file": cs_file.name,
                        "issue": "Not using thread-safe lazy initialization"
                    })
            
            # Check caching
            if 'Cache' in cs_file.name or '_cache' in content.lower():
                results["caching"]["implemented"] = True
                results["caching"]["cache_services"].append(cs_file.name)
    
    # Check INotifyPropertyChanged
    for cs_file in BASE_DIR.rglob("*.cs"):
        if 'obj' in str(cs_file) or 'bin' in str(cs_file):
            continue
        content = read_file(cs_file)
        
        # Count properties
        props = re.findall(r'public\s+\w+\s+(\w+)\s*{\s*get;?\s*set;?\s*}', content)
        for prop in props:
            results["observable_properties"]["total"] += 1
            if 'OnPropertyChanged' in content or 'PropertyChanged' in content:
                results["observable_properties"]["with_notify"] += 1
    
    # Check settings persistence
    settings_manager = BASE_DIR / "Services" / "SettingsManager.cs"
    if settings_manager.exists():
        content = read_file(settings_manager)
        if 'SaveAsync' in content and 'LoadAsync' in content:
            results["settings_persistence"]["implemented"] = True
        else:
            results["settings_persistence"]["issues"].append("Missing SaveAsync or LoadAsync")
    
    return results

def audit_event_handlers():
    """Audit Event Handlers - Click, submit, change handlers"""
    results = {
        "click_handlers": {"declared": 0, "implemented": 0, "missing": []},
        "change_handlers": {"declared": 0, "implemented": 0, "missing": []},
        "keyboard_handlers": {"declared": 0, "implemented": 0, "missing": []},
        "async_handlers": {"total": 0, "properly_async": 0, "sync_issues": []}
    }
    
    for xaml in BASE_DIR.rglob("*.xaml"):
        if 'obj' in str(xaml) or 'bin' in str(xaml):
            continue
        
        content = read_file(xaml)
        cs_file = xaml.with_suffix('.xaml.cs')
        cs_content = read_file(cs_file) if cs_file.exists() else ""
        
        # Click handlers
        clicks = re.findall(r'Click="(\w+)"', content)
        for handler in clicks:
            results["click_handlers"]["declared"] += 1
            if handler in cs_content:
                results["click_handlers"]["implemented"] += 1
                # Check if async
                if f'async void {handler}' in cs_content or f'async Task {handler}' in cs_content:
                    results["async_handlers"]["total"] += 1
                    results["async_handlers"]["properly_async"] += 1
            else:
                results["click_handlers"]["missing"].append({
                    "file": xaml.name,
                    "handler": handler
                })
        
        # Change handlers
        changes = re.findall(r'(?:SelectionChanged|TextChanged|ValueChanged|Checked|Unchecked)="(\w+)"', content)
        for handler in changes:
            results["change_handlers"]["declared"] += 1
            if handler in cs_content:
                results["change_handlers"]["implemented"] += 1
            else:
                results["change_handlers"]["missing"].append({
                    "file": xaml.name,
                    "handler": handler
                })
        
        # Keyboard handlers
        keys = re.findall(r'(?:KeyDown|KeyUp|PreviewKeyDown)="(\w+)"', content)
        for handler in keys:
            results["keyboard_handlers"]["declared"] += 1
            if handler in cs_content:
                results["keyboard_handlers"]["implemented"] += 1
            else:
                results["keyboard_handlers"]["missing"].append({
                    "file": xaml.name,
                    "handler": handler
                })
    
    return results

def audit_validation():
    """Audit Validation - Client-side and server-side input validation"""
    results = {
        "client_validation": {
            "textbox_validation": 0,
            "form_validation": 0,
            "missing_validation": []
        },
        "server_validation": {
            "input_sanitization": 0,
            "type_checking": 0,
            "missing_validation": []
        },
        "validation_messages": {
            "total": 0,
            "user_friendly": 0,
            "technical": []
        }
    }
    
    for cs_file in BASE_DIR.rglob("*.cs"):
        if 'obj' in str(cs_file) or 'bin' in str(cs_file):
            continue
        content = read_file(cs_file)
        
        # Client validation patterns
        if 'string.IsNullOrEmpty' in content or 'string.IsNullOrWhiteSpace' in content:
            results["client_validation"]["textbox_validation"] += 1
        
        if 'Validate' in content or 'IsValid' in content:
            results["client_validation"]["form_validation"] += 1
        
        # Server validation
        if 'TryParse' in content:
            results["server_validation"]["type_checking"] += 1
        
        if 'Regex.IsMatch' in content or 'Sanitize' in content:
            results["server_validation"]["input_sanitization"] += 1
        
        # Validation messages
        messages = re.findall(r'MessageBox\.Show\s*\(\s*"([^"]+)"', content)
        for msg in messages:
            results["validation_messages"]["total"] += 1
            if 'Error' in msg or 'Invalid' in msg or 'required' in msg.lower():
                results["validation_messages"]["user_friendly"] += 1
            else:
                results["validation_messages"]["technical"].append(msg[:50])
    
    return results

def audit_error_handling():
    """Audit Error Handling - User-friendly error messages"""
    results = {
        "try_catch_blocks": {"total": 0, "with_logging": 0, "silent_catches": []},
        "error_messages": {"total": 0, "user_friendly": 0, "technical": []},
        "exception_types": {"generic": 0, "specific": 0},
        "error_recovery": {"implemented": 0, "missing": []}
    }
    
    for cs_file in BASE_DIR.rglob("*.cs"):
        if 'obj' in str(cs_file) or 'bin' in str(cs_file):
            continue
        content = read_file(cs_file)
        
        # Count try-catch blocks
        try_catches = re.findall(r'try\s*{[^}]*}\s*catch\s*\((\w+)', content)
        for exc_type in try_catches:
            results["try_catch_blocks"]["total"] += 1
            
            if exc_type == 'Exception':
                results["exception_types"]["generic"] += 1
            else:
                results["exception_types"]["specific"] += 1
        
        # Check for logging in catch blocks
        if 'catch' in content and ('Logger' in content or 'Log.' in content):
            results["try_catch_blocks"]["with_logging"] += 1
        
        # Silent catches (empty catch blocks)
        silent = re.findall(r'catch\s*\([^)]*\)\s*{\s*}', content)
        if silent:
            results["try_catch_blocks"]["silent_catches"].append({
                "file": cs_file.name,
                "count": len(silent)
            })
        
        # Error messages
        error_msgs = re.findall(r'(?:MessageBox\.Show|throw new \w+Exception)\s*\(\s*"([^"]+)"', content)
        for msg in error_msgs:
            results["error_messages"]["total"] += 1
            if any(word in msg.lower() for word in ['please', 'try', 'unable', 'failed', 'error']):
                results["error_messages"]["user_friendly"] += 1
    
    return results

def audit_backend_services():
    """Audit Backend Components - Services, business logic, data access"""
    results = {
        "services": {
            "total": 0,
            "with_interface": 0,
            "with_disposal": 0,
            "missing_interface": [],
            "missing_disposal": []
        },
        "business_logic": {
            "async_methods": 0,
            "sync_methods": 0,
            "documented_methods": 0
        },
        "data_access": {
            "file_operations": 0,
            "http_operations": 0,
            "database_operations": 0
        },
        "middleware": {
            "logging": False,
            "auth": False,
            "rate_limiting": False
        }
    }
    
    services_dir = BASE_DIR / "Services"
    if services_dir.exists():
        for cs_file in services_dir.glob("*.cs"):
            content = read_file(cs_file)
            results["services"]["total"] += 1
            
            # Check interface
            if ': I' in content and 'interface' not in content:
                results["services"]["with_interface"] += 1
            else:
                results["services"]["missing_interface"].append(cs_file.name)
            
            # Check disposal
            if 'IDisposable' in content and 'Dispose()' in content:
                results["services"]["with_disposal"] += 1
            else:
                results["services"]["missing_disposal"].append(cs_file.name)
            
            # Count methods
            async_methods = len(re.findall(r'public\s+async\s+Task', content))
            sync_methods = len(re.findall(r'public\s+(?!async)\w+\s+\w+\s*\(', content))
            documented = len(re.findall(r'/// <summary>', content))
            
            results["business_logic"]["async_methods"] += async_methods
            results["business_logic"]["sync_methods"] += sync_methods
            results["business_logic"]["documented_methods"] += documented
            
            # Data access patterns
            if 'File.' in content or 'FileEx.' in content:
                results["data_access"]["file_operations"] += 1
            if 'HttpClient' in content or 'WebSocket' in content:
                results["data_access"]["http_operations"] += 1
            
            # Middleware
            if 'Logger' in content or 'Serilog' in content:
                results["middleware"]["logging"] = True
    
    return results

def audit_infrastructure():
    """Audit Infrastructure - Config, auth, logging, caching"""
    results = {
        "configuration": {
            "settings_file": False,
            "env_variables": False,
            "config_validation": False
        },
        "authentication": {
            "implemented": False,
            "session_management": False
        },
        "logging": {
            "implemented": False,
            "log_levels": [],
            "log_rotation": False
        },
        "caching": {
            "implemented": False,
            "cache_invalidation": False
        }
    }
    
    # Check for settings
    settings_file = BASE_DIR / "Models" / "AppSettings.cs"
    if settings_file.exists():
        results["configuration"]["settings_file"] = True
    
    # Check for logging
    for cs_file in BASE_DIR.rglob("*.cs"):
        if 'obj' in str(cs_file):
            continue
        content = read_file(cs_file)
        
        if 'Serilog' in content or 'ILogger' in content:
            results["logging"]["implemented"] = True
        
        if 'Information' in content:
            if 'Information' not in results["logging"]["log_levels"]:
                results["logging"]["log_levels"].append('Information')
        if 'Warning' in content:
            if 'Warning' not in results["logging"]["log_levels"]:
                results["logging"]["log_levels"].append('Warning')
        if 'Error' in content:
            if 'Error' not in results["logging"]["log_levels"]:
                results["logging"]["log_levels"].append('Error')
        if 'Debug' in content:
            if 'Debug' not in results["logging"]["log_levels"]:
                results["logging"]["log_levels"].append('Debug')
        
        if 'SessionManager' in cs_file.name:
            results["authentication"]["session_management"] = True
    
    return results

def audit_quality_assurance():
    """Audit Quality Assurance - Tests, documentation, error recovery"""
    results = {
        "unit_tests": {
            "test_files": 0,
            "test_methods": 0,
            "coverage_estimate": "0%"
        },
        "integration_tests": {
            "test_files": 0,
            "test_methods": 0
        },
        "documentation": {
            "xml_comments": 0,
            "readme_files": 0,
            "api_docs": False
        },
        "error_recovery": {
            "retry_logic": 0,
            "fallback_handlers": 0,
            "graceful_degradation": 0
        }
    }
    
    # Count XML documentation
    for cs_file in BASE_DIR.rglob("*.cs"):
        if 'obj' in str(cs_file):
            continue
        content = read_file(cs_file)
        
        xml_comments = len(re.findall(r'/// <summary>', content))
        results["documentation"]["xml_comments"] += xml_comments
        
        # Error recovery patterns
        if 'retry' in content.lower() or 'Retry' in content:
            results["error_recovery"]["retry_logic"] += 1
        if 'fallback' in content.lower() or 'Fallback' in content:
            results["error_recovery"]["fallback_handlers"] += 1
    
    # Check for test files
    test_dir = Path("SvonyBrowser.Tests")
    if test_dir.exists():
        results["unit_tests"]["test_files"] = len(list(test_dir.rglob("*Test*.cs")))
    
    # Check for README
    readme_count = len(list(Path(".").glob("*.md")))
    results["documentation"]["readme_files"] = readme_count
    
    return results

def main():
    print("=" * 60)
    print("SVONY BROWSER - BEST PRACTICES COMPLIANCE AUDIT")
    print("=" * 60)
    
    audit_results = {
        "ui_components": audit_ui_components(),
        "state_management": audit_state_management(),
        "event_handlers": audit_event_handlers(),
        "validation": audit_validation(),
        "error_handling": audit_error_handling(),
        "backend_services": audit_backend_services(),
        "infrastructure": audit_infrastructure(),
        "quality_assurance": audit_quality_assurance()
    }
    
    # Save full results
    with open('best_practices_audit.json', 'w') as f:
        json.dump(audit_results, f, indent=2, default=str)
    
    # Print summary
    print("\n" + "=" * 60)
    print("AUDIT SUMMARY")
    print("=" * 60)
    
    # UI Components
    ui = audit_results["ui_components"]
    print(f"\n📱 UI COMPONENTS:")
    print(f"   Buttons: {ui['buttons']['with_handlers']}/{ui['buttons']['total']} wired")
    print(f"   TextBoxes: {ui['textboxes']['with_handlers']}/{ui['textboxes']['total']} wired")
    print(f"   ComboBoxes: {ui['comboboxes']['with_handlers']}/{ui['comboboxes']['total']} wired")
    print(f"   Toggles: {ui['toggles']['with_handlers']}/{ui['toggles']['total']} wired")
    print(f"   Sliders: {ui['sliders']['with_handlers']}/{ui['sliders']['total']} wired")
    
    # State Management
    state = audit_results["state_management"]
    print(f"\n🔄 STATE MANAGEMENT:")
    print(f"   Singletons: {state['singletons']['properly_initialized']}/{state['singletons']['total']} thread-safe")
    print(f"   Caching: {'✅' if state['caching']['implemented'] else '❌'}")
    print(f"   Settings Persistence: {'✅' if state['settings_persistence']['implemented'] else '❌'}")
    
    # Event Handlers
    events = audit_results["event_handlers"]
    print(f"\n🎯 EVENT HANDLERS:")
    print(f"   Click: {events['click_handlers']['implemented']}/{events['click_handlers']['declared']} implemented")
    print(f"   Change: {events['change_handlers']['implemented']}/{events['change_handlers']['declared']} implemented")
    print(f"   Keyboard: {events['keyboard_handlers']['implemented']}/{events['keyboard_handlers']['declared']} implemented")
    
    # Validation
    val = audit_results["validation"]
    print(f"\n✅ VALIDATION:")
    print(f"   Client-side: {val['client_validation']['form_validation']} forms validated")
    print(f"   Server-side: {val['server_validation']['type_checking']} type checks")
    print(f"   User-friendly messages: {val['validation_messages']['user_friendly']}/{val['validation_messages']['total']}")
    
    # Error Handling
    err = audit_results["error_handling"]
    print(f"\n⚠️ ERROR HANDLING:")
    print(f"   Try-catch blocks: {err['try_catch_blocks']['total']}")
    print(f"   With logging: {err['try_catch_blocks']['with_logging']}")
    print(f"   Silent catches: {len(err['try_catch_blocks']['silent_catches'])}")
    print(f"   Specific exceptions: {err['exception_types']['specific']}/{err['exception_types']['generic'] + err['exception_types']['specific']}")
    
    # Backend Services
    backend = audit_results["backend_services"]
    print(f"\n🔧 BACKEND SERVICES:")
    print(f"   Services: {backend['services']['total']}")
    print(f"   With interfaces: {backend['services']['with_interface']}")
    print(f"   With disposal: {backend['services']['with_disposal']}")
    print(f"   Async methods: {backend['business_logic']['async_methods']}")
    print(f"   Documented methods: {backend['business_logic']['documented_methods']}")
    
    # Infrastructure
    infra = audit_results["infrastructure"]
    print(f"\n🏗️ INFRASTRUCTURE:")
    print(f"   Settings file: {'✅' if infra['configuration']['settings_file'] else '❌'}")
    print(f"   Logging: {'✅' if infra['logging']['implemented'] else '❌'}")
    print(f"   Log levels: {', '.join(infra['logging']['log_levels'])}")
    print(f"   Session management: {'✅' if infra['authentication']['session_management'] else '❌'}")
    
    # Quality Assurance
    qa = audit_results["quality_assurance"]
    print(f"\n🧪 QUALITY ASSURANCE:")
    print(f"   Test files: {qa['unit_tests']['test_files']}")
    print(f"   XML documentation: {qa['documentation']['xml_comments']} comments")
    print(f"   README files: {qa['documentation']['readme_files']}")
    print(f"   Retry logic: {qa['error_recovery']['retry_logic']} implementations")
    
    print("\n" + "=" * 60)
    print("Full results saved to best_practices_audit.json")
    print("=" * 60)

if __name__ == '__main__':
    main()
