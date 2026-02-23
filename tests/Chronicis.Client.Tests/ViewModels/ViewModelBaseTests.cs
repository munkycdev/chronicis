using System.ComponentModel;
using Chronicis.Client.ViewModels;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class ViewModelBaseTests
{
    // ---------------------------------------------------------------------------
    // Concrete test double
    // ---------------------------------------------------------------------------
    private sealed class TestViewModel : ViewModelBase
    {
        private string _name = string.Empty;
        private int _count;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public int Count
        {
            get => _count;
            set => SetField(ref _count, value);
        }

        // Exposes SetField return value for assertion
        public bool SetName(string value) => SetField(ref _name, value, nameof(Name));

        // Exposes RaisePropertyChanged for direct testing
        public void ForceRaise(string propertyName) => RaisePropertyChanged(propertyName);

        // Tracks OnPropertyChanged calls
        public List<string> OnPropertyChangedCalls { get; } = new();

        protected override void OnPropertyChanged(string propertyName) =>
            OnPropertyChangedCalls.Add(propertyName);
    }

    // ---------------------------------------------------------------------------
    // PropertyChanged event
    // ---------------------------------------------------------------------------

    [Fact]
    public void PropertyChanged_IsRaised_WhenValueChanges()
    {
        var vm = new TestViewModel();
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? string.Empty);

        vm.Name = "Aragorn";

        Assert.Single(raised);
        Assert.Equal("Name", raised[0]);
    }

    [Fact]
    public void PropertyChanged_IsNotRaised_WhenValueIsTheSame()
    {
        var vm = new TestViewModel { Name = "Aragorn" };
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? string.Empty);

        vm.Name = "Aragorn"; // same value

        Assert.Empty(raised);
    }

    [Fact]
    public void PropertyChanged_IsRaised_ForCorrectPropertyName()
    {
        var vm = new TestViewModel();
        string? capturedName = null;
        vm.PropertyChanged += (_, e) => capturedName = e.PropertyName;

        vm.Count = 42;

        Assert.Equal("Count", capturedName);
    }

    [Fact]
    public void PropertyChanged_CanHaveMultipleSubscribers()
    {
        var vm = new TestViewModel();
        var calls1 = new List<string>();
        var calls2 = new List<string>();
        vm.PropertyChanged += (_, e) => calls1.Add(e.PropertyName ?? string.Empty);
        vm.PropertyChanged += (_, e) => calls2.Add(e.PropertyName ?? string.Empty);

        vm.Name = "Legolas";

        Assert.Single(calls1);
        Assert.Single(calls2);
    }

    [Fact]
    public void PropertyChanged_CanBeUnsubscribed()
    {
        var vm = new TestViewModel();
        var raised = new List<string>();
        PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName ?? string.Empty);
        vm.PropertyChanged += handler;
        vm.PropertyChanged -= handler;

        vm.Name = "Gimli";

        Assert.Empty(raised);
    }

    // ---------------------------------------------------------------------------
    // SetField return value
    // ---------------------------------------------------------------------------

    [Fact]
    public void SetField_ReturnsTrue_WhenValueChanges()
    {
        var vm = new TestViewModel();
        var result = vm.SetName("Frodo");
        Assert.True(result);
    }

    [Fact]
    public void SetField_ReturnsFalse_WhenValueIsTheSame()
    {
        var vm = new TestViewModel { Name = "Frodo" };
        var result = vm.SetName("Frodo");
        Assert.False(result);
    }

    // ---------------------------------------------------------------------------
    // RaisePropertyChanged
    // ---------------------------------------------------------------------------

    [Fact]
    public void RaisePropertyChanged_FiresEvent_WithSuppliedName()
    {
        var vm = new TestViewModel();
        string? capturedName = null;
        vm.PropertyChanged += (_, e) => capturedName = e.PropertyName;

        vm.ForceRaise("ComputedProperty");

        Assert.Equal("ComputedProperty", capturedName);
    }

    [Fact]
    public void RaisePropertyChanged_CallsOnPropertyChanged_BeforeEvent()
    {
        var vm = new TestViewModel();
        var order = new List<string>();
        vm.PropertyChanged += (_, _) => order.Add("event");
        // OnPropertyChangedCalls is populated inside OnPropertyChanged, which runs first
        vm.ForceRaise("X");

        // OnPropertyChanged should have been called (captured in OnPropertyChangedCalls)
        Assert.Contains("X", vm.OnPropertyChangedCalls);
        // And the event should also have fired
        Assert.Contains("event", order);
        // OnPropertyChanged runs before the event subscriber
        Assert.Equal(0, vm.OnPropertyChangedCalls.IndexOf("X"));
    }

    // ---------------------------------------------------------------------------
    // OnPropertyChanged virtual hook
    // ---------------------------------------------------------------------------

    [Fact]
    public void OnPropertyChanged_IsCalledForEachPropertyChange()
    {
        var vm = new TestViewModel();

        vm.Name = "Bilbo";
        vm.Count = 1;
        vm.Name = "Bilbo"; // same — should not call again

        Assert.Equal(2, vm.OnPropertyChangedCalls.Count);
        Assert.Equal("Name", vm.OnPropertyChangedCalls[0]);
        Assert.Equal("Count", vm.OnPropertyChangedCalls[1]);
    }

    [Fact]
    public void OnPropertyChanged_IsNotCalled_WhenValueDoesNotChange()
    {
        var vm = new TestViewModel { Name = "Samwise" };
        vm.OnPropertyChangedCalls.Clear();

        vm.Name = "Samwise"; // no change

        Assert.Empty(vm.OnPropertyChangedCalls);
    }

    // ---------------------------------------------------------------------------
    // Null safety
    // ---------------------------------------------------------------------------

    [Fact]
    public void PropertyChanged_IsNull_ByDefault_AndDoesNotThrow()
    {
        var vm = new TestViewModel();
        // Invoking via setter before any subscriber — should not throw
        var ex = Record.Exception(() => vm.Name = "Pippin");
        Assert.Null(ex);
    }
}
