using UnityEditor;

#nullable enable

// -=-=-=- //

public static class VectorierSettings
{
    public const string SettingsPath = "Assets/Settings/VectorierSettings.asset";

    internal const string GameDirectoryKey = "VectorierSettings.GameDirectory";
    internal const string GameShortcutKey = "VectorierSettings.GameShortcutPath";
    internal const string UseShortcutLaunchKey = "VectorierSettings.UseShortcutLaunch";
    public static string? GameDirectory => EditorPrefs.GetString(GameDirectoryKey, "");
    public static string? GameShortcutPath => EditorPrefs.GetString(GameShortcutKey, "");
    public static bool UseShortcutLaunch => EditorPrefs.GetBool(UseShortcutLaunchKey, false);
}