using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace backend.Infrastructure;

/// <summary>
/// 在兼容版本的 Swagger 中标明 warehouseId 已弃用。
/// </summary>
public sealed class DeprecatedWarehouseOperationFilter : IOperationFilter
{
    private const string Description =
        "单仓库兼容参数，已弃用；新客户端不应传入。传入时必须等于系统唯一启用仓库编号。";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters.Where(x =>
                     string.Equals(x.Name, "warehouseId", StringComparison.OrdinalIgnoreCase)))
        {
            parameter.Deprecated = true;
            parameter.Description = Description;
        }

        foreach (var content in operation.RequestBody?.Content.Values ?? [])
        {
            MarkSchema(content.Schema, context.SchemaRepository);
        }
    }

    private static void MarkSchema(OpenApiSchema? schema, SchemaRepository repository)
    {
        if (schema is null) return;

        if (schema.Reference?.Id is { } id && repository.Schemas.TryGetValue(id, out var referenced))
            schema = referenced;

        foreach (var property in schema.Properties.Where(x =>
                     string.Equals(x.Key, "warehouseId", StringComparison.OrdinalIgnoreCase)))
        {
            property.Value.Deprecated = true;
            property.Value.Description = Description;
        }
    }
}
