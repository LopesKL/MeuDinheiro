using AutoMapper;
using Project.Entities;
using Application.Dto.Dtos;
using Application.Dto.Users;
using Application.Dto.Finance;
using Project.Entities.Finance;

namespace Project.Mappings;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Crud, CrudDto>().ReverseMap();
        CreateMap<AppUser, UserDto>();

        CreateMap<Category, CategoryDto>();
        CreateMap<Income, IncomeDto>();
        CreateMap<CreditCard, CreditCardDto>();
        CreateMap<Installment, InstallmentDto>();
        CreateMap<InstallmentPlan, InstallmentPlanDto>()
            .ForMember(d => d.Installments, o => o.MapFrom(s => s.Installments.OrderBy(i => i.SequenceNumber)));
        CreateMap<Debt, DebtDto>()
            .ForMember(d => d.Balance, o => o.MapFrom(s => s.TotalAmount - s.PaidAmount));
        CreateMap<RecurringExpense, RecurringExpenseDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => (int)s.Type))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => (int)s.PaymentMethod));
        CreateMap<Account, AccountDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => (int)s.Type));
        CreateMap<Expense, ExpenseDto>()
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => (int)s.PaymentMethod))
            .ForMember(d => d.CreationSource, o => o.MapFrom(s => (int)s.CreationSource))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
    }
}
