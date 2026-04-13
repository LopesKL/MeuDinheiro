using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class FinancePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fin_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsExpense = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_credit_cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClosingDay = table.Column<int>(type: "integer", nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false),
                    ThemeColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_credit_cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_debts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MonthlyPayment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_debts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_patrimony_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TotalBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_patrimony_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_incomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ReferenceMonth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_incomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_incomes_fin_credit_cards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "fin_credit_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fin_installment_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_installment_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_installment_plans_fin_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "fin_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fin_installment_plans_fin_credit_cards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "fin_credit_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fin_recurring_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastGeneratedMonth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_recurring_expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_recurring_expenses_fin_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "fin_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fin_recurring_expenses_fin_credit_cards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "fin_credit_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fin_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    StoreLocation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstallmentPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecurringExpenseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImagePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreationSource = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_expenses_fin_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "fin_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fin_expenses_fin_credit_cards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "fin_credit_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fin_expenses_fin_installment_plans_InstallmentPlanId",
                        column: x => x.InstallmentPlanId,
                        principalTable: "fin_installment_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_fin_expenses_fin_recurring_expenses_RecurringExpenseId",
                        column: x => x.RecurringExpenseId,
                        principalTable: "fin_recurring_expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "fin_recurring_amount_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RecurringExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_recurring_amount_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_recurring_amount_schedules_fin_recurring_expenses_Recur~",
                        column: x => x.RecurringExpenseId,
                        principalTable: "fin_recurring_expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fin_installments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_installments_fin_expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "fin_expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_fin_installments_fin_installment_plans_InstallmentPlanId",
                        column: x => x.InstallmentPlanId,
                        principalTable: "fin_installment_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fin_accounts_UserId",
                table: "fin_accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_categories_UserId",
                table: "fin_categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_credit_cards_UserId",
                table: "fin_credit_cards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_debts_UserId",
                table: "fin_debts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_expenses_CategoryId",
                table: "fin_expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_expenses_CreditCardId",
                table: "fin_expenses",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_expenses_InstallmentPlanId",
                table: "fin_expenses",
                column: "InstallmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_expenses_RecurringExpenseId",
                table: "fin_expenses",
                column: "RecurringExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_expenses_UserId",
                table: "fin_expenses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_expenses_UserId_Date",
                table: "fin_expenses",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_incomes_CreditCardId",
                table: "fin_incomes",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_incomes_UserId",
                table: "fin_incomes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_incomes_UserId_ReferenceMonth",
                table: "fin_incomes",
                columns: new[] { "UserId", "ReferenceMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_installment_plans_CategoryId",
                table: "fin_installment_plans",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_installment_plans_CreditCardId",
                table: "fin_installment_plans",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_installment_plans_UserId",
                table: "fin_installment_plans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_installments_ExpenseId",
                table: "fin_installments",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_installments_InstallmentPlanId",
                table: "fin_installments",
                column: "InstallmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_patrimony_snapshots_UserId_Year_Month",
                table: "fin_patrimony_snapshots",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fin_recurring_amount_schedules_RecurringExpenseId_Effective~",
                table: "fin_recurring_amount_schedules",
                columns: new[] { "RecurringExpenseId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_recurring_amount_schedules_UserId",
                table: "fin_recurring_amount_schedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_recurring_expenses_Active_DayOfMonth",
                table: "fin_recurring_expenses",
                columns: new[] { "Active", "DayOfMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_recurring_expenses_CategoryId",
                table: "fin_recurring_expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_recurring_expenses_CreditCardId",
                table: "fin_recurring_expenses",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_recurring_expenses_UserId",
                table: "fin_recurring_expenses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fin_accounts");

            migrationBuilder.DropTable(
                name: "fin_debts");

            migrationBuilder.DropTable(
                name: "fin_incomes");

            migrationBuilder.DropTable(
                name: "fin_installments");

            migrationBuilder.DropTable(
                name: "fin_patrimony_snapshots");

            migrationBuilder.DropTable(
                name: "fin_recurring_amount_schedules");

            migrationBuilder.DropTable(
                name: "fin_expenses");

            migrationBuilder.DropTable(
                name: "fin_installment_plans");

            migrationBuilder.DropTable(
                name: "fin_recurring_expenses");

            migrationBuilder.DropTable(
                name: "fin_categories");

            migrationBuilder.DropTable(
                name: "fin_credit_cards");
        }
    }
}
