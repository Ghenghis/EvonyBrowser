#!/usr/bin/env python3
"""
Comprehensive Svony Browser Feature Audit Script
Analyzes all GUI elements, services, and features
"""
import os
import re
import json
from pathlib import Path
from collections import defaultdict

BASE_DIR = Path("SvonyBrowser")

def find_all_files(pattern):
    return list(BASE_DIR.rglob(pattern))

def extract_xaml_elements(xaml_file):
    """Extract all named elements and event handlers from XAML"""
    elements = {
        'named_elements': [],
        'click_handlers': [],
        'other_handlers': [],
        'buttons': [],
        'textboxes': [],
        'comboboxes': [],
        'checkboxes': [],
        'toggles': [],
        'menus': [],
        'tabs': []
    }
    
    content = xaml_file.read_text(encoding='utf-8', errors='ignore')
    
    # Named elements
    for match in re.finditer(r'x:Name="([^"]+)"', content):
        elements['named_elements'].append(match.group(1))
    
    # Click handlers
    for match in re.finditer(r'Click="([^"]+)"', content):
        elements['click_handlers'].append(match.group(1))
    
    # Other event handlers
    for match in re.finditer(r'(SelectionChanged|TextChanged|Checked|Unchecked|KeyDown|Loaded|MouseLeftButtonDown)="([^"]+)"', content):
        elements['other_handlers'].append((match.group(1), match.group(2)))
    
    # Buttons
    for match in re.finditer(r'<Button[^>]*Content="([^"]*)"', content):
        elements['buttons'].append(match.group(1))
    
    # TextBoxes
    for match in re.finditer(r'<TextBox[^>]*x:Name="([^"]+)"', content):
        elements['textboxes'].append(match.group(1))
    
    # ComboBoxes
    for match in re.finditer(r'<ComboBox[^>]*x:Name="([^"]+)"', content):
        elements['comboboxes'].append(match.group(1))
    
    # CheckBoxes
    for match in re.finditer(r'<CheckBox[^>]*x:Name="([^"]+)"', content):
        elements['checkboxes'].append(match.group(1))
    
    return elements

def extract_cs_methods(cs_file):
    """Extract all methods from C# file"""
    methods = {
        'public_methods': [],
        'private_methods': [],
        'async_methods': [],
        'event_handlers': [],
        'stub_methods': [],
        'todo_items': []
    }
    
    content = cs_file.read_text(encoding='utf-8', errors='ignore')
    
    # Public methods
    for match in re.finditer(r'public\s+(?:async\s+)?(?:Task[<\w>]*|void|\w+)\s+(\w+)\s*\(', content):
        methods['public_methods'].append(match.group(1))
    
    # Private methods
    for match in re.finditer(r'private\s+(?:async\s+)?(?:Task[<\w>]*|void|\w+)\s+(\w+)\s*\(', content):
        methods['private_methods'].append(match.group(1))
    
    # Async methods
    for match in re.finditer(r'async\s+(?:Task[<\w>]*|void)\s+(\w+)\s*\(', content):
        methods['async_methods'].append(match.group(1))
    
    # Event handlers (methods ending with _Click, _Changed, etc.)
    for match in re.finditer(r'(?:private|public)\s+(?:async\s+)?void\s+(\w+_(?:Click|Changed|Loaded|KeyDown|Checked|Unchecked))\s*\(', content):
        methods['event_handlers'].append(match.group(1))
    
    # Stub/NotImplemented
    for match in re.finditer(r'throw new NotImplementedException', content):
        methods['stub_methods'].append(cs_file.name)
    
    # TODOs
    for match in re.finditer(r'// TODO:?\s*(.+)', content):
        methods['todo_items'].append(match.group(1).strip())
    
    return methods

