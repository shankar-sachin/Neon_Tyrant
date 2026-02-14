namespace NeonTyrant;

public static class AppPaths
{
    public static string BaseDataDir
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                return Path.Combine(localAppData, "NeonTyrant", "data");
            }

            return Path.Combine(AppContext.BaseDirectory, "data");
        }
    }

    public static string ConfigPath => Path.Combine(BaseDataDir, "config.json");

    public static string ScoresPath => Path.Combine(BaseDataDir, "scores.csv");
}
