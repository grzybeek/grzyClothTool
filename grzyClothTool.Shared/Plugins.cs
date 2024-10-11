namespace grzyClothTool.Shared;

public interface IPlugin
{
    Task Run();
}

public interface IPatreonPlugin : IPlugin
{
    public bool IsLoggedIn { get; }
    public string Username { get; }
    public string ImageUrl { get; }

    public string? Status { get; }
    public string? LastChargeDate { get; }
    public string? NextChargeDate { get; }
    public int TierCents { get; }

    Task Login();
    void Logout();
}

public interface IPluginMetadata
{
    string Name { get; }
}
