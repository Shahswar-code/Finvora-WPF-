using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using System;

namespace Finvora.ViewModels
{
    /// <summary>Read-only view of one Installment's schedule. Collect Payment
    /// (Phase 2) will add the ability to act on a row from here.</summary>
    public partial class InstallmentScheduleViewModel : ObservableObject
    {
        public event Action? RequestClose;

        public Installment Installment { get; }
        public ObservableCollection<InstallmentSchedule> Rows { get; }

        public InstallmentScheduleViewModel(Installment installment)
        {
            Installment = installment;
            Rows = new ObservableCollection<InstallmentSchedule>(installment.Schedule);
        }

        [RelayCommand]
        private void Close() => RequestClose?.Invoke();
    }
} 