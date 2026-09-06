using System.Reflection;
using ArchitectureStandardsInitExample.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureStandardsInitExample.Api.Tests.Infrastructure;

/// <summary>
/// P2's ceiling, half of it. The other half is the `kernel-size` step in
/// ci.yml, which fails past ~800 lines.
/// <para>
/// Both exist because stating the limit in prose has already failed twice in this
/// estate: one shared library began as plumbing and ended holding advert
/// entities and a pricing table, and a second grew a 607-line file of seeded
/// domain prompts. These tests cost ten minutes at t=0 and are the only thing
/// standing between this kernel and the same ending.
/// </para>
/// </summary>
public sealed class KernelBoundaryTests
{
    private static readonly Assembly Kernel = typeof(Extensions).Assembly;

    [Fact]
    public void Kernel_holds_no_entity_type()
    {
        var entityTypes = Kernel.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type =>
                typeof(DbContext).IsAssignableFrom(type)
                || type.GetProperties().Any(property =>
                    property.PropertyType.IsGenericType
                    && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            entityTypes.Count == 0,
            $"The shared kernel must hold no entity type or DbContext (P2). Found: {string.Join(", ", entityTypes)}. " +
            "Business data belongs to the service that owns it, or to the Contracts project if it crosses a boundary.");
    }

    [Fact]
    public void Kernel_does_not_reference_a_service_or_its_contracts()
    {
        // A kernel that compiles a service's assembly has stopped being a kernel:
        // every consumer is then coupled to that service's every change.
        var forbidden = Kernel.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && name.StartsWith("ArchitectureStandardsInitExample.", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            forbidden.Count == 0,
            $"The shared kernel must not reference another project in this solution (P2). Found: {string.Join(", ", forbidden)}.");
    }

    [Fact]
    public void Kernel_exports_extension_methods_and_nothing_to_inherit_from()
    {
        // P2: extension methods over IHostApplicationBuilder, IServiceCollection
        // or WebApplication. No base classes, no inheritance chains, no
        // ModuleBase to derive from (P10).
        var inheritable = Kernel.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && !type.IsSealed && !type.IsAbstract)
            .Where(type => type.GetConstructors().Length > 0)
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            inheritable.Count == 0,
            $"The kernel exports a type that can be inherited from: {string.Join(", ", inheritable)}. " +
            "Extensibility in this estate is an interface plus a DI registration, not a base class (P10).");
    }
}
