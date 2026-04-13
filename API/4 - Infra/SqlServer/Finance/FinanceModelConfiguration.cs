using Microsoft.EntityFrameworkCore;
using Project.Entities.Finance;

namespace SqlServer.Finance;

public static class FinanceModelConfiguration
{
    public static void ConfigureFinance(this ModelBuilder builder)
    {
        builder.Entity<Category>(e =>
        {
            e.ToTable("fin_categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<CreditCard>(e =>
        {
            e.ToTable("fin_credit_cards");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.ThemeColor).HasMaxLength(32);
            e.Property(x => x.MealVoucherDailyAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<Account>(e =>
        {
            e.ToTable("fin_accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(8);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<Debt>(e =>
        {
            e.ToTable("fin_debts");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Balance);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.MonthlyPayment).HasPrecision(18, 2);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<Income>(e =>
        {
            e.ToTable("fin_incomes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Source).HasMaxLength(256);
            e.Property(x => x.Description).HasMaxLength(512);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.ReferenceMonth });
            e.HasOne<CreditCard>().WithMany().HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InstallmentPlan>(e =>
        {
            e.ToTable("fin_installment_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreditCard).WithMany().HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Installments).WithOne(x => x.InstallmentPlan).HasForeignKey(x => x.InstallmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Installment>(e =>
        {
            e.ToTable("fin_installments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.InstallmentPlanId);
            e.HasIndex(x => x.ExpenseId);
            e.HasOne(x => x.Expense).WithMany().HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Expense>(e =>
        {
            e.ToTable("fin_expenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(1024);
            e.Property(x => x.StoreLocation).HasMaxLength(512);
            e.Property(x => x.ImagePath).HasMaxLength(1024);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasIndex(x => x.CreditCardId);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreditCard).WithMany().HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InstallmentPlan).WithMany().HasForeignKey(x => x.InstallmentPlanId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<RecurringExpense>().WithMany().HasForeignKey(x => x.RecurringExpenseId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RecurringExpense>(e =>
        {
            e.ToTable("fin_recurring_expenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(512);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.Active, x.DayOfMonth });
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreditCard).WithMany().HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RecurringExpenseAmountSchedule>(e =>
        {
            e.ToTable("fin_recurring_amount_schedules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.RecurringExpenseId, x.EffectiveFrom });
            e.HasOne<RecurringExpense>().WithMany().HasForeignKey(x => x.RecurringExpenseId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PatrimonyMonthlySnapshot>(e =>
        {
            e.ToTable("fin_patrimony_snapshots");
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalBalance).HasPrecision(18, 2);
            e.HasIndex(x => new { x.UserId, x.Year, x.Month }).IsUnique();
        });
    }
}
