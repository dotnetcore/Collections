using Chloe;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    [SolidPageExtensionsFor(
        ormName: "Chloe",
        sourceTypeFullName: "Chloe.IQuery<T>",
        sourceParamName: "query",
        pageTypeName: "ChloePage",
        paginableTypeName: "PaginableChloeQuery",
        factoryTypeName: "PaginableChloeCollFactory",
        helperTypeName: "ChloeHelper",
        HasAdditionalQueryFunc = true,
        SourceDescriptor = "ChloeQueryable")]
    public static partial class SolidPageExtensions
    {
    }
}
