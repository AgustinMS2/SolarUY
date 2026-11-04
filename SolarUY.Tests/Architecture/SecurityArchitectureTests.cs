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

    [Fact]
    public void MutatingEndpoints_Should_BeRestrictedToAdministrator()
    {
        var mutationAttributes = new[] { typeof(HttpPostAttribute), typeof(HttpPutAttribute), typeof(HttpPatchAttribute), typeof(HttpDeleteAttribute) };

        var notRestricted = Controllers()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => mutationAttributes.Any(a => m.IsDefined(a, inherit: true)))
            .Where(m => !m.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(a => a.Roles == Roles.Administrator))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        Assert.True(notRestricted.Count == 0, "Mutating endpoints not restricted to Administrador: " + string.Join(", ", notRestricted));
    }
}
