using CrmSuite.Modules.Settings.Domain.Entities;
using CrmSuite.Modules.Settings.Domain.Enums;

namespace CrmSuite.Modules.Settings.Application.Abstractions;

public interface ITaxRuleRepository
{
    Task<IReadOnlyList<TaxRule>> GetAllAsync(CancellationToken cancellationToken);

    Task<TaxRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(TaxRule rule);
}

public interface IGatewayConfigRepository
{
    Task<GatewayConfig?> GetByProviderAsync(GatewayProvider provider, CancellationToken cancellationToken);

    Task<IReadOnlyList<GatewayConfig>> GetAllAsync(CancellationToken cancellationToken);

    void Add(GatewayConfig config);
}

public interface ISettingsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
