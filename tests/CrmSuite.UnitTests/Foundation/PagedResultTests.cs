using CrmSuite.BuildingBlocks.Domain.Results;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class PagedResultTests
{
    [Fact]
    public void Calculates_total_pages_and_navigation()
    {
        var page = new PagedResult<int>([1, 2, 3], totalCount: 21, page: 2, pageSize: 10);

        page.TotalPages.Should().Be(3);
        page.HasPrevious.Should().BeTrue();
        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public void Empty_page_has_no_next_page()
    {
        var page = PagedResult<int>.Empty(page: 1, pageSize: 20);

        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
        page.HasNext.Should().BeFalse();
    }

    [Fact]
    public void Map_preserves_paging_metadata()
    {
        var page = new PagedResult<int>([2, 4], totalCount: 8, page: 2, pageSize: 2);
        var mapped = page.Map(value => value * 10);

        mapped.Items.Should().Equal(20, 40);
        mapped.TotalCount.Should().Be(8);
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(2);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public void Rejects_invalid_paging(int page, int pageSize)
    {
        var action = () => new PagedResult<int>([], 0, page, pageSize);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
