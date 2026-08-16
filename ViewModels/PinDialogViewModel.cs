using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Finvora.ViewModels
{
    public enum PinDialogMode { SetupNew, Verify, Change }

    /// <summary>
    /// One flexible ViewModel backing all three PIN dialogs (first-time setup,
    /// verify-before-reset, and change-existing-PIN) -- the mode controls which
    /// fields the window shows, avoiding three near-duplicate ViewModels.
    /// </summary>
    public partial class PinDialogViewModel : ObservableObject
    {
        private readonly SecurityService _securityService;

        public PinDialogMode Mode { get; }

        /// <summary>Raised on close. true = PIN action succeeded, false = cancelled.</summary>
        public event Action<bool>? RequestClose;

        public string HeaderTitle => Mode switch
        {
            PinDialogMode.SetupNew => "Set a Security PIN",
            PinDialogMode.Verify => "Enter PIN to Confirm",
            PinDialogMode.Change => "Change Security PIN",
            _ => "PIN"
        };

        public string HeaderSubtitle => Mode switch
        {
            PinDialogMode.SetupNew => "This PIN will be required before resetting all data.",
            PinDialogMode.Verify => "Enter your PIN to continue with this action.",
            PinDialogMode.Change => "Enter your current PIN, then choose a new one.",
            _ => string.Empty
        };

        public bool ShowCurrentPin => Mode == PinDialogMode.Change;
        public bool ShowNewPinFields => Mode is PinDialogMode.SetupNew or PinDialogMode.Change;
        public bool ShowVerifyField => Mode == PinDialogMode.Verify;

        [ObservableProperty] private string currentPin = string.Empty;
        [ObservableProperty] private string newPin = string.Empty;
        [ObservableProperty] private string confirmPin = string.Empty;
        [ObservableProperty] private string verifyPin = string.Empty;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public PinDialogViewModel(SecurityService securityService, PinDialogMode mode)
        {
            _securityService = securityService;
            Mode = mode;
        }

        [RelayCommand]
        private async Task Confirm()
        {
            ErrorMessage = string.Empty;

            if (Mode == PinDialogMode.Verify)
            {
                if (string.IsNullOrWhiteSpace(VerifyPin)) { ErrorMessage = "Enter your PIN."; return; }

                IsBusy = true;
                var ok = await _securityService.VerifyPinAsync(VerifyPin);
                IsBusy = false;

                if (!ok) { ErrorMessage = "Incorrect PIN."; return; }

                RequestClose?.Invoke(true);
                return;
            }

            if (Mode == PinDialogMode.Change)
            {
                if (string.IsNullOrWhiteSpace(CurrentPin)) { ErrorMessage = "Enter your current PIN."; return; }

                IsBusy = true;
                var currentOk = await _securityService.VerifyPinAsync(CurrentPin);
                IsBusy = false;

                if (!currentOk) { ErrorMessage = "Current PIN is incorrect."; return; }
            }

            // SetupNew and Change both end here: validate + save the new PIN.
            if (!IsValidPin(NewPin)) { ErrorMessage = "PIN must be 4-6 digits."; return; }
            if (NewPin != ConfirmPin) { ErrorMessage = "PINs don't match."; return; }

            IsBusy = true;
            await _securityService.SetPinAsync(NewPin);
            IsBusy = false;

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        private static bool IsValidPin(string pin) =>
            !string.IsNullOrWhiteSpace(pin) && pin.Length is >= 4 and <= 6 && pin.All(char.IsDigit);
    }
} 