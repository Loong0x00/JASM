namespace GIMI_ModManager.Avalonia;

/// <summary>Sent when first-time setup finishes so the shell can take over.</summary>
public sealed record StartupCompletedMessage;

/// <summary>Sent when the user switches the active game so the shell can re-initialize.</summary>
public sealed record GameChangedMessage(string Game);
