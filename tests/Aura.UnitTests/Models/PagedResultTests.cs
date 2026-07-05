namespace Aura.UnitTests.Models;

public class PagedResultTests
{
    [Fact]
    public void EmptyResult_HasZeroItemsAndPages()
    {
        var result = new Aura.Application.Models.PagedResult<string>();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.Page);
        Assert.Equal(0, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ComputesCorrectly()
    {
        var result = new Aura.Application.Models.PagedResult<string>
        {
            Items = new[] { "a", "b", "c" },
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WhenPageSizeIsZero_ReturnsZero()
    {
        var result = new Aura.Application.Models.PagedResult<string>
        {
            Items = Array.Empty<string>(),
            TotalCount = 10,
            Page = 1,
            PageSize = 0
        };

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void SinglePage_ReturnsOneTotalPage()
    {
        var result = new Aura.Application.Models.PagedResult<string>
        {
            Items = new[] { "a", "b", "c" },
            TotalCount = 3,
            Page = 1,
            PageSize = 20
        };

        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public void ExactFit_ReturnsCorrectPageCount()
    {
        var result = new Aura.Application.Models.PagedResult<string>
        {
            Items = new[] { "a", "b", "c", "d", "e" },
            TotalCount = 20,
            Page = 1,
            PageSize = 5
        };

        Assert.Equal(4, result.TotalPages);
    }
}
