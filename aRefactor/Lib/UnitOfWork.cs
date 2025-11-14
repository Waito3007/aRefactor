using aRefactor.Domain.Exception;
using aRefactor.Extension;
using aRefactor.Lib.Interfacde;
using Microsoft.EntityFrameworkCore.Storage;

namespace aRefactor.Lib;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _contextTransaction;
    private bool _disposed = false;
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //clear nếu k null
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    public async Task BeginTransaction()
    {
        if (_contextTransaction != null)
        {
            throw new ProjectException(Response.TransactionNotStarted.GetDescriptionOfEnum());
        }

        _contextTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransaction()
    {
        if (_contextTransaction == null)
        {
            throw new ProjectException(Response.TransactionNotStarted.GetDescriptionOfEnum());
        }

        try
        {
            await _contextTransaction.CommitAsync();
        }
        finally
        {
            await _contextTransaction.DisposeAsync();
            _contextTransaction = null;
        }
    }

    public async Task RollbackTransaction()
    {
        if (_contextTransaction == null)
        {
            throw new ProjectException(Response.TransactionNotStarted.GetDescriptionOfEnum());
        }

        try
        {
            await _contextTransaction.RollbackAsync();
        }
        finally
        {
            await _contextTransaction.DisposeAsync();
            _contextTransaction = null;
        }
       
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
