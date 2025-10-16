using System.ComponentModel.DataAnnotations;

namespace DockBar.DockItem.Structs;

public enum MetadataTypeEnum
{
    String,
    Int,
    Double,
    Bool,
    // TODO Enum 类型必须要有一个 AllowedValuesAttribute
    Enum
}


public sealed record MetadataDescription
{
    public required string Key { get; init; }
    public required MetadataTypeEnum Type { get; init; }
    public required IEnumerable<ValidationAttribute> Validations { get; init; }
}
