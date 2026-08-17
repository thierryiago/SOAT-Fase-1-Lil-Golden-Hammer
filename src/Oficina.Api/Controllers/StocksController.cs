using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Stocks;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController : ControllerBase
{
    private readonly StockService _stocks;

    public StocksController(StockService stocks)
    {
        _stocks = stocks;
    }

    [HttpGet(Name = "ListStocks")]
    [ProducesResponseType(typeof(PagedResponse<StockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var stocks = await _stocks.ListAsync(request, cancellationToken);
        return Ok(stocks);
    }

    [HttpGet("{id:guid}", Name = "GetStockById")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stock = await _stocks.GetByIdAsync(id, cancellationToken);
        return stock is null ? NotFound() : Ok(stock);
    }

    [HttpPut("{partId:guid}/entries", Name = "EntryStock")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Entry(
        Guid partId,
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _stocks.EntryAsync(partId, request, cancellationToken);
        return Ok(stock);
    }

    [HttpPut("{partId:guid}/consumptions", Name = "ConsumeStock")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Consume(
        Guid partId,
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _stocks.ConsumeAsync(partId, request, cancellationToken);
        return Ok(stock);
    }

    [HttpPut("{partId:guid}/adjustments", Name = "AdjustStock")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Adjust(
        Guid partId,
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _stocks.AdjustAsync(partId, request, cancellationToken);
        return Ok(stock);
    }
}
