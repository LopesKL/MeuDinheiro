namespace Project.Entities.Finance;

/// <summary>Origem do lançamento de despesa (persistido para filtros e relatórios).</summary>
public enum ExpenseCreationSource
{
    Unspecified = 0,
    QuickLaunch = 1,
    UploadReceipt = 2,
}
