using ClosedXML.Excel;
using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Enums;
using InventoryMapper.Core.Interfaces;
using InventoryMapper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryMapper.Infrastructure.Services;

public class ImportService(ApplicationDbContext db, ILogger<ImportService> logger) : IImportService
{
    public async Task<ImportBatch> ImportFromStreamAsync(Stream stream, string fileName, string? assetTypeStr, string importedBy, CancellationToken ct = default)
    {
        var batch = new ImportBatch
        {
            FileName = fileName,
            OriginalFileName = fileName,
            Status = ImportStatus.Processing,
            CreatedBy = importedBy,
            StartedAt = DateTime.UtcNow
        };
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        var isXlsx = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
        var rows = isXlsx ? ParseXlsx(stream) : ParseCsv(stream);

        batch.TotalRows = rows.Count;

        foreach (var (row, index) in rows.Select((r, i) => (r, i + 2)))
        {
            var record = new ImportRecord { ImportBatchId = batch.Id, RowNumber = index, RawData = string.Join("|", row.Values) };

            try
            {
                var hostname = GetField(row, "Hostname", "Computer Name", "Name");
                if (string.IsNullOrWhiteSpace(hostname))
                {
                    record.Success = false;
                    record.ErrorMessage = "Hostname is required";
                    batch.ErrorRows++;
                    db.ImportRecords.Add(record);
                    continue;
                }

                // Duplicate check
                var existing = await db.Assets.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.Hostname == hostname, ct);

                if (existing != null)
                {
                    record.IsDuplicate = true;
                    record.AssetId = existing.Id;
                    // Update existing
                    UpdateFromRow(existing, row, assetTypeStr);
                    batch.DuplicateRows++;
                }
                else
                {
                    var asset = CreateFromRow(row, assetTypeStr);
                    db.Assets.Add(asset);
                    await db.SaveChangesAsync(ct);
                    record.AssetId = asset.Id;
                    batch.SuccessRows++;
                }

                record.Success = true;
            }
            catch (Exception ex)
            {
                record.Success = false;
                record.ErrorMessage = ex.Message;
                batch.ErrorRows++;
                logger.LogWarning("Import row {Row} failed: {Msg}", index, ex.Message);
            }

            db.ImportRecords.Add(record);
            batch.ProcessedRows++;
        }

        batch.Status = batch.ErrorRows == 0 ? ImportStatus.Completed : ImportStatus.CompletedWithErrors;
        batch.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return batch;
    }

    private static List<Dictionary<string, string>> ParseXlsx(Stream stream)
    {
        var rows = new List<Dictionary<string, string>>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var headers = ws.Row(1).Cells().Select((c, i) => (Value: c.GetString().Trim(), Index: i + 1)).ToList();

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in headers)
                dict[h.Value] = row.Cell(h.Index).GetString().Trim();
            if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                rows.Add(dict);
        }
        return rows;
    }

    private static List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        var rows = new List<Dictionary<string, string>>();
        using var reader = new StreamReader(stream);
        var headerLine = reader.ReadLine();
        if (headerLine == null) return rows;
        var headers = headerLine.Split(',').Select(h => h.Trim('"', ' ')).ToArray();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var values = line.Split(',').Select(v => v.Trim('"', ' ')).ToArray();
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length && i < values.Length; i++)
                dict[headers[i]] = values[i];
            rows.Add(dict);
        }
        return rows;
    }

    private static Asset CreateFromRow(Dictionary<string, string> row, string? assetTypeStr)
    {
        Enum.TryParse<AssetType>(assetTypeStr ?? GetField(row, "AssetType", "Type", "Device Type"), true, out var assetType);
        return new Asset
        {
            Hostname = GetField(row, "Hostname", "Computer Name", "Name") ?? "Unknown",
            IpAddress = GetField(row, "IP Address", "IPAddress", "IP"),
            MacAddress = GetField(row, "MAC Address", "MACAddress", "MAC"),
            SerialNumber = GetField(row, "Serial Number", "SerialNumber", "Serial"),
            AssetType = assetType == default ? AssetType.PhysicalDevice : assetType,
            Manufacturer = GetField(row, "Manufacturer", "Make", "Vendor"),
            Model = GetField(row, "Model"),
            OperatingSystem = GetField(row, "Operating System", "OS"),
            OsVersion = GetField(row, "OS Version", "OsVersion"),
            OrganizationalUnit = GetField(row, "OU", "Organizational Unit", "OrganizationalUnit"),
            AssignedUser = GetField(row, "Assigned User", "AssignedUser", "User"),
            Department = GetField(row, "Department", "Dept"),
            Notes = GetField(row, "Notes", "Comments"),
            Status = AssetStatus.Active
        };
    }

    private static void UpdateFromRow(Asset asset, Dictionary<string, string> row, string? assetTypeStr)
    {
        asset.IpAddress = GetField(row, "IP Address", "IPAddress", "IP") ?? asset.IpAddress;
        asset.MacAddress = GetField(row, "MAC Address", "MACAddress", "MAC") ?? asset.MacAddress;
        asset.Manufacturer = GetField(row, "Manufacturer", "Make", "Vendor") ?? asset.Manufacturer;
        asset.Model = GetField(row, "Model") ?? asset.Model;
        asset.OperatingSystem = GetField(row, "Operating System", "OS") ?? asset.OperatingSystem;
        asset.AssignedUser = GetField(row, "Assigned User", "AssignedUser", "User") ?? asset.AssignedUser;
        asset.Department = GetField(row, "Department", "Dept") ?? asset.Department;
        asset.UpdatedAt = DateTime.UtcNow;
    }

    private static string? GetField(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
            if (row.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;
        return null;
    }

    public async Task<ImportBatch?> GetImportBatchAsync(Guid batchId, CancellationToken ct = default)
        => await db.ImportBatches
            .Include(b => b.Records).ThenInclude(r => r.Asset)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

    public async Task<IEnumerable<ImportBatch>> GetImportHistoryAsync(int count = 20, CancellationToken ct = default)
        => await db.ImportBatches
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public Task<byte[]> GenerateImportTemplateAsync(string assetType, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Assets");

        var headers = new[] { "Hostname", "IP Address", "MAC Address", "Serial Number", "AssetType",
            "Manufacturer", "Model", "Operating System", "OS Version", "OU",
            "Assigned User", "Department", "Notes" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        ws.Row(2).Cell(1).Value = "EXAMPLE-PC-001";
        ws.Row(2).Cell(2).Value = "10.0.1.100";
        ws.Row(2).Cell(3).Value = "AA:BB:CC:DD:EE:FF";
        ws.Row(2).Cell(4).Value = "SN-EXAMPLE";
        ws.Row(2).Cell(5).Value = assetType;
        ws.Row(2).Cell(6).Value = "Dell";
        ws.Row(2).Cell(7).Value = "OptiPlex 7090";
        ws.Row(2).Cell(8).Value = "Windows 11 Pro";
        ws.Row(2).Cell(9).Value = "22H2";
        ws.Row(2).Cell(10).Value = "OU=Workstations,DC=corp,DC=local";
        ws.Row(2).Cell(11).Value = "jsmith";
        ws.Row(2).Cell(12).Value = "IT";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
