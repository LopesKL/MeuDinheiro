namespace Application.Dto.Finance;

public class ProjectionRequestDto
{
    public int MonthsAhead { get; set; } = 12;
}

public class ProjectionMonthDto
{
    public string Label { get; set; } = string.Empty;
    public decimal ProjectedBalance { get; set; }
    public decimal CumulativeIncome { get; set; }
    public decimal CumulativeExpense { get; set; }
}

public class ProjectionResultDto
{
    public List<ProjectionMonthDto> Months { get; set; } = new();
    public string Notes { get; set; } = "Projeção baseada em médias de renda, gastos fixos, parcelas em aberto e dívidas.";
}

/// <summary>Simulação sandbox — não persiste dados.</summary>
public class SandboxProjectionRequestDto
{
    public decimal MonthlyIncomeAverage { get; set; }
    public decimal MonthlyFixedExpenses { get; set; }
    public decimal MonthlyVariableExpensesAverage { get; set; }
    public decimal OutstandingInstallmentsTotal { get; set; }
    public decimal DebtMonthlyPayments { get; set; }
    public decimal CurrentLiquidPatrimony { get; set; }
    public int MonthsAhead { get; set; } = 12;
}
