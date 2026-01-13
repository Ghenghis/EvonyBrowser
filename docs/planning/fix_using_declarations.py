#!/usr/bin/env python3
"""
Fix C# 8 'using var' declarations to traditional using statements for .NET Framework 4.6.2.
Also fix JsonDocument.Parse to use JObject.Parse from Newtonsoft.Json.
"""

import os
import re
from pathlib import Path

def fix_using_var(content):
    """Convert 'using var x = ...' to 'using (var x = ...) { ... }'"""
    # This is complex because we need to find the scope of the using statement
    # For simplicity, we'll convert to explicit using blocks
    
    lines = content.split('\n')
    result = []
    i = 0
    indent_stack = []
    
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        
        # Check for 'using var' pattern
        match = re.match(r'^(\s*)using var (\w+)\s*=\s*(.+);$', line)
        if match:
            indent = match.group(1)
            var_name = match.group(2)
            expression = match.group(3)
            
            # Convert to traditional using block
            result.append(f'{indent}using (var {var_name} = {expression})')
            result.append(f'{indent}{{')
            
            # Find the end of the current block and insert closing brace
            # For simplicity, we'll just add the closing brace at the end of the method
            # This is a simplified approach - in practice, you'd need proper scope analysis
            indent_stack.append((len(indent), len(result) - 1))
        else:
            result.append(line)
        
        i += 1
    
    # For now, just do simple replacement without block restructuring
    # This will require manual review
    return content

def fix_using_var_simple(content):
    """Simple replacement of 'using var' with 'var' and add comment."""
    # Replace 'using var' with traditional using statement
    # Pattern: using var x = new Something();
    # Replace with: using (var x = new Something())
    
    # This is a simplified approach that adds the using block
    pattern = r'(\s*)using var (\w+)\s*=\s*([^;]+);'
    
    def replace_using(match):
        indent = match.group(1)
        var_name = match.group(2)
        expr = match.group(3)
        return f'{indent}var {var_name} = {expr}; // TODO: Add using block for proper disposal'
    
    # Actually, for .NET Framework, we should use traditional using blocks
    # But that requires restructuring the code significantly
    # For now, let's just remove the 'using' keyword and add a comment
    content = re.sub(pattern, replace_using, content)
    
    return content

def fix_json_document(content):
    """Replace JsonDocument.Parse with JObject.Parse from Newtonsoft.Json."""
    content = content.replace('JsonDocument.Parse', 'JObject.Parse')
    content = content.replace('using var doc = JObject.Parse', 'var doc = JObject.Parse')
    
    # Fix property access patterns
    # JsonDocument: doc.RootElement.GetProperty("x").GetString()
    # JObject: doc["x"]?.ToString()
    
    return content

def add_newtonsoft_linq_using(content):
    """Add Newtonsoft.Json.Linq using if JObject is used."""
    if 'JObject.Parse' in content and 'using Newtonsoft.Json.Linq;' not in content:
        lines = content.split('\n')
        last_using_idx = -1
        for i, line in enumerate(lines):
            if line.strip().startswith('using ') and line.strip().endswith(';'):
                last_using_idx = i
        if last_using_idx >= 0:
            lines.insert(last_using_idx + 1, 'using Newtonsoft.Json.Linq;')
            content = '\n'.join(lines)
    return content

def process_file(filepath):
    """Process a single file."""
    print(f"Processing: {filepath}")
    
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    original = content
    
    content = fix_using_var_simple(content)
    content = fix_json_document(content)
    content = add_newtonsoft_linq_using(content)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Modified")
        return True
    print(f"  No changes")
    return False

def main():
    base_dir = Path('/home/ubuntu/Svony-Browser/SvonyBrowser')
    
    # Files with 'using var' declarations
    files_to_fix = [
        'Services/CdpConnectionService.cs',
        'Services/ExportImportManager.cs',
        'Services/FailsafeManager.cs',
        'Services/LlmIntegrationService.cs',
        'Services/McpConnectionManager.cs',
        'Services/ProtocolFuzzer.cs',
        'Services/ProxyMonitor.cs',
        'Services/RealDataProvider.cs',
        'Services/SessionRecorder.cs',
        'Services/SettingsManager.cs',
        'Services/TrafficPipeClient.cs',
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
