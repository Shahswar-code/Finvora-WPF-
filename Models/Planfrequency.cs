namespace Finvora.Models
{
    /// <summary>
    /// How often an installment is due. Set once when the plan is created (Step F form).
    /// </summary>
    public enum PlanFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }
}  