using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static Comandas.ArchitectureTests.OrdersArchitecture;

namespace Comandas.ArchitectureTests;

/// <summary>
/// The Dependency Rule of Clean Architecture, made executable:
/// dependencies point inwards (API -> Infrastructure -> Application -> Domain -> SharedKernel), never outwards,
/// and the inner layers stay free of frameworks.
/// </summary>
/// <remarks>
/// Every rule uses <c>WithoutRequiringPositiveResults()</c>: by default ArchUnitNET fails a rule that matches
/// no types, and some layers are (still) empty. <see cref="LayerAssemblies_AreLoaded"/> guards against the
/// opposite risk: a rule silently passing because its assembly was never loaded.
/// </remarks>
public sealed class OrdersLayerDependencyTests
{
    // Namespace prefixes of infrastructure-level frameworks. Matches the namespace itself and any sub-namespace.
    private const string FrameworkNamespaces =
        @"^(Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|MediatR|MassTransit|StackExchange\.Redis|FluentValidation)(\..*)?$";

    private const string PersistenceAndTransportNamespaces =
        @"^(Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|MassTransit|StackExchange\.Redis)(\..*)?$";

    [Fact]
    public void LayerAssemblies_AreLoaded()
    {
        var loadedAssemblyNames = Architecture.Assemblies.Select(assembly => assembly.Name).ToList();

        loadedAssemblyNames.Should().HaveCount(5);
        loadedAssemblyNames.Should().Contain(name => name.StartsWith("Orders.Domain", StringComparison.Ordinal));
        loadedAssemblyNames.Should().Contain(name => name.StartsWith("Orders.API", StringComparison.Ordinal));
    }

    [Fact]
    public void SharedKernel_ShouldNotDependOn_AnyServiceLayer()
    {
        IArchRule rule = Types().That().Are(SharedKernelLayer)
            .Should().NotDependOnAny(Types().That().Are(DomainLayer).Or().Are(ApplicationLayer).Or().Are(InfrastructureLayer).Or().Are(ApiLayer))
            .Because("the SharedKernel is reused by every service and cannot know any of them")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_OuterLayers()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(Types().That().Are(ApplicationLayer).Or().Are(InfrastructureLayer).Or().Are(ApiLayer))
            .Because("the Domain is the core: outer layers depend on it, never the other way round")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureOrApi()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(Types().That().Are(InfrastructureLayer).Or().Are(ApiLayer))
            .Because("Application declares interfaces that Infrastructure implements (Dependency Inversion)")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(ApiLayer)
            .Because("the API is the composition root; nothing else may depend on it")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void DomainAndSharedKernel_ShouldNotDependOn_Frameworks()
    {
        IArchRule rule = Types().That().Are(DomainLayer).Or().Are(SharedKernelLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(FrameworkNamespaces)
            .Because("business rules must not change when a framework (EF Core, ASP.NET, MediatR...) changes")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_ShouldNotDependOn_PersistenceOrTransportFrameworks()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(PersistenceAndTransportNamespaces)
            .Because("database, HTTP, messaging and cache details belong to Infrastructure and API")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }
}
