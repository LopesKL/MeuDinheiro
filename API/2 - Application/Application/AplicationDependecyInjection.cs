using Application.Crud;
using Application.Finance;
using Application.Users;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Mappings;

namespace Application;

public static class AplicationDependecyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ApplicationMappingProfile>();
        }, typeof(ApplicationMappingProfile).Assembly);

        // Handlers
        services.AddScoped<CrudHandler>();
        services.AddScoped<UserHandler>();

        // Finance
        services.AddScoped<PatrimonySnapshotService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<IncomeService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<CreditCardService>();
        services.AddScoped<InstallmentPlanService>();
        services.AddScoped<DebtService>();
        services.AddScoped<RecurringExpenseService>();
        services.AddScoped<AccountService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ProjectionService>();
        services.AddScoped<OcrService>();

        return services;
    }
}
