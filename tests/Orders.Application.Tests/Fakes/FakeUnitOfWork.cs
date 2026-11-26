using Comandas.Orders.Application.Abstractions;

namespace Comandas.Orders.Application.Tests.Fakes;

/// <summary>Records how many times the use case committed, so tests can check "saved exactly once" or "never saved".</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}
