using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class ProgressHelperTests
{
    [Fact]
    public void StartAndStop_ToggleSavingPausedAndRaiseStatusEvents()
    {
        var statuses = new List<ProgressStatus>();
        EventHandler<ProgressMessageEventArgs> handler = (_, args) => statuses.Add(args.Status);

        ProgressHelper.ProgressStatusChanged += handler;
        try
        {
            ProgressHelper.Start();

            Assert.True(SaveHelper.SavingPaused);

            ProgressHelper.Stop();

            Assert.False(SaveHelper.SavingPaused);
            Assert.Equal([ProgressStatus.Start, ProgressStatus.Stop], statuses);
        }
        finally
        {
            ProgressHelper.ProgressStatusChanged -= handler;
            SaveHelper.SavingPaused = false;
        }
    }

    [Fact]
    public void StartAndStop_AllowNullOrEmptyLogText()
    {
        ProgressHelper.Start(string.Empty);
        ProgressHelper.Stop(null);

        Assert.False(SaveHelper.SavingPaused);
    }
}
