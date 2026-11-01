using InvoiceApp.Api.Controllers;
using NetArchTest.Rules;
using Xunit;

namespace InvoiceApp.Tests;

public class ArchitectureTests
{
    [Fact]
    public void ApiControllers_ShouldNotDependOnPayExOrInfrastructure()
    {
        var controllers = Types.InAssembly(typeof(InvoicesController).Assembly)
            .That()
            .ResideInNamespace("InvoiceApp.Api.Controllers");

        var payExResult = controllers.ShouldNot().HaveDependencyOn("PayEx").GetResult();
        var infrastructureResult = controllers.ShouldNot().HaveDependencyOn("Infrastructure").GetResult();

        Assert.True(payExResult.IsSuccessful, "API controllers must not depend on PayEx.");
        Assert.True(infrastructureResult.IsSuccessful, "API controllers must not depend on Infrastructure.");
    }
}
