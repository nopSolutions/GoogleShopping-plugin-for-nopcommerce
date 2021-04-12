using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Services
{
    public partial interface IGoogleService
    {
        Task DeleteGoogleProductAsync(GoogleProductRecord googleProductRecord);

        Task<IList<GoogleProductRecord>> GetAllAsync();

        Task<GoogleProductRecord> GetByIdAsync(int googleProductRecordId);

        Task<GoogleProductRecord> GetByProductIdAsync(int productId);

        Task InsertGoogleProductRecordAsync(GoogleProductRecord googleProductRecord);

        Task UpdateGoogleProductRecordAsync(GoogleProductRecord googleProductRecord);

        Task<IList<string>> GetTaxonomyListAsync();
    }
}