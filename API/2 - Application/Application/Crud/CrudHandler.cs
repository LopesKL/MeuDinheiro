using Application.Dto.Dtos;
using Application.Dto.RequestPatterns;
using Application.Dto.ResponsePatterns;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Notifications.Notifications;
using Project.Entities;
using Repositories.Interfaces;

namespace Application.Crud;

public class CrudHandler
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationHandler _notification;

    public CrudHandler(IUnitOfWork uow, IMapper mapper, INotificationHandler notification)
    {
        _uow = uow;
        _mapper = mapper;
        _notification = notification;
    }

    public async Task<CrudDto?> GetByIdAsync(Guid id)
    {
        var entity = await _uow.CrudRepository.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            _notification.DefaultBuilder("GetById_01", "Registro não encontrado");
            return null;
        }

        return _mapper.Map<CrudDto>(entity);
    }

    public async Task<ResponseAllDto<CrudDto>> GetAllAsync(RequestAllDto request, string currentUserId = "")
    {
        var query = _uow.CrudRepository.Find(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search) || 
                                    (x.Description != null && x.Description.Contains(request.Search)));
        }

        var totalCount = await query.CountAsync();

        query = query.ApplyOrdering(request);
        query = query.ApplyPaging(request);

        var entities = await query.ToListAsync();
        var dtos = _mapper.Map<List<CrudDto>>(entities);

        return new ResponseAllDto<CrudDto>
        {
            Success = true,
            Data = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            Message = "Success"
        };
    }

    public async Task<CrudDto?> UpsertAsync(CrudDto dto, string currentUserId = "")
    {
        Project.Entities.Crud entity;

        if (dto.Id == Guid.Empty)
        {
            entity = new Project.Entities.Crud();
            entity.Created = DateTimeOffset.UtcNow;
            entity.CreatedBy = currentUserId;
        }
        else
        {
            entity = await _uow.CrudRepository.FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
            if (entity == null)
            {
                _notification.DefaultBuilder("Upsert_01", "Registro não encontrado");
                return null;
            }
            entity.Updated = DateTimeOffset.UtcNow;
            entity.UpdatedBy = currentUserId;
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;

        if (dto.Id == Guid.Empty)
        {
            _uow.CrudRepository.Insert(entity);
        }
        else
        {
            _uow.CrudRepository.Update(entity);
        }

        await _uow.Save();

        return _mapper.Map<CrudDto>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _uow.CrudRepository.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            _notification.DefaultBuilder("Delete_01", "Registro não encontrado");
            return false;
        }

        entity.IsDeleted = true;
        _uow.CrudRepository.Update(entity);
        await _uow.Save();

        return true;
    }
}
