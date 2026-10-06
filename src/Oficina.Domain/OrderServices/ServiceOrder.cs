using Oficina.Domain.Customers;
using Oficina.Domain.Mechanics;
using Oficina.Domain.OrderService;

namespace Oficina.Domain.ServiceOrders;

public sealed class ServiceOrder
{
    private const string RepeatedPartMessage = "A part cannot be repeated in the service order.";
    private const string RepeatedWorkshopServiceMessage = "A workshop service cannot be repeated in the service order.";

    // Campos mutaveis, nao readonly: Replace* atribui uma lista nova para manter
    // a semantica de substituicao que o EF ve hoje em Parts = parts.ToList().
    private List<ServiceOrderPart> _parts = [];
    private List<ServiceOrderWorkshop> _workshopServices = [];

    private ServiceOrder(Guid id, Guid customerId, Guid? vehicleId,
        string description)
    {
        Id = id;
        CustomerId = customerId;
        VehicleId = vehicleId;
        Description = description;
        Status = ServiceOrderStatus.Created;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid? MechanicId { get; private set; }
    public Guid? VehicleId { get; }
    public string Description { get; private set; }
    public string? CheckList { get; private set; }
    public ServiceOrderStatus? Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public decimal TotalParts { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public Mechanic? Mechanic { get; private set; }
    public Vehicle? Vehicle { get; set; }
    public IReadOnlyCollection<ServiceOrderPart> Parts => _parts;
    public IReadOnlyCollection<ServiceOrderWorkshop> WorkshopServices => _workshopServices;

    public static ServiceOrder Open(Guid customerId, Guid vehicleId, string description)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Service order description is required.", nameof(description));
        }

        var serviceOrder = new ServiceOrder(Guid.NewGuid(), customerId, vehicleId, description.Trim())
        {
            ScheduledAt = DateTimeOffset.UtcNow
        };

        return serviceOrder;
    }

    public void Update(
        Guid? mechanicId,
        string? description,
        string? checkList,
        IReadOnlyCollection<ServiceOrderPart>? parts,
        IReadOnlyCollection<ServiceOrderWorkshop>? workshopServices)
    {
        if (checkList is not null)
        {
            CheckList = checkList.Trim();
        }

        if (description is not null)
        {
            Description = description.Trim();
        }
        if (mechanicId is not null)
        {
            MechanicId = mechanicId;
        }

        if (parts is not null)
        {
            ReplaceParts(parts);
        }

        if (workshopServices is not null)
        {
            ReplaceWorkshopServices(workshopServices);
        }
    }

    /// <summary>
    /// Adiciona uma peca a ordem de servico, recusando uma peca que ja esteja
    /// presente. Junto de <see cref="AddWorkshopService"/>, e o unico ponto por
    /// onde as colecoes crescem, o que mantem a invariante de nao repeticao
    /// dentro do agregado.
    /// </summary>
    public void AddPart(ServiceOrderPart part)
    {
        ArgumentNullException.ThrowIfNull(part);

        if (_parts.Any(existing => existing.PartId == part.PartId))
        {
            throw new InvalidOperationException(RepeatedPartMessage);
        }

        _parts.Add(part);
        RecalculateTotalParts();
    }

    public void AddWorkshopService(ServiceOrderWorkshop workshopService)
    {
        ArgumentNullException.ThrowIfNull(workshopService);

        if (_workshopServices.Any(existing => existing.WorkshopServiceId == workshopService.WorkshopServiceId))
        {
            throw new InvalidOperationException(RepeatedWorkshopServiceMessage);
        }

        _workshopServices.Add(workshopService);
    }

    /// <summary>
    /// Valida os identificadores recebidos antes de qualquer efeito colateral.
    /// Existe porque o caso de uso reserva estoque (com persistencia imediata)
    /// enquanto resolve as pecas, muito antes de montar a colecao: recusar aqui
    /// evita debitar estoque de um pedido que seria rejeitado depois.
    /// </summary>
    public static void EnsureNoRepeatedParts(IEnumerable<Guid> partIds)
    {
        if (HasRepetition(partIds))
        {
            throw new InvalidOperationException(RepeatedPartMessage);
        }
    }

    public static void EnsureNoRepeatedWorkshopServices(IEnumerable<Guid> workshopServiceIds)
    {
        if (HasRepetition(workshopServiceIds))
        {
            throw new InvalidOperationException(RepeatedWorkshopServiceMessage);
        }
    }

