namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class FontScaleOption
{
    public FontScaleOption(float scale, string displayName)
    {
        Scale = scale;
        DisplayName = displayName;
    }

    public float Scale { get; }
    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}

internal static class UiSettings
{
    public static float FontScale { get; private set; } = 1.0f;

    public static IReadOnlyList<FontScaleOption> FontScaleOptions { get; } =
        new[]
        {
            new FontScaleOption(0.95f, "95%"),
            new FontScaleOption(1.00f, "100%"),
            new FontScaleOption(1.15f, "115%"),
            new FontScaleOption(1.30f, "130%"),
            new FontScaleOption(1.45f, "145%"),
            new FontScaleOption(1.60f, "160%")
        };

    public static event Action? AppearanceChanged;

    public static void SetFontScale(float scale)
    {
        float normalized = Math.Clamp(scale, 0.85f, 2.00f);
        if (Math.Abs(FontScale - normalized) < 0.001f) return;
        FontScale = normalized;
        AppearanceChanged?.Invoke();
    }
}
