#!/usr/bin/env python3
"""
Replace System.Text.Json with Newtonsoft.Json for .NET Framework 4.6.2 compatibility.
"""

import os
import re
from pathlib import Path

def fix_json_imports(content):
    """Replace System.Text.Json imports with Newtonsoft.Json."""
    # Replace using statements
    content = content.replace('using System.Text.Json;', 'using Newtonsoft.Json;')
    content = content.replace('using System.Text.Json.Serialization;', 'using Newtonsoft.Json.Serialization;')
    
    return content

def fix_json_serializer_options(content):
    """Replace JsonSerializerOptions with JsonSerializerSettings."""
    # Replace type name
    content = content.replace('JsonSerializerOptions', 'JsonSerializerSettings')
    
    # Replace property names
    content = content.replace('PropertyNamingPolicy = JsonNamingPolicy.CamelCase', 'ContractResolver = new CamelCasePropertyNamesContractResolver()')
    content = content.replace('PropertyNameCaseInsensitive = true', 'ContractResolver = new DefaultContractResolver()')
    content = content.replace('WriteIndented = true', 'Formatting = Formatting.Indented')
    content = content.replace('DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull', 'NullValueHandling = NullValueHandling.Ignore')
    
    return content

def fix_json_serialize(content):
    """Replace JsonSerializer.Serialize with JsonConvert.SerializeObject."""
    content = re.sub(r'JsonSerializer\.Serialize\(([^,]+),\s*([^)]+)\)', r'JsonConvert.SerializeObject(\1, \2)', content)
    content = re.sub(r'JsonSerializer\.Serialize\(([^)]+)\)', r'JsonConvert.SerializeObject(\1)', content)
    content = re.sub(r'JsonSerializer\.Serialize<([^>]+)>\(([^)]+)\)', r'JsonConvert.SerializeObject(\2)', content)
    
    return content

def fix_json_deserialize(content):
    """Replace JsonSerializer.Deserialize with JsonConvert.DeserializeObject."""
    content = re.sub(r'JsonSerializer\.Deserialize<([^>]+)>\(([^,]+),\s*([^)]+)\)', r'JsonConvert.DeserializeObject<\1>(\2, \3)', content)
    content = re.sub(r'JsonSerializer\.Deserialize<([^>]+)>\(([^)]+)\)', r'JsonConvert.DeserializeObject<\1>(\2)', content)
    
    return content

def fix_json_attributes(content):
    """Replace System.Text.Json attributes with Newtonsoft.Json attributes."""
    content = content.replace('[JsonPropertyName(', '[JsonProperty(PropertyName = ')
    content = content.replace('[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]', '[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]')
    
    return content

def add_newtonsoft_using(content):
    """Ensure Newtonsoft.Json using is present."""
    if 'Newtonsoft.Json' in content and 'using Newtonsoft.Json;' not in content:
        lines = content.split('\n')
        last_using_idx = -1
        for i, line in enumerate(lines):
            if line.strip().startswith('using ') and line.strip().endswith(';'):
                last_using_idx = i
        if last_using_idx >= 0:
            lines.insert(last_using_idx + 1, 'using Newtonsoft.Json;')
            content = '\n'.join(lines)
    return content

def process_file(filepath):
    """Process a single file."""
    print(f"Processing: {filepath}")
    
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    original = content
    
    content = fix_json_imports(content)
    content = fix_json_serializer_options(content)
    content = fix_json_serialize(content)
    content = fix_json_deserialize(content)
    content = fix_json_attributes(content)
    content = add_newtonsoft_using(content)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  Modified")
        return True
    print(f"  No changes")
    return False

def main():
    base_dir = Path('/home/ubuntu/Svony-Browser/SvonyBrowser')
    
    # Files that use System.Text.Json
    files_to_fix = [
        'Controls/ChatbotPanel.xaml.cs',
        'Models/AppSettings.cs',
        'Services/CdpConnectionService.cs',
        'Services/FiddlerBridge.cs',
        'Services/LlmIntegrationService.cs',
        'Services/McpConnectionManager.cs',
        'Services/PacketAnalysisEngine.cs',
        'Services/RealDataProvider.cs',
        'Services/SettingsManager.cs',
        'Services/StatusBarManager.cs',
        'Services/VisualAutomationService.cs',
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
