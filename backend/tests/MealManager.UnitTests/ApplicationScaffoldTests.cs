using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using MealManager.Application;

namespace MealManager.UnitTests;

public class ApplicationScaffoldTests
{
    [Fact]
    public void AddApplication_RegistersMediatR()
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISender>());
    }
}
