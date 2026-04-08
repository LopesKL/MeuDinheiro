using Application.Dto.Finance;
using Notifications.Notifications;
using Project.Entities.Finance;
using Repositories.Interfaces;

namespace Application.Finance;

public class CategoryService
{
    private readonly IFinanceStore _finance;
    private readonly INotificationHandler _notification;

    public CategoryService(IFinanceStore finance, INotificationHandler notification)
    {
        _finance = finance;
        _notification = notification;
    }

    public async Task<List<CategoryDto>> GetAllAsync(string userId)
    {
        var list = await _finance.ListCategoriesAsync(userId);
        return list.Select(Map).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(string userId, Guid id)
    {
        var c = await _finance.GetCategoryAsync(userId, id);
        return c == null ? null : Map(c);
    }

    public async Task<CategoryDto?> CreateAsync(string userId, CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _notification.DefaultBuilder("Cat_01", "Nome da categoria é obrigatório");
            return null;
        }

        var entity = new Category
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            IsExpense = dto.IsExpense
        };
        await _finance.InsertCategoryAsync(entity);
        return Map(entity);
    }

    public async Task<CategoryDto?> UpdateAsync(string userId, CategoryDto dto)
    {
        var entity = await _finance.GetCategoryAsync(userId, dto.Id);
        if (entity == null)
        {
            _notification.DefaultBuilder("Cat_02", "Categoria não encontrada");
            return null;
        }

        entity.Name = string.IsNullOrWhiteSpace(dto.Name) ? entity.Name : dto.Name.Trim();
        entity.IsExpense = dto.IsExpense;
        await _finance.UpdateCategoryAsync(entity);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var entity = await _finance.GetCategoryAsync(userId, id);
        if (entity == null)
        {
            _notification.DefaultBuilder("Cat_03", "Categoria não encontrada");
            return false;
        }

        if (await _finance.CategoryInUseAsync(userId, id))
        {
            _notification.DefaultBuilder("Cat_04", "Categoria em uso; remova lançamentos antes.");
            return false;
        }

        await _finance.DeleteCategoryAsync(userId, id);
        return true;
    }

    private static CategoryDto Map(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        IsExpense = c.IsExpense
    };
}
