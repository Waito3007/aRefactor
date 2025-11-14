using System;
using aRefactor.Domain.Model;
using aRefactor.Lib;
using aRefactor.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace aRefactor.Repository;

public class PatternRepository : IPatternRepository
{
    private readonly AppDbContext _context;

    public PatternRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(Pattern request)
    {
        _context.Patterns.Add(request);
    }

    public void Update(Pattern request)
    {
        _context.Patterns.Update(request);
    }

    public void Delete(Pattern request)
    {
        _context.Patterns.Remove(request);
    }

    public async Task<Pattern?> GetBySlug(string slug)
    {
        return await _context.Patterns
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<Pattern?> GetByIdAsync(Guid id)
    {
        return await _context.Patterns.FirstOrDefaultAsync(p => p.Id == id);
    }
}
