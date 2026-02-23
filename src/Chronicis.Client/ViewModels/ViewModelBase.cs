using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// Base class for all Chronicis ViewModels.
/// Implements <see cref="INotifyPropertyChanged"/> with a SetField helper
/// so that Blazor components can subscribe to property changes and call
/// <see cref="Microsoft.AspNetCore.Components.ComponentBase.StateHasChanged"/>
/// without pulling MudBlazor or rendering infrastructure into ViewModels.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
    /// <see cref="PropertyChanged"/> if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">
    /// The property name, automatically provided by the compiler via
    /// <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value changed and <see cref="PropertyChanged"/> was raised;
    /// <c>false</c> if the value was already equal.
    /// </returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for the specified property name.
    /// Use this when a computed/dependent property needs to notify separately.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void RaisePropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Called before <see cref="PropertyChanged"/> is raised.
    /// Override in subclasses to react to specific property changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
    }
}
