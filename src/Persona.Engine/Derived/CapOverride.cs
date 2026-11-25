namespace Persona.Engine.Derived;

// A Violation Override
public sealed record CapOverride(CapViolation Violation, string Note)
{
    public string Note { get; } = !string.IsNullOrWhiteSpace(Note) ? Note :
        throw new ArgumentException("An override requires a note.", nameof(Note));

    public static CapOverrideResult Create(CapViolation violation, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return new CapOverrideResult(null, "An override requires a note.");
        }
        return new CapOverrideResult(new CapOverride(violation, note), null);
    }
}

public sealed record CapOverrideResult(CapOverride? Override, string? Error)
{
    public bool IsSuccess => Override is not null;
}