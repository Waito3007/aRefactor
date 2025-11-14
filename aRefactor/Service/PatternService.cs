using System;
using System.Net;
using AutoMapper;
using aRefactor.Domain.Dto;
using aRefactor.Domain.Exception;
using aRefactor.Domain.Model;
using aRefactor.Extension;
using aRefactor.Lib.Interfacde;
using aRefactor.Repository.Interface;
using aRefactor.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace aRefactor.Service;

public class PatternService : IPatternService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPatternRepository _patternRepository;
    private readonly IMapper _mapper;

    public PatternService(
        IUnitOfWork unitOfWork,
        IPatternRepository patternRepository,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _patternRepository = patternRepository;
        _mapper = mapper;
    }

    public async Task<CreateResponsePattern> CreatePatternAsync(CreateRequestPattern request)
    {
        if (request == null)
        {
            throw new ProjectException(Response.RequestCannotBeNull.GetDescriptionOfEnum());
        }

        request.Validate();

        var pattern = _mapper.Map<Pattern>(request);
        pattern.Id = Guid.NewGuid();

        try
        {
            await _unitOfWork.BeginTransaction();
            _patternRepository.Add(pattern);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();
        }
        catch (ProjectException)
        {
            await _unitOfWork.RollbackTransaction();
            throw;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackTransaction();
            throw new ProjectException(
                "Khong the tao moi pattern vao luc nay.",
                ex,
                HttpStatusCode.InternalServerError,
                "PATTERN_CREATE_FAILED",
                Response.InternalServerError);
        }

        return _mapper.Map<CreateResponsePattern>(pattern);
    }

    public async Task<UpdateResponsePattern> UpdatePatternAsync(UpdateRequestPattern request)
    {
        if (request == null)
        {
            throw new ProjectException(Response.RequestCannotBeNull.GetDescriptionOfEnum());
        }

        request.Validate();

        var pattern = await _patternRepository.GetByIdAsync(request.Id);
        if (pattern == null)
        {
            throw new NotFoundException("Pattern", request.Id);
        }

        _mapper.Map(request, pattern);

        try
        {
            await _unitOfWork.BeginTransaction();
            _patternRepository.Update(pattern);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();
        }
        catch (ProjectException)
        {
            await _unitOfWork.RollbackTransaction();
            throw;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackTransaction();
            throw new ProjectException(
                "Khong the cap nhat pattern vao luc nay.",
                ex,
                HttpStatusCode.InternalServerError,
                "PATTERN_UPDATE_FAILED",
                Response.InternalServerError);
        }

        return _mapper.Map<UpdateResponsePattern>(pattern);
    }

    public async Task<GetResponsePattern> GetPatternAsync(GetRequestPattern request)
    {
        if (request == null)
        {
            throw new ProjectException(Response.RequestCannotBeNull.GetDescriptionOfEnum());
        }

        request.Validate();

        Pattern? pattern;
        try
        {
            pattern = await _patternRepository.GetBySlug(request.Slug);
        }
        catch (DbUpdateException ex)
        {
            throw new ProjectException(
                "Khong the lay pattern vao luc nay.",
                ex,
                HttpStatusCode.InternalServerError,
                "PATTERN_GET_FAILED",
                Response.InternalServerError);
        }

        if (pattern == null)
        {
            throw new NotFoundException("Pattern", request.Slug);
        }

        return _mapper.Map<GetResponsePattern>(pattern);
    }
}
