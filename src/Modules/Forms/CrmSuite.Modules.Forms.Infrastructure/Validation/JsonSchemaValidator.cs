using System.Text.Json;
using System.Text.RegularExpressions;
using CrmSuite.Modules.Forms.Application.Abstractions;

namespace CrmSuite.Modules.Forms.Infrastructure.Validation;

public sealed class JsonSchemaValidator : IFormSchemaValidator
{
    public IReadOnlyList<string> Validate(string schemaJson, string submittedDataJson)
    {
        var errors = new List<string>();

        try
        {
            using var schemaDocument = JsonDocument.Parse(schemaJson);
            using var dataDocument = JsonDocument.Parse(submittedDataJson);

            var schema = schemaDocument.RootElement;
            var data = dataDocument.RootElement;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return ["Schema فرم باید JSON Object باشد."];
            }

            if (data.ValueKind != JsonValueKind.Object)
            {
                return ["داده‌های فرم باید JSON Object باشند."];
            }

            if (schema.TryGetProperty("fields", out var fields)
                && fields.ValueKind == JsonValueKind.Array)
            {
                ValidateFields(fields, data, errors);
            }
            else
            {
                ValidateLegacyObject(schema, data, string.Empty, errors);
            }
        }
        catch (JsonException)
        {
            errors.Add("JSON فرم یا داده‌های ارسالی معتبر نیست.");
        }

