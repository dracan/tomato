namespace Tomato.Services;

/// <summary>
/// Result of a goal dialog interaction.
/// </summary>
/// <param name="Confirmed">Whether the user confirmed (true) or cancelled (false).</param>
/// <param name="Goal">The goal entered by the user, if confirmed.</param>
public record GoalDialogResult(bool Confirmed, string? Goal);

/// <summary>
/// Service for showing dialogs to the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the goal entry dialog for a new focus session.
    /// </summary>
    /// <returns>The dialog result containing confirmation status and goal text.</returns>
    Task<GoalDialogResult> ShowGoalDialogAsync();
}
