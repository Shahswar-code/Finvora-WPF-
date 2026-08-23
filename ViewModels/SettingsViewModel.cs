using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using Finvora.Services.Startup.Tasks;
using Finvora.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Backs the Settings page: Business Profile, Appearance (theme), Backup/Restore,
    /// and the PIN-protected Reset All Data / Change PIN flows.
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly BackupService _backupService;
        private readonly SecurityService _securityService;
        private readonly ThemeService _themeService;

        // ---------- Business Profile ----------
        [ObservableProperty] private string businessName = string.Empty;
        [ObservableProperty] private string ownerName = string.Empty;
        [ObservableProperty] private string contactPhone = string.Empty;
        [ObservableProperty] private string contactAddress = string.Empty;
        [ObservableProperty] private string currencySymbol = string.Empty;

        [ObservableProperty] private string saveMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        // ---------- Appearance ----------
        public ObservableCollection<ThemeOption> AvailableThemes { get; } = new()
        {
            new ThemeOption(AppTheme.DarkNavy,       "Dark Navy",       "#0B1120", "#8B5CF6"),
            new ThemeOption(AppTheme.Light,          "Light",           "#F8FAFC", "#7C3AED"),
            new ThemeOption(AppTheme.CyberDark,      "Cyber Dark",      "#05050A", "#FF00E5"),
            new ThemeOption(AppTheme.MidnightPurple, "Midnight Purple", "#120B1F", "#EC4899"),
            new ThemeOption(AppTheme.EmeraldForest,  "Emerald Forest",  "#0A1712", "#0D9488"),
        };

        public SettingsViewModel(SettingsService settingsService, BackupService backupService,
            SecurityService securityService, ThemeService themeService)
        {
            _settingsService = settingsService;
            _backupService = backupService;
            _securityService = securityService;
            _themeService = themeService;

            var current = _settingsService.Current;
            BusinessName = current.BusinessName;
            OwnerName = current.OwnerName;
            ContactPhone = current.ContactPhone;
            ContactAddress = current.ContactAddress;
            CurrencySymbol = current.CurrencySymbol;

            MarkSelectedTheme(_themeService.Current);
        }

        [RelayCommand]
        private void SaveProfile()
        {
            SaveMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(BusinessName))
            {
                SaveMessage = "Business name can't be empty.";
                return;
            }

            var updated = new BusinessSettings
            {
                BusinessName = BusinessName.Trim(),
                OwnerName = OwnerName.Trim(),
                ContactPhone = ContactPhone.Trim(),
                ContactAddress = ContactAddress.Trim(),
                CurrencySymbol = string.IsNullOrWhiteSpace(CurrencySymbol) ? "Rs" : CurrencySymbol.Trim(),
                Theme = _themeService.Current
            };

            _settingsService.Save(updated);
            SaveMessage = "Business profile saved.";
        }

        // ---------- Appearance ----------
        [RelayCommand]
        private void SelectTheme(ThemeOption option)
        {
            if (option.Theme == _themeService.Current) return;

            _themeService.ApplyTheme(option.Theme);
            MarkSelectedTheme(option.Theme);

            var current = _settingsService.Current;
            var updated = new BusinessSettings
            {
                BusinessName = current.BusinessName,
                OwnerName = current.OwnerName,
                ContactPhone = current.ContactPhone,
                ContactAddress = current.ContactAddress,
                CurrencySymbol = current.CurrencySymbol,
                Theme = option.Theme
            };

            _settingsService.Save(updated);
        }

        private void MarkSelectedTheme(AppTheme theme)
        {
            foreach (var option in AvailableThemes)
                option.IsSelected = option.Theme == theme;
        }

        // ---------- Backup ----------
        [RelayCommand]
        private async Task BackupData()
        {
            SaveMessage = string.Empty;
            IsBusy = true;
            try
            {
                var path = await _backupService.BackupAsync();
                SaveMessage = $"Backup saved: {path}";
            }
            catch (Exception ex)
            {
                SaveMessage = $"Backup failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------- Restore ----------
        [RelayCommand]
        private async Task RestoreData()
        {
            SaveMessage = string.Empty;

            var dialog = new OpenFileDialog
            {
                Title = "Select Backup File",
                InitialDirectory = EnsureAppDataFolderTask.BackupsFolderPath,
                Filter = "Backup file (*.bak)|*.bak"
            };

            if (dialog.ShowDialog() != true) return;

            var confirm = MessageBox.Show(
                "Restoring will replace ALL current data with this backup. This cannot be undone. Continue?",
                "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await _backupService.RestoreAsync(dialog.FileName);
                SaveMessage = "Restore complete. Please restart Finvora for the changes to fully apply.";
            }
            catch (Exception ex)
            {
                SaveMessage = $"Restore failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------- Reset all data (PIN protected) ----------
        [RelayCommand]
        private async Task ResetAllData()
        {
            SaveMessage = string.Empty;

            var hasPin = await _securityService.HasPinAsync();

            if (!hasPin)
            {
                var setupVm = new PinDialogViewModel(_securityService, PinDialogMode.SetupNew);
                var setupWindow = new PinDialogWindow(setupVm) { Owner = Application.Current.MainWindow };
                setupWindow.ShowDialog();

                if (setupWindow.Result == true)
                {
                    SaveMessage = "PIN created. Click \"Reset All Data\" again to proceed with the reset.";
                }
                return;
            }

            var verifyVm = new PinDialogViewModel(_securityService, PinDialogMode.Verify);
            var verifyWindow = new PinDialogWindow(verifyVm) { Owner = Application.Current.MainWindow };
            verifyWindow.ShowDialog();

            if (verifyWindow.Result != true) return;

            var finalConfirm = MessageBox.Show(
                "This will permanently delete ALL customers, plans, and payment records. This cannot be undone. Are you absolutely sure?",
                "Confirm Full Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (finalConfirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await _backupService.ResetAsync();
                SaveMessage = "All data has been reset. Please restart Finvora.";
            }
            catch (Exception ex)
            {
                SaveMessage = $"Reset failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------- Change PIN ----------
        [RelayCommand]
        private async Task ChangePin()
        {
            SaveMessage = string.Empty;

            var hasPin = await _securityService.HasPinAsync();
            var mode = hasPin ? PinDialogMode.Change : PinDialogMode.SetupNew;

            var vm = new PinDialogViewModel(_securityService, mode);
            var window = new PinDialogWindow(vm) { Owner = Application.Current.MainWindow };
            window.ShowDialog();

            if (window.Result == true)
            {
                SaveMessage = hasPin ? "PIN updated." : "PIN created.";
            }
        }
    }
} 