namespace Configuration;

/// <summary>Strongly typed view of the <c>Greeting</c> configuration section.</summary>
public sealed class GreetingOptions
{
    public const string SectionName = "Greeting";

    public string Message { get; set; } = "";
}
