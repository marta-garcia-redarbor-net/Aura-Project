using Aura.Application.Models;

namespace Aura.UnitTests.WorkItems;

public class WorkItemPersistenceResultTests
{
    [Fact]
    public void Failure_WhenReasonIsWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => WorkItemPersistenceResult.Failure("   "));
    }
}
