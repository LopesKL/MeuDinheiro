namespace Application.Dto.Finance;

public class DashboardSummaryDto
{
    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }
    public decimal MonthBalance { get; set; }
    public decimal TotalPatrimony { get; set; }
    public decimal TotalDebtBalance { get; set; }
    public List<MonthlyFlowDto> LastMonthsFlow { get; set; } = new();
    /// <summary>Gastos do mês selecionado agregados por categoria (para gráfico).</summary>
    public List<MonthExpenseCategorySliceDto> MonthExpensesByCategory { get; set; } = new();
}

public class MonthExpenseCategorySliceDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Despesas, parcelas do mês e recorrentes previstos que compõem o total (espelha o gráfico).</summary>
    public List<MonthExpenseCategoryLineDto> Items { get; set; } = new();
}

public class MonthExpenseCategoryLineDto
{
    /// <summary>expense | installment | recurring</summary>
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? Date { get; set; }
}

public class MonthlyFlowDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}
