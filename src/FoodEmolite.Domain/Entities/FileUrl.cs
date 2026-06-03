using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("files", Schema = "food_emolite")]
public class FileUrl : BaseEntity
{
    [Column("original_name")]
    public string OriginalName { get; set; }

    [Column("stored_name")]
    public string StoredName { get; set; }

    [Column("file_extension")]
    public string? FileExtension { get; set; }

    [Column("content_type")]
    public string? ContentType { get; set; }

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("storage_provider")]
    public string? StorageProvider { get; set; }

    [Column("storage_bucket")]
    public string? StorageBucket { get; set; }

    [Column("file_path")]
    public string FilePath { get; set; }

    [Column("full_url")]
    public string FullUrl { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = true;
}