def analyze_service(cs_file):
    """Analyze a service file for completeness"""
    content = cs_file.read_text(encoding='utf-8', errors='ignore')
    
    analysis = {
        'name': cs_file.stem,
        'has_singleton': 'Instance =>' in content or '_lazyInstance' in content,
        'has_async': 'async' in content,
        'has_events': 'event Action' in content or 'event EventHandler' in content,
        'has_dispose': 'IDisposable' in content or 'Dispose()' in content,
        'stub_count': content.count('NotImplementedException'),
        'todo_count': content.count('// TODO'),
        'line_count': len(content.splitlines()),
        'public_methods': len(re.findall(r'public\s+(?:async\s+)?(?:Task|void|\w+)\s+\w+\s*\(', content)),
        'dependencies': []
    }
    
    # Find dependencies
    for match in re.finditer(r'(\w+Service|Manager|Engine|Provider)\.Instance', content):
        dep = match.group(1)
        if dep not in analysis['dependencies']:
            analysis['dependencies'].append(dep)
    
    return analysis

def main():
    audit = {
        'gui': {
            'windows': [],
            'controls': [],
            'total_buttons': 0,
            'total_textboxes': 0,
            'total_handlers': 0
        },
        'services': [],
        'features': {
            'implemented': [],
            'partial': [],
            'stub': []
        },
        'todos': [],
        'summary': {}
    }
    
    # Analyze XAML files
    print("Analyzing XAML files...")
    for xaml in find_all_files("*.xaml"):
        if 'obj' in str(xaml):
            continue
        elements = extract_xaml_elements(xaml)
        
        entry = {
            'file': str(xaml),
            'name': xaml.stem,
            **elements
        }
        
        if 'Window' in xaml.stem or xaml.stem == 'MainWindow':
            audit['gui']['windows'].append(entry)
        else:
            audit['gui']['controls'].append(entry)
        
        audit['gui']['total_buttons'] += len(elements['buttons'])
        audit['gui']['total_textboxes'] += len(elements['textboxes'])
        audit['gui']['total_handlers'] += len(elements['click_handlers']) + len(elements['other_handlers'])
    
    # Analyze Services
    print("Analyzing services...")
    services_dir = BASE_DIR / "Services"
    if services_dir.exists():
        for cs_file in services_dir.glob("*.cs"):
            analysis = analyze_service(cs_file)
            audit['services'].append(analysis)
            
            # Categorize
            if analysis['stub_count'] > 0:
                audit['features']['stub'].append(analysis['name'])
            elif analysis['todo_count'] > 3:
                audit['features']['partial'].append(analysis['name'])
            else:
                audit['features']['implemented'].append(analysis['name'])
            
            # Collect TODOs
            methods = extract_cs_methods(cs_file)
            for todo in methods['todo_items']:
                audit['todos'].append({
                    'file': cs_file.name,
                    'todo': todo
                })
    
    # Summary
    audit['summary'] = {
        'total_xaml_files': len(audit['gui']['windows']) + len(audit['gui']['controls']),
        'total_services': len(audit['services']),
        'total_buttons': audit['gui']['total_buttons'],
        'total_handlers': audit['gui']['total_handlers'],
        'implemented_services': len(audit['features']['implemented']),
        'partial_services': len(audit['features']['partial']),
        'stub_services': len(audit['features']['stub']),
        'total_todos': len(audit['todos']),
        'total_lines': sum(s['line_count'] for s in audit['services'])
    }
    
    # Output
    with open('audit_results.json', 'w') as f:
        json.dump(audit, f, indent=2)
    
    print(f"\n=== AUDIT SUMMARY ===")
    print(f"XAML Files: {audit['summary']['total_xaml_files']}")
    print(f"Services: {audit['summary']['total_services']}")
    print(f"Total Buttons: {audit['summary']['total_buttons']}")
    print(f"Total Event Handlers: {audit['summary']['total_handlers']}")
    print(f"Implemented Services: {audit['summary']['implemented_services']}")
    print(f"Partial Services: {audit['summary']['partial_services']}")
    print(f"Stub Services: {audit['summary']['stub_services']}")
    print(f"Total TODOs: {audit['summary']['total_todos']}")
    print(f"Total Lines of Code: {audit['summary']['total_lines']}")
    
    print(f"\nResults saved to audit_results.json")

if __name__ == '__main__':
    main()
