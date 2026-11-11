using System.Globalization;
using System.Text.Json;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;

namespace CrmSuite.Modules.Forms.Application.Schema;

public sealed record FormFieldDefinition(
    string Key,
    string Label,
    FormFieldType FieldType,
    bool IsRequired,
    string? DefaultValue,
    string? OptionsJson,
    string? MinValue,
    string? MaxValue,
    string? RegexPattern,
    int Order,
    string? VisibleIfJson)
{
    public FormField ToEntity(Guid definitionId) =>
        new(
            definitionId,
            Key,
            Label,
            FieldType,
            IsRequired,
            DefaultValue,
            OptionsJson,
            MinValue,
            MaxValue,
            RegexPattern,
            Order,
            VisibleIfJson);
}

public sealed record FormSchemaDefinition(IReadOnlyList<FormFieldDefinition> Fields);

public static class FormSchemaParser
{
    public static Result<FormSchemaDefinition> Parse(
        string? schemaJson,
        bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return Result.Failure<FormSchemaDefinition>(
                "Schema فرم الزامی است.",
                "FORMS_SCHEMA_REQUIRED");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException)
        {
            return Result.Failure<FormSchemaDefinition>(
                "Schema فرم JSON معتبری نیست.",
                "FORMS_SCHEMA_INVALID");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<FormSchemaDefinition>(
                    "Schema فرم باید یک JSON Object باشد.",
                    "FORMS_SCHEMA_INVALID");
            }

            var fields = new List<FormFieldDefinition>();
            if (root.TryGetProperty("fields", out var fieldsElement)
                && fieldsElement.ValueKind == JsonValueKind.Array)
            {
                var order = 1;
                foreach (var field in fieldsElement.EnumerateArray())
                {
                    var parsed = ParseField(field, order++);
                    if (parsed.IsFailure)
                    {
                        return Result.Failure<FormSchemaDefinition>(parsed.Errors, parsed.Message);
                    }

                    fields.Add(parsed.Value!);
                }
            }
            else if (root.TryGetProperty("properties", out var properties)
                     && properties.ValueKind == JsonValueKind.Object)
            {
                var requiredKeys = ReadRequiredKeys(root);
                var order = 1;
                foreach (var property in properties.EnumerateObject())
                {
                    var parsed = ParseLegacyField(
                        property.Name,
                        property.Value,
                        requiredKeys.Contains(property.Name),
                        order++);
                    if (parsed.IsFailure)
                    {
                        return Result.Failure<FormSchemaDefinition>(parsed.Errors, parsed.Message);
                    }

                    fields.Add(parsed.Value!);
                }
            }

            if (fields.Count == 0)
            {
                if (allowEmpty)
                {
                    return Result.Success(new FormSchemaDefinition([]));
                }

                return Result.Failure<FormSchemaDefinition>(
                    "فرم حداقل به یک فیلد نیاز دارد.",
                    "FORMS_FIELDS_REQUIRED");
            }

            var duplicate = fields
                .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                return Result.Failure<FormSchemaDefinition>(
                    $"کلید فیلد «{duplicate.Key}» تکراری است.",
                    "FORMS_FIELD_KEY_DUPLICATE");
            }

            return Result.Success(new FormSchemaDefinition(
                fields.OrderBy(field => field.Order).ToArray()));
        }
    }

    private static Result<FormFieldDefinition> ParseField(JsonElement field, int fallbackOrder)
    {
        if (field.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<FormFieldDefinition>(
                "هر فیلد Schema باید یک Object باشد.",
                "FORMS_FIELD_INVALID");
        }

        var key = ReadString(field, "key");
        var label = ReadString(field, "label");
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure<FormFieldDefinition>(
                "کلید همه فیلدهای Schema الزامی است.",
                "FORMS_FIELD_KEY_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Result.Failure<FormFieldDefinition>(
                $"عنوان فیلد «{key}» الزامی است.",
                "FORMS_FIELD_LABEL_REQUIRED");
        }

        var typeText = ReadString(field, "type") ?? "text";
        if (!TryMapType(typeText, out var fieldType))
        {
            return Result.Failure<FormFieldDefinition>(
                $"نوع فیلد «{key}» معتبر نیست.",
                "FORMS_FIELD_TYPE_INVALID");
        }

        var required = field.TryGetProperty("required", out var requiredElement)
                       && requiredElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                       && requiredElement.GetBoolean();
        var order = field.TryGetProperty("order", out var orderElement)
                    && orderElement.TryGetInt32(out var orderValue)
            ? orderValue
            : fallbackOrder;
        var optionsJson = field.TryGetProperty("options", out var options)
            ? options.GetRawText()
            : null;
        var visibleIfJson = field.TryGetProperty("visibleIf", out var visibleIf)
            ? visibleIf.GetRawText()
            : null;

        return Result.Success(new FormFieldDefinition(
            key.Trim(),
            label.Trim(),
            fieldType,
            required,
            ReadString(field, "defaultValue"),
            optionsJson,
            ReadScalar(field, "min") ?? ReadScalar(field, "minLength"),
            ReadScalar(field, "max") ?? ReadScalar(field, "maxLength"),
            ReadString(field, "regex") ?? ReadString(field, "pattern"),
            Math.Max(0, order),
            visibleIfJson));
    }

    private static Result<FormFieldDefinition> ParseLegacyField(
        string key,
        JsonElement property,
        bool required,
        int order)
    {
        if (property.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<FormFieldDefinition>(
                $"تعریف فیلد «{key}» معتبر نیست.",
                "FORMS_FIELD_INVALID");
        }

        var typeText = ReadString(property, "type") ?? "string";
        if (!TryMapType(typeText, out var fieldType))
        {
            return Result.Failure<FormFieldDefinition>(
                $"نوع فیلد «{key}» معتبر نیست.",
                "FORMS_FIELD_TYPE_INVALID");
        }

        return Result.Success(new FormFieldDefinition(
            key,
            ReadString(property, "title") ?? key,
            fieldType,
            required,
            ReadString(property, "default") ?? ReadString(property, "defaultValue"),
            property.TryGetProperty("enum", out var options) ? options.GetRawText() : null,
            ReadScalar(property, "min") ?? ReadScalar(property, "minimum") ?? ReadScalar(property, "minLength"),
            ReadScalar(property, "max") ?? ReadScalar(property, "maximum") ?? ReadScalar(property, "maxLength"),
            ReadString(property, "pattern"),
            order,
            null));
    }

    private static HashSet<string> ReadRequiredKeys(JsonElement root)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("required", out var required)
            && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in required.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    keys.Add(item.GetString()!);
                }
            }
        }

        return keys;
    }

    private static bool TryMapType(string value, out FormFieldType fieldType)
    {
        fieldType = value.Trim().ToLowerInvariant() switch
        {
            "text" or "string" => FormFieldType.Text,
            "textarea" => FormFieldType.TextArea,
            "number" or "integer" => FormFieldType.Number,
            "select" => FormFieldType.Select,
            "multiselect" or "multi-select" => FormFieldType.MultiSelect,
            "checkbox" or "boolean" => FormFieldType.Checkbox,
            "radio" => FormFieldType.Radio,
            "date" => FormFieldType.Date,
            "email" => FormFieldType.Email,
            "phone" or "tel" => FormFieldType.Phone,
            "file" => FormFieldType.File,
            _ => (FormFieldType)0,
        };

        return fieldType != 0;
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? ReadScalar(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) =>
                number.ToString(CultureInfo.InvariantCulture),
            JsonValueKind.String => value.GetString(),
            _ => null,
        };
    }
}
