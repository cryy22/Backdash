namespace SpaceWar;

public class AppSettings
{
    public int Port = 9000;
    public string LobbyName = "spacewar";
    public string Username = string.Empty;

    public readonly Uri LobbyUrl = new("https://lobby-server.fly.dev");
    // public readonly Uri LobbyUrl = new("http://localhost:9999");

    public AppSettings(string[] args)
    {
        if (args is [{ } portArg, ..] && int.TryParse(portArg, out var port))
            Port = port;

        if (args is [_, { } username, ..] && !string.IsNullOrWhiteSpace(username))
            Username = username;
    }
}
