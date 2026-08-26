using Oficina.Application.Common;

namespace Oficina.Tests.Application;

public sealed class PaginationTests
{
    [Fact]
    public void Create_should_reject_page_less_than_one()
    {
        var act = () => Pagination.Create(Array.Empty<int>(), new PageRequest(Page: 0));

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_should_reject_page_size_out_of_range(int pageSize)
    {
        var act = () => Pagination.Create(Array.Empty<int>(), new PageRequest(PageSize: pageSize));

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_should_return_zero_total_pages_for_empty_source()
    {
        var result = Pagination.Create(Array.Empty<int>(), new PageRequest());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Create_should_paginate_source_items()
    {
        var source = Enumerable.Range(1, 25);

        var result = Pagination.Create(source, new PageRequest(Page: 2, PageSize: 10));

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(11, result.Items.First());
        Assert.Equal(25, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }
}
