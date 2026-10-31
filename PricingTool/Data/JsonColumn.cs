using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PricingTool.Data;

// A typed value stored as a JSON string in an nvarchar(max) column, such as Site.Warnings. Compared by
// its JSON, so EF notices changes made inside the list or dictionary.
public static class JsonColumn
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static ValueConverter<T, string> Converter<T>() where T : new() => new(
        v => JsonSerializer.Serialize(v, Options),
        v => string.IsNullOrEmpty(v) ? new T() : JsonSerializer.Deserialize<T>(v, Options) ?? new T());

    public static ValueComparer<T> Comparer<T>() where T : new() => new(
        (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
        v => JsonSerializer.Serialize(v, Options).GetHashCode(),
        v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options) ?? new T());
}
