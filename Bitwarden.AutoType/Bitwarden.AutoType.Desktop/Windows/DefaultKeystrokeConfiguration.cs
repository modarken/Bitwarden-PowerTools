using System;

namespace Bitwarden.AutoType.Desktop.Windows;

public interface IKeystrokeConfiguration
{
    TimeSpan DelayBetweenKeystrokes { get; set; }
    TimeSpan PressKeyTime { get; set; }
}

public class DefaultKeystrokeConfiguration : IKeystrokeConfiguration
{
    private static readonly int _delayBetweenKeyStrokes = 15;
    private static readonly int _pressKeyTime = 15;
    public TimeSpan DelayBetweenKeystrokes { get; set; } = TimeSpan.FromMilliseconds(_delayBetweenKeyStrokes);
    public TimeSpan PressKeyTime { get; set; } = TimeSpan.FromMilliseconds(_pressKeyTime);
}