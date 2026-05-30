using InventoryMapper.Core.Entities;

namespace InventoryMapper.Core.Interfaces;

public interface IImportService
{
    Task<ImportBatch> ImportFromStreamAsync(Stream stream, string fileName, string? assetType, string importedBy, CancellationToken ct = default);
    Task<ImportBatch?> GetImportBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<IEnumerable<ImportBatch>> GetImportHistoryAsync(int count = 20, CancellationToken ct = default);
    Task<byte[]> GenerateImportTemplateAsync(string assetType, CancellationToken ct = default);
}
