namespace Oficina.Infrastructure.Notifications;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string PublicBaseUrl { get; set; } = string.Empty;
}