        return errors;
    }

    private static void ValidateFields(
        JsonElement fields,
        JsonElement data,
        ICollection<string> errors)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var field in fields.EnumerateArray())
        {
            index++;
            if (field.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"تعریف فیلد شماره {index} معتبر نیست.");
                continue;
            }

            var key = ReadString(field, "key");
            var label = ReadString(field, "label") ?? key ?? $"فیلد {index}";
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"کلید فیلد شماره {index} الزامی است.");
                continue;
            }

            if (!keys.Add(key))
            {
                errors.Add($"کلید فیلد «{key}» تکراری است.");
                continue;
            }

            if (!IsVisible(field, data))
            {
                continue;
            }

            var hasValue = data.TryGetProperty(key, out var value)
                           && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                           && !(value.ValueKind == JsonValueKind.String
                                && string.IsNullOrWhiteSpace(value.GetString()));

            var required = field.TryGetProperty("required", out var requiredElement)
                           && requiredElement.ValueKind == JsonValueKind.True;

            if (!hasValue)
            {
                if (required)
                {
                    errors.Add($"فیلد «{label}» الزامی است.");
                }

                continue;
            }

            ValidateFieldValue(field, key, label, value, errors);
        }
    }

    private static void ValidateFieldValue(
        JsonElement field,
        string key,
        string label,
        JsonElement value,
        ICollection<string> errors)
    {
        var type = ReadString(field, "type")?.Trim().ToLowerInvariant() ?? "text";

        var typeValid = type switch
        {
            "text" or "textarea" or "select" or "radio" or "date" or "email" or "phone" or "tel"
                or "file" or "string" => value.ValueKind == JsonValueKind.String,
            "number" or "integer" => value.ValueKind == JsonValueKind.Number,
            "multiselect" or "multi-select" => value.ValueKind == JsonValueKind.Array,
            "checkbox" or "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False
                                        || value.ValueKind == JsonValueKind.Array,
            _ => true,
        };

        if (!typeValid)
        {
            errors.Add($"نوع فیلد «{label}» معتبر نیست.");
            return;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (!value.TryGetDecimal(out var number))
            {
                errors.Add($"مقدار فیلد «{label}» عدد معتبری نیست.");
                return;
            }

            if (type == "integer" && number != decimal.Truncate(number))
            {
                errors.Add($"مقدار فیلد «{label}» باید عدد صحیح باشد.");
            }

            var minimum = ReadDecimal(field, "min") ?? ReadDecimal(field, "minimum");
            var maximum = ReadDecimal(field, "max") ?? ReadDecimal(field, "maximum");
            if (minimum.HasValue && number < minimum.Value)
            {
                errors.Add($"مقدار فیلد «{label}» کمتر از حد مجاز است.");
            }

            if (maximum.HasValue && number > maximum.Value)
            {
                errors.Add($"مقدار فیلد «{label}» بیشتر از حد مجاز است.");
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString() ?? string.Empty;
            var minLength = ReadInt(field, "minLength") ?? ReadInt(field, "min");
            var maxLength = ReadInt(field, "maxLength") ?? ReadInt(field, "max");
            if (minLength.HasValue && text.Length < minLength.Value)
            {
                errors.Add($"طول فیلد «{label}» کمتر از حد مجاز است.");
            }

            if (maxLength.HasValue && text.Length > maxLength.Value)
            {
                errors.Add($"طول فیلد «{label}» بیشتر از حد مجاز است.");
            }

            var pattern = ReadString(field, "pattern") ?? ReadString(field, "regex");
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                try
                {
                    if (!Regex.IsMatch(text, pattern))
                    {
                        errors.Add($"مقدار فیلد «{label}» با الگوی مجاز مطابقت ندارد.");
                    }
                }
                catch (ArgumentException)
                {
                    errors.Add($"الگوی اعتبارسنجی فیلد «{label}» معتبر نیست.");
                }
            }

            if (type == "email"
                && !Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errors.Add($"ایمیل فیلد «{label}» معتبر نیست.");
            }

            if ((type is "phone" or "tel")
                && !Regex.IsMatch(text, @"^[0-9+\-\s()]{7,20}$"))
            {
                errors.Add($"شماره تماس فیلد «{label}» معتبر نیست.");
            }

            ValidateOptions(field, key, label, value, errors);
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                ValidateOptions(field, key, label, item, errors);
            }
        }
    }

    private static void ValidateOptions(
        JsonElement field,
        string key,
        string label,
        JsonElement value,
        ICollection<string> errors)
    {
        if (!field.TryGetProperty("options", out var options)
            || options.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var allowed = options
            .EnumerateArray()
            .Select(option => option.ValueKind == JsonValueKind.Object
                ? ReadString(option, "value")
                : option.ValueKind == JsonValueKind.String
                    ? option.GetString()
                    : null)
            .Where(value => value is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowed.Count == 0)
        {
            return;
        }

        var selected = value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
        if (!string.IsNullOrEmpty(selected) && !allowed.Contains(selected))
        {
            errors.Add($"مقدار فیلد «{label}» (کلید {key}) در گزینه‌های مجاز نیست.");
        }
    }

    private static bool IsVisible(JsonElement field, JsonElement data)
    {
        if (!field.TryGetProperty("visibleIf", out var visibleIf)
            || visibleIf.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        var dependentField = ReadString(visibleIf, "field");
        if (string.IsNullOrWhiteSpace(dependentField)
            || !data.TryGetProperty(dependentField, out var dependentValue))
        {
            return false;
        }

        if (!visibleIf.TryGetProperty("equals", out var expected))
        {
            return true;
        }

        return CompareJsonValue(dependentValue, expected);
    }

    private static bool CompareJsonValue(JsonElement actual, JsonElement expected)
    {
        if (actual.ValueKind == expected.ValueKind)
        {
            return actual.ValueKind switch
            {
                JsonValueKind.String => string.Equals(
                    actual.GetString(),
                    expected.GetString(),
                    StringComparison.OrdinalIgnoreCase),
                JsonValueKind.Number => actual.TryGetDecimal(out var left)
                                        && expected.TryGetDecimal(out var right)
                                        && left == right,
                JsonValueKind.True or JsonValueKind.False => actual.GetBoolean() == expected.GetBoolean(),
                _ => actual.GetRawText() == expected.GetRawText(),
            };
        }

        var actualText = actual.ValueKind == JsonValueKind.String
            ? actual.GetString()
            : actual.ToString();
        var expectedText = expected.ValueKind == JsonValueKind.String
            ? expected.GetString()
            : expected.ToString();
        return string.Equals(actualText, expectedText, StringComparison.OrdinalIgnoreCase);
    }

    // Legacy JSON Schema support: keeps existing Forms consumers and tests working.
    private static void ValidateLegacyObject(
        JsonElement schema,
        JsonElement data,
        string path,
        ICollection<string> errors)
    {
        if (!schema.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (schema.TryGetProperty("required", out var required)
            && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var requiredProperty in required.EnumerateArray())
            {
                var name = requiredProperty.GetString();
                if (!string.IsNullOrWhiteSpace(name)
                    && (!data.TryGetProperty(name, out var value)
                        || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined))
                {
                    errors.Add($"فیلد «{Label(path, name)}» الزامی است.");
                }
            }
        }

        foreach (var property in properties.EnumerateObject())
        {
            if (!data.TryGetProperty(property.Name, out var value))
            {
                continue;
            }

            ValidateLegacyValue(property.Value, value, Label(path, property.Name), errors);
        }

        if (schema.TryGetProperty("additionalProperties", out var additional)
            && additional.ValueKind == JsonValueKind.False)
        {
            foreach (var property in data.EnumerateObject())
            {
                if (!properties.TryGetProperty(property.Name, out _))
                {
                    errors.Add($"فیلد «{Label(path, property.Name)}» در Schema تعریف نشده است.");
                }
            }
        }
    }

    private static void ValidateLegacyValue(
        JsonElement propertySchema,
        JsonElement value,
        string path,
        ICollection<string> errors)
    {
        if (propertySchema.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            if (!MatchesType(type, value))
            {
                errors.Add($"نوع فیلد «{path}» باید {type} باشد.");
                return;
            }
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString() ?? string.Empty;

            if (propertySchema.TryGetProperty("minLength", out var minLength)
                && text.Length < minLength.GetInt32())
            {
                errors.Add($"طول فیلد «{path}» کمتر از حد مجاز است.");
            }

            if (propertySchema.TryGetProperty("maxLength", out var maxLength)
                && text.Length > maxLength.GetInt32())
            {
                errors.Add($"طول فیلد «{path}» بیشتر از حد مجاز است.");
            }

            if (propertySchema.TryGetProperty("pattern", out var pattern)
                && pattern.GetString() is { } patternText
                && !Regex.IsMatch(text, patternText))
            {
                errors.Add($"مقدار فیلد «{path}» با الگوی مجاز مطابقت ندارد.");
            }
        }

        if (value.ValueKind == JsonValueKind.Number
            && value.TryGetDecimal(out var number))
        {
            if (propertySchema.TryGetProperty("minimum", out var minimum)
                && number < minimum.GetDecimal())
            {
                errors.Add($"مقدار فیلد «{path}» کمتر از حد مجاز است.");
            }

            if (propertySchema.TryGetProperty("maximum", out var maximum)
                && number > maximum.GetDecimal())
            {
                errors.Add($"مقدار فیلد «{path}» بیشتر از حد مجاز است.");
            }
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            ValidateLegacyObject(propertySchema, value, path, errors);
        }
    }

    private static bool MatchesType(string? type, JsonElement value) =>
        type switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "integer" => value.ValueKind == JsonValueKind.Number
                         && value.TryGetDecimal(out var integer)
                         && integer == decimal.Truncate(integer),
            "number" => value.ValueKind == JsonValueKind.Number,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => value.ValueKind == JsonValueKind.Null,
            _ => true,
        };

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var number)
            ? number
            : null;

    private static decimal? ReadDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetDecimal(out var number)
            ? number
            : null;

    private static string Label(string path, string name) =>
        string.IsNullOrEmpty(path) ? name : $"{path}.{name}";
}
