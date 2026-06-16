using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;
using Xunit;

namespace WinPrint.Maui.UITests;

/// <summary>
///     Appium UI automation for the MacCatalyst app's native <b>File</b> menu — the File ▸ Open… and
///     File ▸ Print… items wired in <c>Platforms/MacCatalyst/AppDelegate.cs</c>. Drives the running app
///     (bundle id <c>com.kindel.winprint</c>) through the macOS <c>mac2</c> driver.
///     <para>
///         Prerequisites (the macOS CI job provides these): an Appium server with <c>appium-mac2-driver</c>
///         on <c>APPIUM_SERVER</c> (default <c>http://127.0.0.1:4723</c>), and the app installed/launchable.
///         Tests no-op on non-macOS hosts so the cross-platform solution build stays green.
///     </para>
/// </summary>
[Collection("maui-ui")]
public class MacMenuUITests
{
    private const string BundleId = "com.kindel.winprint";

    // Only run when the Appium harness is wired up (the macOS CI job sets APPIUM_SERVER). This keeps the
    // tests a no-op on Windows CI and on dev machines without an Appium server, rather than failing.
    private static bool HarnessAvailable =>
        OperatingSystem.IsMacOS() && Environment.GetEnvironmentVariable("APPIUM_SERVER") is not null;

    [Fact]
    public void FileMenu_Open_IsEnabledAtStartup()
    {
        if (!HarnessAvailable)
        {
            return;
        }

        using MacDriver driver = CreateDriver();
        AppiumElement open = OpenFileMenuItem(driver, "Open…");

        Assert.True(open.Enabled, "File ▸ Open… should always be enabled.");
    }

    [Fact]
    public void FileMenu_Print_IsDisabledUntilAFileIsLoaded()
    {
        if (!HarnessAvailable)
        {
            return;
        }

        using MacDriver driver = CreateDriver();

        // No document is open at startup, so AppDelegate greys out Print… (CanPrint == false).
        AppiumElement print = OpenFileMenuItem(driver, "Print…");

        Assert.False(print.Enabled, "File ▸ Print… should be disabled until a document is loaded.");
    }

    // Clicks the top-level File menu and returns the named item beneath it.
    private static AppiumElement OpenFileMenuItem(MacDriver driver, string itemTitle)
    {
        AppiumElement fileMenu = driver.FindElement(
            By.XPath("//XCUIElementTypeMenuBarItem[@title='File']"));
        fileMenu.Click();

        try
        {
            return driver.FindElement(
                By.XPath($"//XCUIElementTypeMenuItem[@title='{itemTitle}']"));
        }
        catch (WebDriverException)
        {
            // The menu item wasn't where we expected. Dump the live accessibility tree so the exact
            // representation of the Catalyst File-menu items (element type / title / label) is visible in
            // the CI log and uploaded as an artifact, instead of guessing at the locator blind.
            DumpAccessibilityTree(driver, itemTitle);
            throw;
        }
    }

    private static void DumpAccessibilityTree(MacDriver driver, string itemTitle)
    {
        string source;
        try
        {
            source = driver.PageSource;
        }
        catch (WebDriverException ex)
        {
            source = $"<could-not-capture-page-source>{ex.Message}</could-not-capture-page-source>";
        }

        Console.WriteLine($"===== ACCESSIBILITY TREE (item '{itemTitle}' not found) =====");
        Console.WriteLine(source);
        Console.WriteLine("===== END ACCESSIBILITY TREE =====");

        string dir = Environment.GetEnvironmentVariable("MENU_TREE_DUMP_DIR")
                     ?? Directory.GetCurrentDirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir, "menu-tree.xml"), source);
        }
        catch (IOException)
        {
            // Best-effort: the console dump above is the primary record.
        }
    }

    private static MacDriver CreateDriver()
    {
        string server = Environment.GetEnvironmentVariable("APPIUM_SERVER") ?? "http://127.0.0.1:4723";

        var options = new AppiumOptions
        {
            PlatformName = "mac",
            AutomationName = "mac2"
        };

        // Prefer launching the freshly built bundle by path (set by the macOS CI job); otherwise launch
        // the installed app by bundle id.
        string? appPath = Environment.GetEnvironmentVariable("APPIUM_APP");
        if (!string.IsNullOrEmpty(appPath))
        {
            // `app` is a reserved capability with a first-class property; AddAdditionalAppiumOption("app", …)
            // throws ("use the Application property instead"). Set the property so it serializes to appium:app.
            options.App = appPath;
        }
        else
        {
            options.AddAdditionalAppiumOption("bundleId", BundleId);
        }

        options.AddAdditionalAppiumOption("showServerLogs", true);

        var driver = new MacDriver(new Uri(server), options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);

        // Launching by `app` path starts the process but does NOT make it frontmost — XCUITest's menu bar
        // is owned by whatever app is active (Finder on a fresh CI desktop), so menu-item lookups would hit
        // Finder's File menu, not ours. Explicitly activate our app so its menu bar is the one in the tree.
        ActivateApp(driver);
        return driver;
    }

    private static void ActivateApp(MacDriver driver)
    {
        try
        {
            driver.ExecuteScript("macos: activateApp", new Dictionary<string, object> { ["bundleId"] = BundleId });
        }
        catch (WebDriverException)
        {
            // Non-fatal: if activation isn't supported/needed the menu-item find (and its tree dump on
            // failure) still runs and tells us what actually happened.
        }
    }
}
