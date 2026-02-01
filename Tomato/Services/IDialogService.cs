namespace Tomato.Services;

/// <summary>
/// Result of a goal dialog interaction.
/// </summary>
/// <param name="Confirmed">Whether the user confirmed (true) or cancelled (false).</param>
/// <param name="Goal">The goal entered by the user, if confirmed.</param>
public record GoalDialogResult(bool Confirmed, string? Goal);

/// <summary>
/// Result of a results dialog interaction.
/// </summary>
/// <param name="Confirmed">Whether the user confirmed (true) or skipped (false).</param>
/// <param name="Results">The results entered by the user, if confirmed.</param>
public record ResultsDialogResult(bool Confirmed, string? Results);

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

    /// <summary>
    /// Shows the results dialog after a focus session completes.
    /// </summary>
    /// <param name="goal">The goal that was set for the session, if any.</param>
    /// <returns>The dialog result containing confirmation status and results text.</returns>
    Task<ResultsDialogResult> ShowResultsDialogAsync(string? goal);
}
