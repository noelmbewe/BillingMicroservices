namespace BillingService.Domain.ValueObjects;

public record BillingPlan(
    string PlanName,
    string PlanCode,
    string Interval,
    string Currency,
    string Description,
    List<ChargeModel> Charges
);

public record ChargeModel(
    string Model,
    string Interval,
    int FirstUnit,
    int LastUnit,
    decimal PerUnit,
    decimal FlatFee
);