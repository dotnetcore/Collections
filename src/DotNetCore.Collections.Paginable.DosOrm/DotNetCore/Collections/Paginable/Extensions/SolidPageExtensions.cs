using Dos.ORM;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    [SolidPageExtensionsFor(
        ormName: "DosORM",
        sourceTypeFullName: "Dos.ORM.FromSection<T>",
        sourceParamName: "query",
        pageTypeName: "DosPage",
        paginableTypeName: "PaginableDosQuery",
        factoryTypeName: "PaginableDosCollFactory",
        helperTypeName: "DosHelper",
        GenericConstraint = "Entity",
        HasAdditionalQueryFunc = true,
        ToPaginableHasAdditionalQueryFunc = true,
        SourceDescriptor = "DosQueryable")]
    public static partial class SolidPageExtensions
    {
    }
}
