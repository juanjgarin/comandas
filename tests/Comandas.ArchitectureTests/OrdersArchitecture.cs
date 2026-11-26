using Assembly = System.Reflection.Assembly;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Comandas.ArchitectureTests;

/// <summary>
/// Loads the compiled Orders assemblies once and names each Clean Architecture layer,
/// so the rules read like the architecture diagram in PLAN.md.
/// </summary>
internal static class OrdersArchitecture
{
    // Loaded by name: the layers are referenced by this project, so their DLLs sit next to the test assembly.
    public static readonly Assembly SharedKernelAssembly = Assembly.Load("Comandas.SharedKernel");
    public static readonly Assembly DomainAssembly = Assembly.Load("Orders.Domain");
    public static readonly Assembly ApplicationAssembly = Assembly.Load("Orders.Application");
    public static readonly Assembly InfrastructureAssembly = Assembly.Load("Orders.Infrastructure");
    public static readonly Assembly ApiAssembly = Assembly.Load("Orders.API");

    public static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(SharedKernelAssembly, DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly)
        .Build();

    public static readonly IObjectProvider<IType> SharedKernelLayer =
        Types().That().ResideInAssembly(SharedKernelAssembly).As("SharedKernel");

    public static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Orders.Domain");

    public static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Orders.Application");

    public static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Orders.Infrastructure");

    public static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(ApiAssembly).As("Orders.API");
}
