import os
import re

def count_controls(xaml_file):
    """Count all interactive controls in XAML"""
    with open(xaml_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    controls = {
        'Button': len(re.findall(r'<Button\s', content)),
        'ToggleButton': len(re.findall(r'<ToggleButton\s', content)),
        'CheckBox': len(re.findall(r'<CheckBox\s', content)),
        'RadioButton': len(re.findall(r'<RadioButton\s', content)),
        'Slider': len(re.findall(r'<Slider\s', content)),
        'TextBox': len(re.findall(r'<TextBox\s', content)),
        'ComboBox': len(re.findall(r'<ComboBox\s', content)),
        'ListBox': len(re.findall(r'<ListBox\s', content)),
    }
    
    # Check for event handlers
    has_click = len(re.findall(r'Click="', content))
    has_checked = len(re.findall(r'Checked="', content))
    has_unchecked = len(re.findall(r'Unchecked="', content))
    has_value_changed = len(re.findall(r'ValueChanged="', content))
    has_selection_changed = len(re.findall(r'SelectionChanged="', content))
    has_text_changed = len(re.findall(r'TextChanged="', content))
    
    return controls, {
        'Click': has_click,
        'Checked': has_checked,
        'Unchecked': has_unchecked,
        'ValueChanged': has_value_changed,
        'SelectionChanged': has_selection_changed,
        'TextChanged': has_text_changed,
    }

def check_dynamic_wiring(cs_file):
    """Check if file uses dynamic event wiring"""
    with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    patterns = {
        'WireUpChangeEvents': 'WireUpChangeEvents' in content,
        'FindVisualChildren': 'FindVisualChildren' in content,
        'Checked +=': '.Checked +=' in content or 'Checked+=' in content,
        'ValueChanged +=': '.ValueChanged +=' in content,
        'SelectionChanged +=': '.SelectionChanged +=' in content,
        'TextChanged +=': '.TextChanged +=' in content,
    }
    return patterns

xaml_files = [
    ('SvonyBrowser/MainWindow.xaml', 'SvonyBrowser/MainWindow.xaml.cs'),
    ('SvonyBrowser/SettingsWindow.xaml', 'SvonyBrowser/SettingsWindow.xaml.cs'),
    ('SvonyBrowser/SettingsControlCenter.xaml', 'SvonyBrowser/SettingsControlCenter.xaml.cs'),
    ('SvonyBrowser/Controls/ChatbotPanel.xaml', 'SvonyBrowser/Controls/ChatbotPanel.xaml.cs'),
    ('SvonyBrowser/Controls/TrafficViewer.xaml', 'SvonyBrowser/Controls/TrafficViewer.xaml.cs'),
    ('SvonyBrowser/Controls/ProtocolExplorer.xaml', 'SvonyBrowser/Controls/ProtocolExplorer.xaml.cs'),
    ('SvonyBrowser/Controls/StatusBar.xaml', 'SvonyBrowser/Controls/StatusBar.xaml.cs'),
]

print("=" * 100)
print("CONTROL INVENTORY AND WIRING STATUS")
print("=" * 100)

totals = {k: 0 for k in ['Button', 'ToggleButton', 'CheckBox', 'RadioButton', 'Slider', 'TextBox', 'ComboBox', 'ListBox']}

for xaml, cs in xaml_files:
    if not os.path.exists(xaml):
        continue
    
    controls, handlers = count_controls(xaml)
    dynamic = check_dynamic_wiring(cs) if os.path.exists(cs) else {}
    
    print(f"\n{os.path.basename(xaml)}")
    print("-" * 80)
    
    # Controls
    print("  Controls:", end=" ")
    for name, count in controls.items():
        if count > 0:
            print(f"{name}:{count}", end=" ")
            totals[name] += count
    print()
    
    # Explicit handlers
    print("  Explicit handlers:", end=" ")
    for name, count in handlers.items():
        if count > 0:
            print(f"{name}:{count}", end=" ")
    print()
    
    # Dynamic wiring
    if any(dynamic.values()):
        print("  Dynamic wiring:", end=" ")
        for name, has in dynamic.items():
            if has:
                print(f"✓{name}", end=" ")
        print()

print("\n" + "=" * 100)
print("TOTALS")
print("=" * 100)
for name, count in totals.items():
    if count > 0:
        print(f"  {name}: {count}")

total_interactive = sum(totals.values())
print(f"\n  TOTAL INTERACTIVE CONTROLS: {total_interactive}")
