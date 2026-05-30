using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.Entities;

public class ImportBatch : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public AssetType? TargetAssetType { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public int DuplicateRows { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<ImportRecord> Records { get; set; } = [];
}

public class ImportRecord : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public int RowNumber { get; set; }
    public bool Success { get; set; }
    public bool IsDuplicate { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawData { get; set; }

    // Created or updated asset reference
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
}
