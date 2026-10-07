using System.ComponentModel.DataAnnotations;

namespace CodeExample.Shared.Web;

/// <summary>Имя сервиса в ответах и журналах. Читается там, где контейнера ещё нет.</summary>
public sealed class ServiceOptions
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Описание документа OpenAPI.</summary>
public sealed class SwaggerOptions
{
    [Required(ErrorMessage = "SwaggerOptions:Title is required.")]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Version { get; set; } = "v1";

    public SwaggerSecurityScheme Scheme { get; set; } = SwaggerSecurityScheme.Bearer;
}

/// <summary>Какую схему безопасности ожидают эндпоинты сервиса.</summary>
public enum SwaggerSecurityScheme
{
    None = 0,

    Bearer = 1,

    ApiKey = 2
}
