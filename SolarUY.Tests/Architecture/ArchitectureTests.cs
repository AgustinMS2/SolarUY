using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using Xunit;

namespace SolarUY.Tests.Architecture;

public class ArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(global::Program).Assembly;

    [Fact]
    public void Controllers_Should_InheritFromControllerBase()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("SolarUY.ApiService.Controllers")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(ControllerBase))
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ServicesAndRepositories_ShouldNot_DependOnControllersOrMvc()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceEndingWith("Services")
            .Or()
            .ResideInNamespaceEndingWith("Repositories")
            .ShouldNot()
            .HaveDependencyOnAny("SolarUY.ApiService.Controllers", "Microsoft.AspNetCore.Mvc")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Controllers_ShouldNot_DependOnRepositories()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceEndingWith("Controllers")
            .ShouldNot()
            .HaveDependencyOn("SolarUY.ApiService.Repositories")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
