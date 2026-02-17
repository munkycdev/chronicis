using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for the QuestStatus enum to ensure all expected values exist
/// and can be properly parsed and converted.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuestStatusTests
{
    [Fact]
    public void QuestStatus_HasActive()
    {
        var value = QuestStatus.Active;
        Assert.Equal(0, (int)value);
    }

    [Fact]
    public void QuestStatus_HasCompleted()
    {
        var value = QuestStatus.Completed;
        Assert.Equal(1, (int)value);
    }

    [Fact]
    public void QuestStatus_HasFailed()
    {
        var value = QuestStatus.Failed;
        Assert.Equal(2, (int)value);
    }

    [Fact]
    public void QuestStatus_HasAbandoned()
    {
        var value = QuestStatus.Abandoned;
        Assert.Equal(3, (int)value);
    }

    [Fact]
    public void QuestStatus_GetValues_ReturnsAllExpectedValues()
    {
        var values = Enum.GetValues<QuestStatus>();

        Assert.Equal(4, values.Length);
        Assert.Contains(QuestStatus.Active, values);
        Assert.Contains(QuestStatus.Completed, values);
        Assert.Contains(QuestStatus.Failed, values);
        Assert.Contains(QuestStatus.Abandoned, values);
    }

    [Fact]
    public void QuestStatus_GetNames_ReturnsCorrectNames()
    {
        var names = Enum.GetNames<QuestStatus>();

        Assert.Equal(4, names.Length);
        Assert.Contains("Active", names);
        Assert.Contains("Completed", names);
        Assert.Contains("Failed", names);
        Assert.Contains("Abandoned", names);
    }

    [Theory]
    [InlineData("Active", QuestStatus.Active)]
    [InlineData("Completed", QuestStatus.Completed)]
    [InlineData("Failed", QuestStatus.Failed)]
    [InlineData("Abandoned", QuestStatus.Abandoned)]
    public void QuestStatus_Parse_ParsesCorrectly(string name, QuestStatus expected)
    {
        var result = Enum.Parse<QuestStatus>(name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("active", QuestStatus.Active)]
    [InlineData("COMPLETED", QuestStatus.Completed)]
    [InlineData("Failed", QuestStatus.Failed)]
    public void QuestStatus_Parse_IsCaseInsensitive(string name, QuestStatus expected)
    {
        var result = Enum.Parse<QuestStatus>(name, ignoreCase: true);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void QuestStatus_Parse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Enum.Parse<QuestStatus>("InvalidStatus"));
    }

    [Theory]
    [InlineData(QuestStatus.Active, "Active")]
    [InlineData(QuestStatus.Completed, "Completed")]
    [InlineData(QuestStatus.Failed, "Failed")]
    [InlineData(QuestStatus.Abandoned, "Abandoned")]
    public void QuestStatus_ToString_ReturnsCorrectName(QuestStatus value, string expected)
    {
        Assert.Equal(expected, value.ToString());
    }

    [Theory]
    [InlineData(0, QuestStatus.Active)]
    [InlineData(1, QuestStatus.Completed)]
    [InlineData(2, QuestStatus.Failed)]
    [InlineData(3, QuestStatus.Abandoned)]
    public void QuestStatus_CastFromInt_Works(int value, QuestStatus expected)
    {
        var result = (QuestStatus)value;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void QuestStatus_IsDefined_ReturnsTrueForValidValues()
    {
        Assert.True(Enum.IsDefined(typeof(QuestStatus), QuestStatus.Active));
        Assert.True(Enum.IsDefined(typeof(QuestStatus), QuestStatus.Completed));
        Assert.True(Enum.IsDefined(typeof(QuestStatus), QuestStatus.Failed));
        Assert.True(Enum.IsDefined(typeof(QuestStatus), QuestStatus.Abandoned));
    }

    [Fact]
    public void QuestStatus_IsDefined_ReturnsFalseForInvalidValues()
    {
        Assert.False(Enum.IsDefined(typeof(QuestStatus), 4));
        Assert.False(Enum.IsDefined(typeof(QuestStatus), -1));
        Assert.False(Enum.IsDefined(typeof(QuestStatus), 99));
    }

    [Fact]
    public void QuestStatus_DefaultValue_IsActive()
    {
        var defaultValue = default(QuestStatus);
        Assert.Equal(QuestStatus.Active, defaultValue);
        Assert.Equal(0, (int)defaultValue);
    }

    [Theory]
    [InlineData("Active", true)]
    [InlineData("Completed", true)]
    [InlineData("Failed", true)]
    [InlineData("Abandoned", true)]
    [InlineData("InvalidStatus", false)]
    [InlineData("", false)]
    public void QuestStatus_TryParse_WorksCorrectly(string name, bool shouldSucceed)
    {
        var result = Enum.TryParse<QuestStatus>(name, out var value);
        Assert.Equal(shouldSucceed, result);

        if (shouldSucceed)
        {
            Assert.True(Enum.IsDefined(typeof(QuestStatus), value));
        }
    }

    [Fact]
    public void QuestStatus_AllValuesCovered()
    {
        // Ensure no gaps in the sequence 0, 1, 2, 3
        var values = Enum.GetValues<QuestStatus>().Select(v => (int)v).OrderBy(v => v).ToList();

        Assert.Equal(0, values[0]);
        Assert.Equal(1, values[1]);
        Assert.Equal(2, values[2]);
        Assert.Equal(3, values[3]);

        // No gaps
        for (int i = 0; i < values.Count - 1; i++)
        {
            Assert.Equal(1, values[i + 1] - values[i]);
        }
    }

    [Theory]
    [InlineData(QuestStatus.Active, false)]
    [InlineData(QuestStatus.Completed, true)]
    [InlineData(QuestStatus.Failed, true)]
    [InlineData(QuestStatus.Abandoned, true)]
    public void QuestStatus_IsTerminalState_CorrectlyIdentifiesEndStates(QuestStatus status, bool isTerminal)
    {
        // Active is the only non-terminal state
        // All others represent end states of a quest
        var actuallyTerminal = status != QuestStatus.Active;
        Assert.Equal(isTerminal, actuallyTerminal);
    }

    [Fact]
    public void QuestStatus_ActiveIsOnlyOngoingState()
    {
        // Verify that Active (0) is the only "in progress" state
        // All other states represent completed lifecycle
        var ongoingStates = Enum.GetValues<QuestStatus>()
            .Where(s => s == QuestStatus.Active)
            .ToList();

        Assert.Single(ongoingStates);
        Assert.Equal(QuestStatus.Active, ongoingStates[0]);
    }

    [Theory]
    [InlineData(QuestStatus.Completed, true)]
    [InlineData(QuestStatus.Failed, false)]
    [InlineData(QuestStatus.Abandoned, false)]
    [InlineData(QuestStatus.Active, false)]
    public void QuestStatus_IsSuccessfulCompletion_OnlyTrueForCompleted(QuestStatus status, bool isSuccess)
    {
        var actualSuccess = status == QuestStatus.Completed;
        Assert.Equal(isSuccess, actualSuccess);
    }
}
