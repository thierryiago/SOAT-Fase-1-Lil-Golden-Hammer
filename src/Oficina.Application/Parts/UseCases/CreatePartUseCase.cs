using Oficina.Application.Common;
using Oficina.Application.Stocks;
using Oficina.Domain.Parts;
using Oficina.Domain.Stock;

namespace Oficina.Application.Parts.UseCases;

public class CreatePartUseCase(
    IPartRepository parts,
    IStockRepository stocks)
{
    private readonly IPartRepository _parts = parts;
    private readonly IStockRepository _stocks = stocks;

    public async Task<PartResponse> CreateAsync(
        CreatePartRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _parts.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A part or consumable with the informed code already exists.");
        }

        var part = Part.Create(
            request.Name,
            request.Code,
            request.UnitPrice,
            request.Kind);
        await _parts.AddAsync(part, cancellationToken);
        await _stocks.AddAsync(StockPart.Create(part.Id, 0), cancellationToken);
        return PartResponseMapper.Map(part);
    }
}
