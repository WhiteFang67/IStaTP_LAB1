using OnlineStoreDomain.Model;
using System;

namespace OnlineStoreInfrastructure.Services
{
    public class DataPortServiceFactory : IDataPortServiceFactory<Product>
    {
        private readonly IServiceProvider _serviceProvider;

        public DataPortServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IImportService<Product> GetImportService(string contentType)
        {
            if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return _serviceProvider.GetService<IImportService<Product>>();
            }
            throw new NotSupportedException($"Content type {contentType} is not supported for import.");
        }

        public IExportService<Product> GetExportService(string contentType)
        {
            if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return _serviceProvider.GetService<IExportService<Product>>();
            }
            throw new NotSupportedException($"Content type {contentType} is not supported for export.");
        }
    }
}