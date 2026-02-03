namespace Tomato.Models;

/// <summary>
/// Represents a quick-capture todo item captured during a focus session.
/// </summary>
/// <param name="Text">The todo text.</param>
/// <param name="CapturedAt">When the todo was captured.</param>
public record TodoItem(string Text, DateTime CapturedAt);
