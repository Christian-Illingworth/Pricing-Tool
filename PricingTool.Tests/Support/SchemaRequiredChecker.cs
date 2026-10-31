using System.Text.Json;

namespace PricingTool.Tests.Support;

// A small check of a JSON document against a JSON Schema's "required", "enum" and "const" rules,
// following "properties", "items", "allOf", "oneOf" and local "$ref"s. Enough to prove a reporting
// record has every required field without adding a schema package (plan.md "Packages").
public static class SchemaRequiredChecker
{
    public static IReadOnlyList<string> Problems(JsonElement schemaRoot, JsonElement document)
    {
        var problems = new List<string>();
        Check(schemaRoot, schemaRoot, document, "$", problems);
        return problems;
    }

    public static JsonDocument LoadSchema(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "specs"))) dir = dir.Parent;
        if (dir is null) throw new DirectoryNotFoundException("Couldn't find the repository's specs folder.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir.FullName, "specs", "001-pricing-tool", "contracts", fileName)));
    }

    private static void Check(JsonElement root, JsonElement schema, JsonElement value, string path, List<string> problems)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            Check(root, Resolve(root, reference.GetString()!), value, path, problems);
            return;
        }
        if (value.ValueKind == JsonValueKind.Null) return;

        if (schema.TryGetProperty("const", out var constant) && constant.ToString() != value.ToString())
            problems.Add($"{path} should be {constant}");

        if (schema.TryGetProperty("enum", out var allowed)
            && !allowed.EnumerateArray().Any(a => a.ValueKind == value.ValueKind && a.ToString() == value.ToString()))
            problems.Add($"{path} = {value} isn't one of {allowed}");

        if (schema.TryGetProperty("allOf", out var allOf))
            foreach (var part in allOf.EnumerateArray()) Check(root, part, value, path, problems);

        if (schema.TryGetProperty("oneOf", out var oneOf))
        {
            var objectSchema = oneOf.EnumerateArray().FirstOrDefault(s => s.TryGetProperty("$ref", out _));
            if (objectSchema.ValueKind != JsonValueKind.Undefined) Check(root, objectSchema, value, path, problems);
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out var required))
                foreach (var name in required.EnumerateArray().Select(r => r.GetString()!))
                    if (!value.TryGetProperty(name, out _)) problems.Add($"{path}.{name} is missing");

            if (schema.TryGetProperty("properties", out var properties))
                foreach (var property in properties.EnumerateObject())
                    if (value.TryGetProperty(property.Name, out var child))
                        Check(root, property.Value, child, $"{path}.{property.Name}", problems);
        }

        if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            var i = 0;
            foreach (var item in value.EnumerateArray()) Check(root, items, item, $"{path}[{i++}]", problems);
        }
    }

    private static JsonElement Resolve(JsonElement root, string reference)
    {
        var current = root;
        foreach (var part in reference.TrimStart('#', '/').Split('/')) current = current.GetProperty(part);
        return current;
    }
}
