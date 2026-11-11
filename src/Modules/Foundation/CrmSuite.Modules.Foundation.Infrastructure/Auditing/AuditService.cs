using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Entities;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Foundation.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly AppDbContext _dbContext;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditService(
        AppDbContext dbContext,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var log = AuditLog.Create(
            entry.EntityName,
            entry.EntityId,
            entry.Action,
            SerializeAndRedact(entry.OldValues),
            SerializeAndRedact(entry.NewValues),
            entry.UserId ?? _currentUser.UserId,
            _currentUser.IpAddress,
            _clock.UtcNow);

        _dbContext.Set<AuditLog>().Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializeAndRedact(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);
        if (node is null)
        {
            return null;
        }

        Redact(node);
        return node.ToJsonString(SerializerOptions);
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                var isSecret = obj.TryGetPropertyValue("isSecret", out var flag)
                    && flag is JsonValue value
                    && value.TryGetValue<bool>(out var parsed)
                    && parsed;

                foreach (var propertyName in obj.Select(pair => pair.Key).ToArray())
                {
                    if (IsSensitiveName(propertyName)
                        || (isSecret && propertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
                    {
                        obj[propertyName] = "***";
                        continue;
                    }

                    Redact(obj[propertyName]);
                }

                break;
            }

            case JsonArray array:
                foreach (var item in array)
                {
                    Redact(item);
                }

                break;
        }
    }

    private static bool IsSensitiveName(string name)
    {
        var normalized = name
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        // Flags such as IsSecret describe a field and are not themselves sensitive.
        if (normalized is "issecret" or "isencrypted" or "hastoken")
        {
            return false;
        }

        return normalized.Contains("password", StringComparison.Ordinal)
            || normalized.Contains("secret", StringComparison.Ordinal)
            || normalized.Contains("token", StringComparison.Ordinal)
            || normalized.Contains("apikey", StringComparison.Ordinal)
            || normalized.Contains("privatekey", StringComparison.Ordinal)
            || normalized.Contains("connectionstring", StringComparison.Ordinal)
            || normalized is "pwd";
    }
}