    private static bool HasRepetition(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var seen = new HashSet<Guid>();
        return ids.Any(id => !seen.Add(id));
    }

    private void ReplaceParts(IReadOnlyCollection<ServiceOrderPart> parts)
    {
        _parts = [];
        foreach (var part in parts)
        {
            AddPart(part);
        }
    }

    private void ReplaceWorkshopServices(IReadOnlyCollection<ServiceOrderWorkshop> workshopServices)
    {
        _workshopServices = [];
        foreach (var workshopService in workshopServices)
        {
            AddWorkshopService(workshopService);
        }
    }

    private void RecalculateTotalParts() =>
        TotalParts = _parts.Sum(item => item.QuantityUsed * (item.Part?.UnitPrice ?? 0));

    public void UpdateStatus(
        bool? clientApproved = null,
        bool finalized = false,
        bool delivered = false)
    {
        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("A delivered service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Rejected)
        {
            throw new InvalidOperationException("A rejected service order cannot be changed.");
        }

        if (Receive())
            return;

        if (StartDiagnosis())
            return;

        if (RequestApproval())
            return;

        if (ResolveApproval(clientApproved))
            return;

        if (Finish(finalized))
            return;

        Deliver(delivered);
    }

    private void EnsureNotTerminal()
    {
        if (Status == ServiceOrderStatus.Finalized)
        {
            throw new InvalidOperationException("A finalized service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("A delivered service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Rejected)
        {
            throw new InvalidOperationException("A rejected service order cannot be changed.");
        }
    }

    private bool Receive()
    {
        if (Status is not (null or ServiceOrderStatus.Created) || string.IsNullOrWhiteSpace(CheckList))
        {
            return false;
        }

        Status = ServiceOrderStatus.Received;
        return true;
    }

    private bool StartDiagnosis()
    {
        if (Status != ServiceOrderStatus.Received || !MechanicId.HasValue)
        {
            return false;
        }

        Status = ServiceOrderStatus.InDiagnosis;
        return true;
    }

    private bool RequestApproval()
    {
        if (Status != ServiceOrderStatus.InDiagnosis || WorkshopServices.Count == 0)
        {
            return false;
        }

        Status = ServiceOrderStatus.AwaitingApproval;
        return true;
    }

    private bool ResolveApproval(bool? clientApproved)
    {
        if (Status != ServiceOrderStatus.AwaitingApproval || !clientApproved.HasValue)
        {
            return false;
        }

        Status = clientApproved.Value
            ? ServiceOrderStatus.InExecution
            : ServiceOrderStatus.Rejected;
        return true;
    }

    private bool Finish(bool finalized)
    {
        if (Status != ServiceOrderStatus.InExecution || !finalized)
        {
            return false;
        }

        Status = ServiceOrderStatus.Finalized;
        return true;
    }

    private void Deliver(bool delivered)
    {
        if (Status != ServiceOrderStatus.Finalized || !delivered)
        {
            return;
        }

        Status = ServiceOrderStatus.Delivered;
    }

    public void ValidateUpdate(
        Guid? newMechanicId,
        bool hasItemChanges)
    {
        EnsureNotTerminal();

        if (Status is
            ServiceOrderStatus.InDiagnosis or
            ServiceOrderStatus.AwaitingApproval or
            ServiceOrderStatus.InExecution)
        {
            if (newMechanicId is not null && newMechanicId != MechanicId)
            {
                throw new InvalidOperationException(
                    "The mechanic cannot be removed or changed at this stage.");
            }
        }

        if (hasItemChanges &&
            Status is null or ServiceOrderStatus.Created or ServiceOrderStatus.Received)
        {
            throw new InvalidOperationException(
                "Services and parts cannot be added at this stage.");
        }
    }

    public void RequestReapproval(bool hasItemChanges)
    {
        EnsureNotTerminal();

        if (!hasItemChanges)
        {
            return;
        }

        if (Status != ServiceOrderStatus.InExecution)
        {
            throw new InvalidOperationException(
                "Only a service order in execution can be submitted for reapproval.");
        }

        if (WorkshopServices.Count == 0)
        {
            throw new InvalidOperationException(
                "The service order must have at least one workshop service for reapproval.");
        }

        Status = ServiceOrderStatus.AwaitingApproval;
    }

}
