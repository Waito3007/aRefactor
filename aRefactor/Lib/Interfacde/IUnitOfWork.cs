namespace aRefactor.Lib.Interfacde;

public interface IUnitOfWork
{
    Task BeginTransaction();
    Task CommitTransaction();
    Task RollbackTransaction();
    Task SaveChangesAsync();
}