using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Stocks;
using Oficina.Application.Stocks.UseCases;
using Oficina.Application.Stocks.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/stocks")]
[ExcludeFromCodeCoverage]
public sealed class StocksController : ControllerBase
{
    private readonly EntryStockUseCase _entryStock;
    private readonly ConsumeStockUseCase _consumeStock;
    private readonly AdjustStockUseCase _adjustStock;
    private readonly ListStocksUseCase _listStocks;
    private readonly GetStockByIdUseCase _getStockById;

    public StocksController(
        EntryStockUseCase entryStock,
        ConsumeStockUseCase consumeStock,
        AdjustStockUseCase adjustStock,
        ListStocksUseCase listStocks,
        GetStockByIdUseCase getStockById)
    {
        _entryStock = entryStock;
        _consumeStock = consumeStock;
        _adjustStock = adjustStock;
        _listStocks = listStocks;
        _getStockById = getStockById;
    }

    [HttpGet(Name = "ListStocks")]
    [ProducesResponseType(typeof(PagedResponse<StockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var stocks = await _listStocks.ListAsync(request, cancellationToken);
        return Ok(stocks);
    }

    [HttpGet("{id:guid}", Name = "GetStockById")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stock = await _getStockById.GetByIdAsync(id, cancellationToken);
        return stock is null ? NotFound() : Ok(stock);
    }

    [HttpPut("stocks-part/{partId:guid}/entries", Name = "EntryStock")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Entry(
        Guid partId,
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _entryStock.EntryAsync(partId, request, cancellationToken);
        return Ok(stock);
    }

    [HttpPut("stocks-part/{partId:guid}/consumptions", Name = "ConsumeStock")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Consume(
        Guid partId,
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _consumeStock.ConsumeAsync(partId, request, cancellationToken);
        return Ok(stock);
    }

    [HttpPut("stocks-part/{partId:guid}/adjustments", Name = "AdjustStock")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Adjust(
        Guid partId,
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _adjustStock.AdjustAsync(partId, request, cancellationToken);
        return Ok(stock);
    }
}
