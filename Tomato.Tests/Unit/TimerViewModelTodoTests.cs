using FluentAssertions;
using NSubstitute;
using Tomato.Models;
using Tomato.Services;
using Tomato.ViewModels;

namespace Tomato.Tests.Unit;

public class TimerViewModelTodoTests
{
    private readonly ISessionManager _sessionManager;
    private readonly IDialogService _dialogService;
    private readonly IStatisticsReportService _statisticsReportService;
    private readonly TimerViewModel _sut;

    public TimerViewModelTodoTests()
    {
        _sessionManager = Substitute.For<ISessionManager>();
        _sessionManager.Cycle.Returns(new PomodoroCycle());
        _sessionManager.TodayStatistics.Returns(DailyStatistics.Create(DateOnly.FromDateTime(DateTime.Now)));

        _dialogService = Substitute.For<IDialogService>();
        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, null));

        _statisticsReportService = Substitute.For<IStatisticsReportService>();

        _sut = new TimerViewModel(_sessionManager, _dialogService, _statisticsReportService);
    }

    #region Initial State

    [Fact]
    public void CapturedTodos_InitiallyEmpty()
    {
        // Assert
        _sut.CapturedTodos.Should().BeEmpty();
    }

    [Fact]
    public void HasCapturedTodos_InitiallyFalse()
    {
        // Assert
        _sut.HasCapturedTodos.Should().BeFalse();
    }

    [Fact]
    public void IsTodoInputVisible_InitiallyFalse()
    {
        // Assert
        _sut.IsTodoInputVisible.Should().BeFalse();
    }

    [Fact]
    public void NewTodoText_InitiallyEmpty()
    {
        // Assert
        _sut.NewTodoText.Should().BeEmpty();
    }

    [Fact]
    public void CanShowTodoInput_WhenIdle_ReturnsFalse()
    {
        // Assert - not running, not paused
        _sut.CanShowTodoInput.Should().BeFalse();
    }

    #endregion

    #region ShowTodoInput Command

    [Fact]
    public void ShowTodoInputCommand_WhenRunningFocusSession_ShowsInput()
    {
        // Arrange - simulate running focus session
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Act
        _sut.ShowTodoInputCommand.Execute(null);

        // Assert
        _sut.IsTodoInputVisible.Should().BeTrue();
    }

    [Fact]
    public void ShowTodoInputCommand_WhenPausedFocusSession_ShowsInput()
    {
        // Arrange - simulate paused focus session
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Paused;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Paused));

        // Act
        _sut.ShowTodoInputCommand.Execute(null);

        // Assert
        _sut.IsTodoInputVisible.Should().BeTrue();
    }

    [Fact]
    public void CanShowTodoInput_DuringFocusSession_ReturnsTrue()
    {
        // Arrange
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.CanShowTodoInput.Should().BeTrue();
    }

    [Fact]
    public void CanShowTodoInput_DuringBreakSession_ReturnsFalse()
    {
        // Arrange
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Assert
        _sut.CanShowTodoInput.Should().BeFalse();
    }

    #endregion

    #region AddTodo Command

    [Fact]
    public void AddTodoCommand_WithText_AddsTodoAndClearsInput()
    {
        // Arrange
        _sut.NewTodoText = "Buy groceries";
        _sut.IsTodoInputVisible = true;

        // Act
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.CapturedTodos.Should().HaveCount(1);
        _sut.CapturedTodos[0].Text.Should().Be("Buy groceries");
        _sut.NewTodoText.Should().BeEmpty();
        _sut.IsTodoInputVisible.Should().BeFalse();
    }

    [Fact]
    public void AddTodoCommand_WithWhitespaceOnly_DoesNotAdd()
    {
        // Arrange
        _sut.NewTodoText = "   ";
        _sut.IsTodoInputVisible = true;

        // Act
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.CapturedTodos.Should().BeEmpty();
        _sut.IsTodoInputVisible.Should().BeFalse();
    }

    [Fact]
    public void AddTodoCommand_TrimsText()
    {
        // Arrange
        _sut.NewTodoText = "  Buy groceries  ";

        // Act
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.CapturedTodos[0].Text.Should().Be("Buy groceries");
    }

    [Fact]
    public void AddTodoCommand_SetsCorrectCapturedAt()
    {
        // Arrange
        var beforeAdd = DateTime.Now;
        _sut.NewTodoText = "Todo item";

        // Act
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.CapturedTodos[0].CapturedAt.Should().BeOnOrAfter(beforeAdd);
        _sut.CapturedTodos[0].CapturedAt.Should().BeOnOrBefore(DateTime.Now);
    }

    [Fact]
    public void AddTodoCommand_UpdatesHasCapturedTodos()
    {
        // Arrange
        _sut.HasCapturedTodos.Should().BeFalse();
        _sut.NewTodoText = "Todo item";

        // Act
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.HasCapturedTodos.Should().BeTrue();
    }

    [Fact]
    public void AddTodoCommand_UpdatesCapturedTodosCountText()
    {
        // Arrange
        _sut.NewTodoText = "First todo";
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.CapturedTodosCountText.Should().Be("1 todo");

        // Add another
        _sut.NewTodoText = "Second todo";
        _sut.AddTodoCommand.Execute(null);

        // Assert
        _sut.CapturedTodosCountText.Should().Be("2 todos");
    }

    #endregion

    #region CancelTodoInput Command

    [Fact]
    public void CancelTodoInputCommand_ClearsInputAndHides()
    {
        // Arrange
        _sut.NewTodoText = "Some text";
        _sut.IsTodoInputVisible = true;

        // Act
        _sut.CancelTodoInputCommand.Execute(null);

        // Assert
        _sut.NewTodoText.Should().BeEmpty();
        _sut.IsTodoInputVisible.Should().BeFalse();
    }

    #endregion

    #region Clear Todos on Session Start

    [Fact]
    public async Task StartFocusCommand_WhenConfirmed_ClearsPreviousTodos()
    {
        // Arrange - add some todos first
        _sut.NewTodoText = "Old todo";
        _sut.AddTodoCommand.Execute(null);
        _sut.CapturedTodos.Should().HaveCount(1);

        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, "New goal"));

        // Act
        await _sut.StartFocusCommand.ExecuteAsync(null);

        // Assert
        _sut.CapturedTodos.Should().BeEmpty();
        _sut.HasCapturedTodos.Should().BeFalse();
    }

    [Fact]
    public async Task StartFocusWithDurationCommand_WhenConfirmed_ClearsPreviousTodos()
    {
        // Arrange - add some todos first
        _sut.NewTodoText = "Old todo";
        _sut.AddTodoCommand.Execute(null);
        _sut.CapturedTodos.Should().HaveCount(1);

        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(true, "New goal"));

        // Act
        await _sut.StartFocusWithDurationCommand.ExecuteAsync("15");

        // Assert
        _sut.CapturedTodos.Should().BeEmpty();
        _sut.HasCapturedTodos.Should().BeFalse();
    }

    [Fact]
    public async Task StartFocusCommand_WhenCancelled_DoesNotClearTodos()
    {
        // Arrange - add some todos first
        _sut.NewTodoText = "Old todo";
        _sut.AddTodoCommand.Execute(null);
        _sut.CapturedTodos.Should().HaveCount(1);

        _dialogService.ShowGoalDialogAsync().Returns(new GoalDialogResult(false, null));

        // Act
        await _sut.StartFocusCommand.ExecuteAsync(null);

        // Assert - todos should remain (goal dialog was cancelled)
        _sut.CapturedTodos.Should().HaveCount(1);
    }

    #endregion

    #region Cancel Command Shows Todos

    [Fact]
    public async Task CancelCommand_WithTodos_ShowsTodosDialog()
    {
        // Arrange - simulate running focus session with todos
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        _sut.NewTodoText = "My todo";
        _sut.AddTodoCommand.Execute(null);

        // Act
        await _sut.CancelCommand.ExecuteAsync(null);

        // Assert - todos dialog should have been shown
        await _dialogService.Received(1).ShowTodosDialogAsync(
            Arg.Is<IReadOnlyList<TodoItem>>(todos => todos.Count == 1 && todos[0].Text == "My todo"));
    }

    [Fact]
    public async Task CancelCommand_WithoutTodos_DoesNotShowTodosDialog()
    {
        // Arrange - simulate running focus session without todos
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Act
        await _sut.CancelCommand.ExecuteAsync(null);

        // Assert - todos dialog should NOT have been shown
        await _dialogService.DidNotReceive().ShowTodosDialogAsync(Arg.Any<IReadOnlyList<TodoItem>>());
    }

    [Fact]
    public async Task CancelCommand_WithTodos_ClearsTodosAfterShowing()
    {
        // Arrange - simulate running focus session with todos
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        _sut.NewTodoText = "My todo";
        _sut.AddTodoCommand.Execute(null);
        _sut.CapturedTodos.Should().HaveCount(1);

        // Act
        await _sut.CancelCommand.ExecuteAsync(null);

        // Assert - todos should be cleared
        _sut.CapturedTodos.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelCommand_DuringBreak_DoesNotShowTodosDialog()
    {
        // Arrange - simulate running break session (shouldn't have todos anyway, but test the logic)
        var session = Session.CreateShortBreak();
        session.Status = SessionStatus.Running;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.NotStarted, SessionStatus.Running));

        // Act
        await _sut.CancelCommand.ExecuteAsync(null);

        // Assert - todos dialog should NOT have been shown (break session)
        await _dialogService.DidNotReceive().ShowTodosDialogAsync(Arg.Any<IReadOnlyList<TodoItem>>());
    }

    #endregion

    #region Todos Passed to Results Dialog

    [Fact]
    public async Task WhenFocusSessionCompletes_PassesTodosToResultsDialog()
    {
        // Arrange - add some todos
        _sut.NewTodoText = "Todo 1";
        _sut.AddTodoCommand.Execute(null);
        _sut.NewTodoText = "Todo 2";
        _sut.AddTodoCommand.Execute(null);

        _dialogService.ShowResultsDialogAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyList<TodoItem>?>())
            .Returns(new ResultsDialogResult(true, "Results", null));

        // Simulate focus session completion
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert
        await _dialogService.Received(1).ShowResultsDialogAsync(
            Arg.Any<string?>(),
            Arg.Is<IReadOnlyList<TodoItem>?>(todos =>
                todos != null && todos.Count == 2 &&
                todos[0].Text == "Todo 1" &&
                todos[1].Text == "Todo 2"));
    }

    [Fact]
    public async Task WhenFocusSessionCompletesWithNoTodos_PassesNullToResultsDialog()
    {
        // Arrange - no todos
        _sut.CapturedTodos.Should().BeEmpty();

        _dialogService.ShowResultsDialogAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyList<TodoItem>?>())
            .Returns(new ResultsDialogResult(true, "Results", null));

        // Simulate focus session completion
        var session = Session.CreateFocus();
        session.Status = SessionStatus.Completed;
        _sessionManager.CurrentSession.Returns(session);
        _sessionManager.SessionStateChanged += Raise.EventWith(
            new SessionStateChangedEventArgs(session, SessionStatus.Running, SessionStatus.Completed));

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert
        await _dialogService.Received(1).ShowResultsDialogAsync(
            Arg.Any<string?>(),
            Arg.Is<IReadOnlyList<TodoItem>?>(todos => todos == null));
    }

    #endregion
}
