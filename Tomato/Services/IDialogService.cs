using Tomato.Models;

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
/// <param name="Rating">The optional 1-5 star rating, if provided.</param>
public record ResultsDialogResult(bool Confirmed, string? Results, int? Rating);

/// <summary>
/// Result of a supplemental activity dialog interaction.
/// </summary>
/// <param name="Confirmed">Whether the user confirmed (true) or cancelled (false).</param>
/// <param name="Description">The activity description entered by the user, if confirmed.</param>
public record SupplementalActivityDialogResult(bool Confirmed, string? Description);

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
    /// <param name="capturedTodos">Optional list of todos captured during the session.</param>
    /// <returns>The dialog result containing confirmation status and results text.</returns>
    Task<ResultsDialogResult> ShowResultsDialogAsync(string? goal, IReadOnlyList<TodoItem>? capturedTodos = null);

    /// <summary>
    /// Shows a dialog displaying captured todos (without feedback options).
    /// Used when a session is cancelled early but has captured todos.
    /// </summary>
    /// <param name="capturedTodos">The list of todos to display.</param>
    Task ShowTodosDialogAsync(IReadOnlyList<TodoItem> capturedTodos);

    /// <summary>
    /// Shows the supplemental activity entry dialog.
    /// </summary>
    /// <returns>The dialog result containing confirmation status and activity description.</returns>
    Task<SupplementalActivityDialogResult> ShowSupplementalActivityDialogAsync();
}
