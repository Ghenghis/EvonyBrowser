using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace SvonyBrowser.Tests.UI;

/// <summary>
/// Base class for WPF UI automation tests using FlaUI.
/// Provides common functionality for launching, interacting with, and testing the application.
/// </summary>
public abstract class WpfTestBase : IDisposable
{
    protected Application? App { get; private set; }
    protected UIA3Automation? Automation { get; private set; }
    protected Window? MainWindow { get; private set; }
    protected ConditionFactory CF => Automation!.ConditionFactory;
    
    private static readonly string AppPath = GetAppPath();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private bool _disposed;

    #region Setup/Teardown

    protected virtual void SetUp()
    {
        Automation = new UIA3Automation();
        
        // Launch the application
        App = Application.Launch(new ProcessStartInfo
        {
            FileName = AppPath,
            WorkingDirectory = Path.GetDirectoryName(AppPath)
        });

        // Wait for main window
        MainWindow = WaitForMainWindow();
    }

    protected virtual void TearDown()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            App?.Close();
            App?.Dispose();
        }
        catch { }

        Automation?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Window Management

    protected Window? WaitForMainWindow(TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var deadline = DateTime.Now + timeout.Value;

        while (DateTime.Now < deadline)
        {
            try
            {
                var window = App?.GetMainWindow(Automation!);
                if (window != null && window.IsAvailable)
                {
                    return window;
                }
            }
            catch { }
            Thread.Sleep(100);
        }

        throw new TimeoutException("Main window did not appear within timeout");
    }

    protected Window? WaitForWindow(string title, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var deadline = DateTime.Now + timeout.Value;

        while (DateTime.Now < deadline)
        {
            try
            {
                var windows = App?.GetAllTopLevelWindows(Automation!);
                var window = windows?.FirstOrDefault(w => w.Title.Contains(title));
                if (window != null && window.IsAvailable)
                {
                    return window;
                }
            }
            catch { }
            Thread.Sleep(100);
        }

        return null;
    }

    protected void CloseWindow(Window window)
    {
        try
        {
            window.Close();
        }
        catch { }
    }

    #endregion

    #region Element Finding

    protected AutomationElement? FindByAutomationId(string automationId, AutomationElement? parent = null)
    {
        parent ??= MainWindow;
        return parent?.FindFirstDescendant(CF.ByAutomationId(automationId));
    }

    protected AutomationElement? FindByName(string name, AutomationElement? parent = null)
    {
        parent ??= MainWindow;
        return parent?.FindFirstDescendant(CF.ByName(name));
    }

    protected AutomationElement? FindByClassName(string className, AutomationElement? parent = null)
    {
        parent ??= MainWindow;
        return parent?.FindFirstDescendant(CF.ByClassName(className));
    }

    protected AutomationElement? FindByText(string text, AutomationElement? parent = null)
    {
        parent ??= MainWindow;
        return parent?.FindFirstDescendant(CF.ByText(text));
    }

    protected AutomationElement[] FindAllByClassName(string className, AutomationElement? parent = null)
    {
        parent ??= MainWindow;
        return parent?.FindAllDescendants(CF.ByClassName(className)) ?? Array.Empty<AutomationElement>();
    }

    protected T? FindElement<T>(string automationId, AutomationElement? parent = null) where T : AutomationElement
    {
        var element = FindByAutomationId(automationId, parent);
        return element?.AsBase() as T;
    }

    protected Button? FindButton(string automationId, AutomationElement? parent = null)
    {
        return FindByAutomationId(automationId, parent)?.AsButton();
    }

    protected TextBox? FindTextBox(string automationId, AutomationElement? parent = null)
    {
        return FindByAutomationId(automationId, parent)?.AsTextBox();
    }

    protected CheckBox? FindCheckBox(string automationId, AutomationElement? parent = null)
    {
        return FindByAutomationId(automationId, parent)?.AsCheckBox();
    }

    protected ComboBox? FindComboBox(string automationId, AutomationElement? parent = null)
    {
        return FindByAutomationId(automationId, parent)?.AsComboBox();
    }

    protected Slider? FindSlider(string automationId, AutomationElement? parent = null)
    {
        return FindByAutomationId(automationId, parent)?.AsSlider();
    }

    #endregion

    #region Element Waiting

    protected AutomationElement? WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var deadline = DateTime.Now + timeout.Value;

        while (DateTime.Now < deadline)
        {
            var element = FindByAutomationId(automationId);
            if (element != null && element.IsAvailable)
            {
                return element;
            }
            Thread.Sleep(100);
        }

        return null;
    }

    protected bool WaitForElementToDisappear(string automationId, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var deadline = DateTime.Now + timeout.Value;

        while (DateTime.Now < deadline)
        {
            var element = FindByAutomationId(automationId);
            if (element == null || !element.IsAvailable)
            {
                return true;
            }
            Thread.Sleep(100);
        }

        return false;
    }

    protected bool WaitForCondition(Func<bool> condition, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var deadline = DateTime.Now + timeout.Value;

        while (DateTime.Now < deadline)
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch { }
            Thread.Sleep(100);
        }

        return false;
    }

    #endregion

    #region Interactions

    protected void Click(AutomationElement element)
    {
        element.Click();
        Thread.Sleep(100); // Allow UI to update
    }

    protected void DoubleClick(AutomationElement element)
    {
        element.DoubleClick();
        Thread.Sleep(100);
    }

    protected void RightClick(AutomationElement element)
    {
        element.RightClick();
        Thread.Sleep(100);
    }

    protected void EnterText(TextBox textBox, string text)
    {
        textBox.Focus();
        textBox.Text = text;
        Thread.Sleep(50);
    }

    protected void ClearAndEnterText(TextBox textBox, string text)
    {
        textBox.Focus();
        textBox.Text = string.Empty;
        Thread.Sleep(50);
        textBox.Text = text;
        Thread.Sleep(50);
    }

    protected void SetCheckBox(CheckBox checkBox, bool isChecked)
    {
        if (checkBox.IsChecked != isChecked)
        {
            checkBox.Click();
            Thread.Sleep(100);
        }
    }

    protected void SelectComboBoxItem(ComboBox comboBox, string itemText)
    {
        comboBox.Expand();
        Thread.Sleep(100);
        
        var item = comboBox.Items.FirstOrDefault(i => i.Text == itemText);
        item?.Click();
        Thread.Sleep(100);
    }

    protected void SelectComboBoxItemByIndex(ComboBox comboBox, int index)
    {
        comboBox.Expand();
        Thread.Sleep(100);
        
        if (index >= 0 && index < comboBox.Items.Length)
        {
            comboBox.Items[index].Click();
        }
        Thread.Sleep(100);
    }

    protected void SetSliderValue(Slider slider, double value)
    {
        slider.Value = value;
        Thread.Sleep(100);
    }

    protected void PressKey(VirtualKeyShort key)
    {
        Keyboard.Press(key);
        Thread.Sleep(50);
    }

    protected void TypeText(string text)
    {
        Keyboard.Type(text);
        Thread.Sleep(50);
    }

    protected void PressKeyCombo(params VirtualKeyShort[] keys)
    {
        foreach (var key in keys)
        {
            Keyboard.Press(key);
        }
        Thread.Sleep(50);
        foreach (var key in keys.Reverse())
        {
            Keyboard.Release(key);
        }
        Thread.Sleep(50);
    }

    #endregion

    #region Assertions

    protected void AssertElementExists(string automationId, string message = "")
    {
        var element = FindByAutomationId(automationId);
        if (element == null || !element.IsAvailable)
        {
            throw new AssertionException($"Element '{automationId}' not found. {message}");
        }
    }

    protected void AssertElementNotExists(string automationId, string message = "")
    {
        var element = FindByAutomationId(automationId);
        if (element != null && element.IsAvailable)
        {
            throw new AssertionException($"Element '{automationId}' should not exist. {message}");
        }
    }

    protected void AssertTextEquals(TextBox textBox, string expected, string message = "")
    {
        if (textBox.Text != expected)
        {
            throw new AssertionException($"Expected text '{expected}' but got '{textBox.Text}'. {message}");
        }
    }

    protected void AssertIsChecked(CheckBox checkBox, string message = "")
    {
        if (checkBox.IsChecked != true)
        {
            throw new AssertionException($"CheckBox should be checked. {message}");
        }
    }

    protected void AssertIsNotChecked(CheckBox checkBox, string message = "")
    {
        if (checkBox.IsChecked != false)
        {
            throw new AssertionException($"CheckBox should not be checked. {message}");
        }
    }

    protected void AssertIsEnabled(AutomationElement element, string message = "")
    {
        if (!element.IsEnabled)
        {
            throw new AssertionException($"Element should be enabled. {message}");
        }
    }

    protected void AssertIsDisabled(AutomationElement element, string message = "")
    {
        if (element.IsEnabled)
        {
            throw new AssertionException($"Element should be disabled. {message}");
        }
    }

    protected void AssertWindowTitle(string expectedTitle, string message = "")
    {
        if (MainWindow?.Title != expectedTitle)
        {
            throw new AssertionException($"Expected window title '{expectedTitle}' but got '{MainWindow?.Title}'. {message}");
        }
    }

    #endregion

    #region Screenshots

    protected void TakeScreenshot(string fileName)
    {
        var screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotDir);
        
        var fullPath = Path.Combine(screenshotDir, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        MainWindow?.Capture().ToFile(fullPath);
    }

    #endregion

    #region Helpers

    private static string GetAppPath()
    {
        // Look for the built application
        var possiblePaths = new[]
        {
            @"..\..\..\..\SvonyBrowser\bin\x64\Release\SvonyBrowser.exe",
            @"..\..\..\..\SvonyBrowser\bin\x64\Debug\SvonyBrowser.exe",
            @"..\..\..\..\SvonyBrowser\bin\Release\SvonyBrowser.exe",
            @"..\..\..\..\SvonyBrowser\bin\Debug\SvonyBrowser.exe",
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException("Could not find SvonyBrowser.exe. Please build the application first.");
    }

    #endregion
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
