using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Services
{
    public partial class GoogleService : IGoogleService
    {
        #region Fields

        private readonly IRepository<GoogleProductRecord> _gpRepository;

        #endregion

        #region Ctor

        public GoogleService(IRepository<GoogleProductRecord> gpRepository)
        {
            _gpRepository = gpRepository;
        }

        #endregion

        #region Utilities

        private async Task<string> GetEmbeddedFileContentAsync(string resourceName)
        {
            var fullResourceName = $"Nop.Plugin.Feed.GoogleShopping.Files.{resourceName}";
            var assem = GetType().Assembly;
            using var stream = assem.GetManifestResourceStream(fullResourceName);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        #endregion

        #region Methods

        public virtual async Task DeleteGoogleProductAsync(GoogleProductRecord googleProductRecord)
        {
            if (googleProductRecord == null)
                throw new ArgumentNullException(nameof(googleProductRecord));

            await _gpRepository.DeleteAsync(googleProductRecord);
        }

        public virtual async Task<IList<GoogleProductRecord>> GetAllAsync()
        {
            var query = from gp in _gpRepository.Table
                        orderby gp.Id
                        select gp;
            var records = await query.ToListAsync();
            return records;
        }

        public virtual async Task<GoogleProductRecord> GetByIdAsync(int googleProductRecordId)
        {
            if (googleProductRecordId == 0)
                return null;

            return await _gpRepository.GetByIdAsync(googleProductRecordId);
        }

        public virtual async Task<GoogleProductRecord> GetByProductIdAsync(int productId)
        {
            if (productId == 0)
                return null;

            var query = from gp in _gpRepository.Table
                        where gp.ProductId == productId
                        orderby gp.Id
                        select gp;
            var record = await query.FirstOrDefaultAsync();
            return record;
        }

        public virtual async Task InsertGoogleProductRecordAsync(GoogleProductRecord googleProductRecord)
        {
            if (googleProductRecord == null)
                throw new ArgumentNullException(nameof(googleProductRecord));

            await _gpRepository.InsertAsync(googleProductRecord);
        }

        public virtual async Task UpdateGoogleProductRecordAsync(GoogleProductRecord googleProductRecord)
        {
            if (googleProductRecord == null)
                throw new ArgumentNullException(nameof(googleProductRecord));

            await _gpRepository.UpdateAsync(googleProductRecord);
        }

        public virtual async Task<IList<string>> GetTaxonomyListAsync()
        {
            var fileContent = await GetEmbeddedFileContentAsync("taxonomy.txt");
            if (string.IsNullOrEmpty(fileContent))
                return new List<string>();

            //parse the file
            var result = fileContent.Split(new [] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries).ToList();
            return result;
        }

        #endregion
    }
}