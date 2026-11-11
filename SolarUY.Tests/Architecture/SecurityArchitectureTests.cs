using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using SolarUY.ApiService.Security;
using Xunit;

namespace SolarUY.Tests.Architecture;

public class SecurityArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(global::Program).Assembly;

    private static IEnumerable<Type> Controllers()
    {
        return Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("SolarUY.ApiService.Controllers")
            .And()
            .AreClasses()
            .GetTypes();
    }

    [Fact]
    public void Controllers_Should_RequireAuthenticatedUserWithRole()
    {
        Assert.True(Controllers().Count() >= 3, "The architecture test did not find the controllers.");

        var unprotected = Controllers()
            .Where(t => !t.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(a => !string.IsNullOrWhiteSpace(a.Roles)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(unprotected.Count == 0, "Controllers without [Authorize(Roles = ...)]: " + string.Join(", ", unprotected));
    }

    // Mutating endpoints that the Técnico role may also use. Every entry must be justified by a spec and
    // is expected to allow exactly Administrador and Técnico (Roles.Any). Keep this list as short as possible.
    private static readonly HashSet<string> TechnicianAllowedMutations =
    [
        "MaintenancesController.ConfirmMaintenance" // Spec 026: the technician confirms the maintenance he performed.
    ];

    private static IEnumerable<MethodInfo> MutatingEndpoints()
    {
        var mutationAttributes = new[] { typeof(HttpPostAttribute), typeof(HttpPutAttribute), typeof(HttpPatchAttribute), typeof(HttpDeleteAttribute) };

        return Controllers()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => mutationAttributes.Any(a => m.IsDefined(a, inherit: true)));
    }

    private static string NameOf(MethodInfo method) => $"{method.DeclaringType!.Name}.{method.Name}";

    [Fact]
    public void MutatingEndpoints_Should_BeRestrictedToAdministrator_UnlessExplicitlyAllowedForTechnician()
    {
        var notRestricted = MutatingEndpoints()
            .Where(m =>
            {
                var expectedRoles = TechnicianAllowedMutations.Contains(NameOf(m)) ? Roles.Any : Roles.Administrator;
                return !m.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(a => a.Roles == expectedRoles);
            })
            .Select(NameOf)
            .ToList();

        Assert.True(notRestricted.Count == 0, "Mutating endpoints with the wrong roles: " + string.Join(", ", notRestricted));
    }

    [Fact]
    public void TechnicianAllowedMutations_Should_OnlyListEndpointsThatExist()
    {
        var existing = MutatingEndpoints().Select(NameOf).ToHashSet();

        var stale = TechnicianAllowedMutations.Where(name => !existing.Contains(name)).ToList();

        Assert.True(stale.Count == 0, "Allow-list entries without a matching mutating endpoint: " + string.Join(", ", stale));
    }
}
