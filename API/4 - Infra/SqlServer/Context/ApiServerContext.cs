using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project.Entities;
using Project.Entities.Finance;
using SqlServer.Finance;
using SqlServer.Interfaces;

namespace SqlServer.Context;

public class ApiServerContext : IdentityDbContext<AppUser, AppRole, string, IdentityUserClaim<string>,
    AppUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>, IDbContext
{
    public ApiServerContext(DbContextOptions<ApiServerContext> options) : base(options)
    {
    }

    public DbSet<Crud> Crud { get; set; } = null!;

    public DbSet<Category> FinanceCategories => Set<Category>();
    public DbSet<Expense> FinanceExpenses => Set<Expense>();
    public DbSet<Income> FinanceIncomes => Set<Income>();
    public DbSet<CreditCard> FinanceCreditCards => Set<CreditCard>();
    public DbSet<InstallmentPlan> FinanceInstallmentPlans => Set<InstallmentPlan>();
    public DbSet<Installment> FinanceInstallments => Set<Installment>();
    public DbSet<RecurringExpense> FinanceRecurringExpenses => Set<RecurringExpense>();
    public DbSet<RecurringExpenseAmountSchedule> FinanceRecurringAmountSchedules => Set<RecurringExpenseAmountSchedule>();
    public DbSet<Debt> FinanceDebts => Set<Debt>();
    public DbSet<Account> FinanceAccounts => Set<Account>();
    public DbSet<PatrimonyMonthlySnapshot> FinancePatrimonySnapshots => Set<PatrimonyMonthlySnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<AppRole>().HasData(
            new AppRole { Id = "1", Name = "RoleAdmin", NormalizedName = "ROLEADMIN" },
            new AppRole { Id = "2", Name = "RoleUser", NormalizedName = "ROLEUSER" }
        );

        builder.Entity<Crud>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.UpdatedBy);
            entity.HasIndex(e => e.IsDeleted);
        });

        builder.ConfigureFinance();
    }
}
