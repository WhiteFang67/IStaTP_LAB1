using OnlineStoreDomain.Model;

namespace OnlineStoreInfrastructure.Services
{
    public interface IExportService<TEntity>
        where TEntity : Entity
    {
        Task WriteToAsync(Stream stream, CancellationToken cancellationToken);
    }
}
