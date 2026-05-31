namespace grzyClothTool.Tests.Wpf.Tests;

[Collection(FlaUiTestCollection.Name)]
public class HomeMenuTests : IClassFixture<SharedAppFixture>
{
    private const string DialogWindowTitle = "Project Setup";

    private readonly SharedAppFixture _fixture;

    public HomeMenuTests(SharedAppFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToHome();
    }

    [Fact]
    public void HomeScreen_ShowsActions()
    {
        Assert.NotNull(_fixture.App.WaitForElementByName("Create new project"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Import project"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Open addon"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Open save"));

        Assert.NotNull(_fixture.App.WaitForElementByName("Project"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Settings"));
        Assert.NotNull(_fixture.App.WaitForElementByName("View"));

        Assert.NotNull(_fixture.App.WaitForElementByName("No recent projects"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Discord"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Source code"));
        Assert.NotNull(_fixture.App.WaitForElementByName("Support me!"));
    }

    [Fact]
    public void CreateNewProject_OpensSetupDialog()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");

        var dialog = _fixture.App.WaitForWindowByName(DialogWindowTitle);
        Assert.NotNull(dialog);
    }

    [Fact]
    public void CreateNewProject_ConfirmButtonDisabledWithNoName()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");
        _fixture.App.WaitForWindowByName(DialogWindowTitle);

        var confirmButton = _fixture.App.WaitForElementByAutomationId("ConfirmButton");
        Assert.False(confirmButton.Properties.IsEnabled.ValueOrDefault);
    }

    [Fact]
    public void CreateNewProject_ConfirmButtonEnabledAfterTypingName()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");
        _fixture.App.WaitForWindowByName(DialogWindowTitle);

        _fixture.App.TypeTextByAutomationId("ProjectNameTextBox", "TestProject");

        var confirmButton = _fixture.App.WaitForElementByAutomationId("ConfirmButton");
        Assert.True(confirmButton.Properties.IsEnabled.ValueOrDefault);
    }

    [Fact]
    public void CreateNewProject_CancelClosesDialogWithoutNavigating()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");
        _fixture.App.WaitForWindowByName(DialogWindowTitle);

        _fixture.App.ClickByName("Cancel");

        Assert.NotNull(_fixture.App.WaitForElementByName("Create new project"));
    }

    [Fact]
    public void CreateNewProject_CompletesAndNavigatesToProjectView()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");
        _fixture.App.WaitForWindowByName(DialogWindowTitle);

        _fixture.App.TypeTextByAutomationId("ProjectNameTextBox", UniqueProjectName("MyNewProject"));
        _fixture.App.ClickByName("Create");

        Assert.NotNull(_fixture.App.WaitForElementByName("ADD DRAWABLES"));
    }

    [Fact]
    public void CreateNewProject_ExternalType_CompletesAndNavigatesToProjectView()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");
        _fixture.App.WaitForWindowByName(DialogWindowTitle);

        _fixture.App.TypeTextByAutomationId("ProjectNameTextBox", UniqueProjectName("MyExternalProject"));

        FlaUiTestApplication.InvokeElement(_fixture.App.WaitForElementByName("External"));

        _fixture.App.ClickByName("Create");

        Assert.NotNull(_fixture.App.WaitForElementByName("ADD DRAWABLES"));
    }

    [Fact]
    public void AddDrawables_OpensDrawableFileDialogFromProjectView()
    {
        _fixture.App.BeginInvokeByAutomationId("Create new project");
        _fixture.App.WaitForWindowByName(DialogWindowTitle);
        _fixture.App.TypeTextByAutomationId("ProjectNameTextBox", UniqueProjectName("DrawableDialogProject"));
        _fixture.App.ClickByName("Create");
        Assert.NotNull(_fixture.App.WaitForElementByName("ADD DRAWABLES"));

        _fixture.App.BeginInvokeByAutomationId("ADD DRAWABLES");

        var dialog = _fixture.App.WaitForWindowByName("Select drawable files");
        Assert.NotNull(dialog);
        dialog.Close();
    }

    private static string UniqueProjectName(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }
}
