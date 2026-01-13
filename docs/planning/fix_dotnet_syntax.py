#!/usr/bin/env python3
"""
.NET Framework 4.6.2 Syntax Fixer
Converts .NET 6+ C# syntax to .NET Framework 4.6.2 compatible syntax.
"""

import os
import re
import sys
from pathlib import Path

def fix_file_scoped_namespace(content):
    """Convert file-scoped namespace to block-scoped namespace."""
    lines = content.split('\n')
    new_lines = []
    namespace_found = False
    namespace_name = None
    in_namespace = False
    
    for i, line in enumerate(lines):
        stripped = line.strip()
        
        # Check for file-scoped namespace (ends with semicolon)
        if stripped.startswith('namespace ') and stripped.endswith(';') and not namespace_found:
            namespace_name = stripped[10:-1].strip()  # Extract namespace name
            namespace_found = True
            new_lines.append(f'namespace {namespace_name}')
            new_lines.append('{')
            in_namespace = True
        elif in_namespace:
            # Indent all content inside namespace
            if stripped:  # Non-empty line
                new_lines.append('    ' + line)
            else:
                new_lines.append(line)
        else:
            new_lines.append(line)
    
    if namespace_found:
        new_lines.append('}')
    
    return '\n'.join(new_lines)

def fix_nullable_annotations(content):
    """Remove nullable reference type annotations from reference types only."""
    # Remove ? from common reference types
    # Be careful not to remove from value type nullables (int?, bool?, etc.)
    
    # Reference types that commonly have ?
    ref_types = [
        'string', 'object', 'Exception', 'Task', 'Action', 'Func',
        'List', 'Dictionary', 'HashSet', 'IEnumerable', 'IList', 'ICollection',
        'ChromiumWebBrowser', 'CancellationTokenSource', 'Timer', 'Process',
        'HttpClient', 'HttpResponseMessage', 'JObject', 'JToken', 'JArray',
        'Match', 'Regex', 'StringBuilder', 'Stream', 'StreamReader', 'StreamWriter',
        'FileStream', 'MemoryStream', 'BinaryReader', 'BinaryWriter',
        'TcpClient', 'NetworkStream', 'Socket', 'IPAddress', 'IPEndPoint',
        'Thread', 'SemaphoreSlim', 'ManualResetEvent', 'AutoResetEvent',
        'EventHandler', 'PropertyChangedEventHandler', 'RoutedEventHandler',
        'Window', 'Control', 'FrameworkElement', 'UIElement', 'DependencyObject',
        'Brush', 'SolidColorBrush', 'Color', 'Thickness', 'Visibility',
        'DispatcherTimer', 'Dispatcher', 'Application',
        'JsonSerializer', 'JsonReader', 'JsonWriter',
        'XmlDocument', 'XmlNode', 'XmlElement',
        'Type', 'MethodInfo', 'PropertyInfo', 'FieldInfo', 'Assembly',
        'Attribute', 'EventArgs', 'CancelEventArgs',
        'Uri', 'Version', 'Encoding', 'CultureInfo',
        'Array', 'Delegate', 'MulticastDelegate',
    ]
    
    for rt in ref_types:
        # Pattern: Type? followed by space, comma, >, ), or end
        content = re.sub(rf'\b{rt}\?\s', f'{rt} ', content)
        content = re.sub(rf'\b{rt}\?,', f'{rt},', content)
        content = re.sub(rf'\b{rt}\?>', f'{rt}>', content)
        content = re.sub(rf'\b{rt}\?\)', f'{rt})', content)
        content = re.sub(rf'\b{rt}\?\[', f'{rt}[', content)
    
    # Also handle generic types like List<string>?
    content = re.sub(r'>\?\s', '> ', content)
    content = re.sub(r'>\?,', '>,', content)
    content = re.sub(r'>\?\)', '>)', content)
    
    return content

def fix_target_typed_new(content):
    """Replace new() with explicit type constructor."""
    lines = content.split('\n')
    new_lines = []
    
    for line in lines:
        original_line = line
        
        # Pattern 1: Type variable = new();
        # Match: private readonly List<string> _items = new();
        match = re.search(r'([\w<>\[\],\s]+?)\s+(\w+)\s*=\s*new\(\)\s*;', line)
        if match:
            full_match = match.group(0)
            type_part = match.group(1).strip()
            # Extract just the type name (last word before variable)
            type_words = type_part.split()
            if type_words:
                actual_type = type_words[-1]
                line = line.replace('= new();', f'= new {actual_type}();')
        
        # Pattern 2: Property { get; set; } = new();
        match = re.search(r'([\w<>\[\],]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}\s*=\s*new\(\)\s*;', line)
        if match:
            type_name = match.group(1)
            line = line.replace('= new();', f'= new {type_name}();')
        
        new_lines.append(line)
    
    return '\n'.join(new_lines)

def fix_pattern_matching(content):
    """Convert C# 9+ pattern matching to traditional syntax."""
    # is not null -> != null
    content = re.sub(r'\bis not null\b', '!= null', content)
    
    # is null -> == null  
    content = re.sub(r'\bis null\b', '== null', content)
    
    # is not pattern for types -> traditional check
    # e.g., "if (x is not string s)" -> more complex, skip for now
    
    return content

def fix_lambda_static(content):
    """Remove static keyword from lambda expressions."""
    # static () => ... -> () => ...
    content = re.sub(r'\bstatic\s+\(\s*\)\s*=>', '() =>', content)
    content = re.sub(r'\bstatic\s+(\w+)\s*=>', r'\1 =>', content)
    return content

def fix_init_accessor(content):
    """Replace init accessor with set."""
    content = re.sub(r'\{\s*get;\s*init;\s*\}', '{ get; set; }', content)
    return content

def fix_record_types(content):
    """Convert record types to classes (basic conversion)."""
    # record ClassName -> public class ClassName
    content = re.sub(r'\brecord\s+(\w+)', r'public class \1', content)
    return content

def process_file(filepath):
    """Process a single C# file."""
    print(f"Processing: {filepath}")
    
    try:
        with open(filepath, 'r', encoding='utf-8-sig') as f:
            content = f.read()
    except Exception as e:
        print(f"  Error reading: {e}")
        return False
    
    original = content
    
    # Apply fixes in order
    content = fix_file_scoped_namespace(content)
    content = fix_nullable_annotations(content)
    content = fix_target_typed_new(content)
    content = fix_pattern_matching(content)
    content = fix_lambda_static(content)
    content = fix_init_accessor(content)
    
    if content != original:
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"  Modified: {filepath}")
            return True
        except Exception as e:
            print(f"  Error writing: {e}")
            return False
    else:
        print(f"  No changes: {filepath}")
        return False

def main():
    """Main entry point."""
    base_dir = Path('/home/ubuntu/Svony-Browser/SvonyBrowser')
    
    # Find all C# files
    cs_files = list(base_dir.rglob('*.cs'))
    
    # Exclude files we don't want to modify
    exclude = ['AssemblyInfo.cs', 'NetFrameworkExtensions.cs', 'Program.Flash.cs']
    cs_files = [f for f in cs_files if f.name not in exclude]
    
    print(f"Found {len(cs_files)} C# files to process")
    print("=" * 60)
    
    modified_count = 0
    for filepath in sorted(cs_files):
        if process_file(filepath):
            modified_count += 1
    
    print("=" * 60)
    print(f"Done! Modified {modified_count} files.")

if __name__ == '__main__':
    main()
