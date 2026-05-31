using System.ComponentModel;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using FlaUiWindow = FlaUI.Core.AutomationElements.Window;

namespace grzyClothTool.Tests.Wpf;

public sealed class FlaUiTestApplication : IDisposable
{
    private const string MainWindowTitle = "grzyClothTool";

    private FlaUiTestApplication(FlaUI.Core.Application app, UIA3Automation automation, FlaUiWindow mainWindow)
    {
        App = app;
        Automation = automation;
        MainWindow = mainWindow;
    }

    public FlaUI.Core.Application App { get; }

    public UIA3Automation Automation { get; }

    public FlaUiWindow MainWindow { get; private set; }

    public static FlaUiTestApplication Launch(string localAppDataRoot)
    {
        var exePath = GetAppExecutablePath();
        Assert.True(File.Exists(exePath), $"Could not find app executable at {exePath}");

        var startInfo = new ProcessStartInfo(exePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exePath)!
        };

        startInfo.Environment["GRZYCLOTHTOOL_LOCALAPPDATA"] = localAppDataRoot;

        var app = FlaUI.Core.Application.Launch(startInfo);

        var automation = new UIA3Automation();

        try
        {
            var mainWindow = WaitForMainWindow(app, automation);
            mainWindow.Focus();
            mainWindow.SetForeground();
            return new FlaUiTestApplication(app, automation, mainWindow);
        }
        catch
        {
            automation.Dispose();

            if (!app.HasExited)
            {
                app.Kill();
            }

            app.Dispose();
            throw;
        }
    }

    public AutomationElement? FindByName(string name)
    {
        RefreshMainWindow();
        var matches = GetAllAppWindows()
            .SelectMany(window => window.FindAllDescendants(cf => cf.ByName(name)))
            .ToArray();

        return matches.FirstOrDefault();
    }

    public AutomationElement WaitForElementByName(string name, int timeoutSeconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (!App.HasExited && DateTime.UtcNow < deadline)
        {
            var element = FindByName(name);
            if (element is not null)
            {
                return element;
            }

            Thread.Sleep(150);
        }

        throw new TimeoutException($"Could not find UI element named '{name}'.");
    }

    public FlaUiWindow WaitForWindowByName(string name, int timeoutSeconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        FlaUiWindow[] windows = [];

        while (!App.HasExited && DateTime.UtcNow < deadline)
        {
            windows = GetAllAppWindows();

            var window = windows
                .FirstOrDefault(candidate => string.Equals(candidate.Properties.Name.ValueOrDefault, name, StringComparison.Ordinal));

            if (window is not null)
            {
                return window;
            }

            Thread.Sleep(150);
        }

        throw new TimeoutException(
            $"Could not find window named '{name}'. Found windows: {string.Join(", ", windows.Select(window => window.Properties.Name.ValueOrDefault))}");
    }

    public AutomationElement WaitForElementByAutomationId(string automationId, int timeoutSeconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (!App.HasExited && DateTime.UtcNow < deadline)
        {
            var element = GetAllAppWindows()
                .SelectMany(window => window.FindAllDescendants(cf => cf.ByAutomationId(automationId)))
                .OrderByDescending(e => e.Patterns.Invoke.IsSupported ? 1 : 0)
                .FirstOrDefault();

            if (element is not null)
            {
                return element;
            }

            Thread.Sleep(150);
        }

        throw new TimeoutException($"Could not find UI element with AutomationId '{automationId}'.");
    }

    public void ClickByName(string name)
    {
        InvokeElement(WaitForElementByName(name));
    }

    public void NavigateHome()
    {
        // Use Project menu → Back to home screen
        ClickByName("Project");
        ClickByName("Back to home screen");
        WaitForElementByName("Create new project");
    }

    public void ClickByAutomationId(string automationId)
    {
        InvokeElement(WaitForElementByAutomationId(automationId));
    }

    public void BeginInvokeByAutomationId(string automationId)
    {
        BeginInvokeElement(WaitForElementByAutomationId(automationId));
    }

    public void TypeTextByAutomationId(string automationId, string text)
    {
        var element = WaitForElementByAutomationId(automationId);
        SetElementText(element, text);
    }

    public static void InvokeElement(AutomationElement element)
    {
        var invokePattern = element.Patterns.Invoke.PatternOrDefault;
        if (invokePattern is not null)
        {
            invokePattern.Invoke();
            return;
        }

        try
        {
            element.Focus();
            element.SetForeground();
            element.Click();
        }
        catch (Win32Exception)
        {
            throw new NotSupportedException(
                $"Could not invoke element '{element.Properties.Name.ValueOrDefault}': no Invoke pattern and SendInput is not available in this session.");
        }
    }

    public static void BeginInvokeElement(AutomationElement element)
    {
        var invokePattern = element.Patterns.Invoke.PatternOrDefault;
        if (invokePattern is not null)
        {
            _ = Task.Run(invokePattern.Invoke);
            Thread.Sleep(250);
            return;
        }

        _ = Task.Run(() => InvokeElement(element));
        Thread.Sleep(250);
    }

    public static void SetElementText(AutomationElement element, string text)
    {
        var valuePattern = element.Patterns.Value.PatternOrDefault;
        if (valuePattern is not null)
        {
            valuePattern.SetValue(text);
            return;
        }

        element.Focus();
        Keyboard.Type(text);
    }

    public static void SelectRadioButton(AutomationElement element)
    {
        element.AsRadioButton().IsChecked = true;
    }

    public void Dispose()
    {
        if (!App.HasExited)
        {
            App.Close();
        }

        if (!App.HasExited)
        {
            App.Kill();
        }

        Automation.Dispose();
        App.Dispose();
    }

    private void RefreshMainWindow()
    {
        if (App.HasExited)
        {
            throw new InvalidOperationException("grzyClothTool exited unexpectedly.");
        }

        MainWindow = WaitForMainWindow(App, Automation);
    }

    private FlaUiWindow[] GetAllAppWindows()
    {
        var topLevel = App.GetAllTopLevelWindows(Automation)
            .Where(w => w.Properties.ControlType.ValueOrDefault == ControlType.Window)
            .ToArray();

        var modals = topLevel
            .SelectMany(w => w.ModalWindows)
            .ToArray();

        return [.. topLevel, .. modals];
    }

    private static FlaUiWindow WaitForMainWindow(FlaUI.Core.Application app, UIA3Automation automation)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        FlaUiWindow[] windows = [];

        while (!app.HasExited && DateTime.UtcNow < deadline)
        {
            windows = app.GetAllTopLevelWindows(automation)
                .Where(window => window.Properties.ControlType.ValueOrDefault == ControlType.Window)
                .ToArray();

            var mainWindow = windows.FirstOrDefault(window =>
                string.Equals(window.Properties.Name.ValueOrDefault, MainWindowTitle, StringComparison.Ordinal));

            if (mainWindow is not null)
            {
                return mainWindow;
            }

            Thread.Sleep(250);
        }

        Assert.False(app.HasExited, "grzyClothTool exited before the main window appeared.");
        throw new TimeoutException(
            $"Could not find the main grzyClothTool window. Found windows: {string.Join(", ", windows.Select(window => window.Properties.Name.ValueOrDefault))}");
    }

    private static string GetAppExecutablePath()
    {
        var repositoryRoot = FindRepositoryRoot();
        var buildConfiguration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";

        return Path.Combine(
            repositoryRoot.FullName,
            "grzyClothTool",
            "bin",
            buildConfiguration,
            "net10.0-windows",
            "win-x64",
            "grzyClothTool.exe");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "grzyClothTool.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing grzyClothTool.sln.");
    }
}
