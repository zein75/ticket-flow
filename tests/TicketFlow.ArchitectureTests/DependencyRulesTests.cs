using System.Reflection;
using TicketFlow.Application.Features.Auth;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Domain_ShouldNotReferenceApplicationInfrastructureOrApi()
    {
        var references = GetReferencedAssemblyNames(typeof(User).Assembly);

        Assert.DoesNotContain("TicketFlow.Application", references);
        Assert.DoesNotContain("TicketFlow.Infrastructure", references);
        Assert.DoesNotContain("TicketFlow.Api", references);
    }

    [Fact]
    public void Application_ShouldNotReferenceInfrastructureOrApi()
    {
        var references = GetReferencedAssemblyNames(typeof(AuthService).Assembly);

        Assert.DoesNotContain("TicketFlow.Infrastructure", references);
        Assert.DoesNotContain("TicketFlow.Api", references);
    }

    [Fact]
    public void Infrastructure_ShouldNotReferenceApi()
    {
        var references = GetReferencedAssemblyNames(typeof(TicketFlowDbContext).Assembly);

        Assert.DoesNotContain("TicketFlow.Api", references);
    }

    private static HashSet<string> GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
