using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.Extensions;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the Getting Started / onboarding wizard.
/// Owns step navigation state and the async completion flow.
/// </summary>
public sealed class GettingStartedViewModel : ViewModelBase
{
    /// <summary>Total number of wizard steps.</summary>
    public const int TotalSteps = 4;

    private readonly IUserApiService _userApi;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<GettingStartedViewModel> _logger;

    private int _currentStep = 0;
    private bool _isCompleting = false;
    private bool _isReturningUser = false;

    /// <summary>Zero-based index of the currently displayed step (0 – <see cref="TotalSteps"/> − 1).</summary>
    public int CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    /// <summary>Whether the completion async operation is in-flight.</summary>
    public bool IsCompleting
    {
        get => _isCompleting;
        private set => SetField(ref _isCompleting, value);
    }

    /// <summary>
    /// Whether the user has previously completed onboarding.
    /// Affects button labels and skip-to-end behaviour.
    /// </summary>
    public bool IsReturningUser
    {
        get => _isReturningUser;
        private set => SetField(ref _isReturningUser, value);
    }

    public GettingStartedViewModel(
        IUserApiService userApi,
        IAppNavigator navigator,
        IUserNotifier notifier,
        ILogger<GettingStartedViewModel> logger)
    {
        _userApi = userApi;
        _navigator = navigator;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>
    /// Loads the user profile to determine whether this is a returning user.
    /// Call from <c>OnInitializedAsync</c>.
    /// </summary>
    public async Task InitializeAsync()
    {
        var profile = await _userApi.GetUserProfileAsync();
        IsReturningUser = profile?.HasCompletedOnboarding == true;
    }

    /// <summary>Advances to the next step, clamped at the last step.</summary>
    public void NextStep()
    {
        if (CurrentStep < TotalSteps - 1)
            CurrentStep++;
    }

    /// <summary>Returns to the previous step, clamped at step 0.</summary>
    public void PreviousStep()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    /// <summary>Jumps directly to <paramref name="step"/> if it is within bounds.</summary>
    public void GoToStep(int step)
    {
        if (step >= 0 && step < TotalSteps)
            CurrentStep = step;
    }

    /// <summary>
    /// Completes onboarding (or, for returning users, simply navigates back to the dashboard).
    /// Marks the user's profile as having completed onboarding before navigating.
    /// </summary>
    public async Task CompleteOnboardingAsync()
    {
        if (IsReturningUser)
        {
            _navigator.NavigateTo("/dashboard");
            return;
        }

        IsCompleting = true;
        try
        {
            var success = await _userApi.CompleteOnboardingAsync();
            if (success)
            {
                _navigator.NavigateTo("/dashboard", replace: true);
            }
            else
            {
                _notifier.Error("Failed to complete setup. Please try again.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error completing onboarding");
            _notifier.Error("An error occurred. Please try again.");
        }
        finally
        {
            IsCompleting = false;
        }
    }
}
