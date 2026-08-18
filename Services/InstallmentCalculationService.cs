using System;
using System.Collections.Generic;
using Finvora.Models;

namespace Finvora.Services
{
    /// <summary>
    /// Pure math -- no database access. Generates a payment schedule and keeps
    /// "amount vs count" in sync in the Create Installment form: whichever field
    /// the user typed last drives the other. The final installment always
    /// absorbs any rounding remainder, so the schedule reconciles exactly to
    /// the financed amount.
    /// </summary>
    public static class InstallmentCalculationService
    {
        public static List<InstallmentSchedule> GenerateSchedule(
            decimal financedAmount, int numberOfInstallments, decimal installmentAmount,
            DateTime firstDueDate, PlanFrequency frequency)
        {
            var schedule = new List<InstallmentSchedule>();
            if (numberOfInstallments <= 0) return schedule;

            decimal allocated = 0;
            var dueDate = firstDueDate;

            for (int i = 1; i <= numberOfInstallments; i++)
            {
                decimal amount = i == numberOfInstallments
                    ? Math.Round(financedAmount - allocated, 2)   // last one absorbs rounding
                    : Math.Round(installmentAmount, 2);

                if (amount < 0) amount = 0;

                schedule.Add(new InstallmentSchedule
                {
                    SequenceNumber = i,
                    DueDate = dueDate,
                    Amount = amount
                });

                allocated += amount;
                dueDate = Advance(dueDate, frequency);
            }

            return schedule;
        }

        public static DateTime Advance(DateTime date, PlanFrequency frequency) => frequency switch
        {
            PlanFrequency.Daily => date.AddDays(1),
            PlanFrequency.Weekly => date.AddDays(7),
            PlanFrequency.Biweekly => date.AddDays(14),
            PlanFrequency.Monthly => date.AddMonths(1),
            PlanFrequency.Yearly => date.AddYears(1),
            _ => date.AddMonths(1)
        };

        public static int CountFromAmount(decimal financedAmount, decimal installmentAmount) =>
            installmentAmount <= 0 ? 0 : (int)Math.Ceiling(financedAmount / installmentAmount);

        public static decimal AmountFromCount(decimal financedAmount, int numberOfInstallments) =>
            numberOfInstallments <= 0 ? 0 : Math.Round(financedAmount / numberOfInstallments, 2);
    }
}  