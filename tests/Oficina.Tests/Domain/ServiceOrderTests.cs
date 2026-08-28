using Oficina.Domain.OrderService;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Domain;

public sealed class ServiceOrderTests
{
    [Fact]
    public void Open_starts_with_null_status()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");

        Assert.Null(serviceOrder.Status);
    }

[Fact]
    public void Update_preserves_optional_text_when_it_is_not_provided()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");

        serviceOrder.Update(
            mechanicId: null,
            description: null,
            checkList: null,
            parts: null,
            workshopServices: null);

        Assert.Equal("Initial description", serviceOrder.Description);
        Assert.Null(serviceOrder.CheckList);
    }

    [Fact]
    public void Update_preserves_the_assigned_mechanic_when_it_is_not_provided()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");
        var mechanicId = Guid.NewGuid();
        AssignMechanic(serviceOrder, mechanicId);

        serviceOrder.Update(
            mechanicId: null,
            description: null,
            checkList: "Checklist ok",
            parts: null,
            workshopServices: null);

        Assert.Equal(mechanicId, serviceOrder.MechanicId);
    }

    [Fact]
    public void UpdateStatus_still_advances_to_InDiagnosis_after_a_later_update_omits_the_mechanic()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");
        AssignMechanic(serviceOrder, Guid.NewGuid());
        AddChecklist(serviceOrder);
        serviceOrder.UpdateStatus();

        serviceOrder.Update(
            mechanicId: null,
            description: null,
            checkList: null,
            parts: null,
            workshopServices: null);
        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.InDiagnosis, serviceOrder.Status);
    }

    [Fact]
    public void ValidateUpdate_allows_omitting_the_mechanic_once_diagnosis_has_started()
    {
        var serviceOrder = OpenAndAdvanceToInDiagnosis();

        var exception = Record.Exception(() =>
            serviceOrder.ValidateUpdate(newMechanicId: null, hasNewItems: false));

        Assert.Null(exception);
    }

    [Fact]
    public void UpdateStatus_does_nothing_when_checklist_is_missing()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");

        serviceOrder.UpdateStatus();

        Assert.Null(serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_moves_to_Received_once_checklist_is_set()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");
        AddChecklist(serviceOrder);

        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.Received, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_does_not_advance_past_Received_without_a_mechanic()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");
        AddChecklist(serviceOrder);
        serviceOrder.UpdateStatus();

        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.Received, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_advances_only_one_step_even_when_checklist_and_mechanic_arrive_together()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");

        AddChecklist(serviceOrder);
        AssignMechanic(serviceOrder, Guid.NewGuid());
        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.Received, serviceOrder.Status);

        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.InDiagnosis, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_does_not_advance_to_AwaitingApproval_when_only_a_part_is_added()
    {
        var serviceOrder = OpenAndAdvanceToInDiagnosis();

        AddPart(serviceOrder);
        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.InDiagnosis, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_moves_to_AwaitingApproval_once_a_workshop_service_is_added()
    {
        var serviceOrder = OpenAndAdvanceToInDiagnosis();

        AddWorkshopService(serviceOrder);
        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_moving_to_AwaitingApproval_still_allows_a_part_to_have_been_added_alongside_the_service()
    {
        var serviceOrder = OpenAndAdvanceToInDiagnosis();

        AddPart(serviceOrder);
        AddWorkshopService(serviceOrder);
        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_leaves_AwaitingApproval_untouched_when_no_decision_is_given()
    {
        var serviceOrder = OpenAndAdvanceToAwaitingApproval();

        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_client_approval_moves_to_InExecution()
    {
        var serviceOrder = OpenAndAdvanceToAwaitingApproval();

        serviceOrder.UpdateStatus(clientApproved: true);

        Assert.Equal(ServiceOrderStatus.InExecution, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_client_rejection_moves_to_Rejected()
    {
        var serviceOrder = OpenAndAdvanceToAwaitingApproval();

        serviceOrder.UpdateStatus(clientApproved: false);

        Assert.Equal(ServiceOrderStatus.Rejected, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_does_not_finalize_while_still_in_execution_unless_asked()
    {
        var serviceOrder = OpenAndAdvanceToInExecution();

        serviceOrder.UpdateStatus();

        Assert.Equal(ServiceOrderStatus.InExecution, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_finalized_flag_moves_InExecution_to_Finalized()
    {
        var serviceOrder = OpenAndAdvanceToInExecution();

        serviceOrder.UpdateStatus(finalized: true);

        Assert.Equal(ServiceOrderStatus.Finalized, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_finalized_flag_is_ignored_outside_InExecution()
    {
        var serviceOrder = OpenAndAdvanceToAwaitingApproval();

        serviceOrder.UpdateStatus(finalized: true);

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_delivered_flag_moves_Finalized_to_Delivered()
    {
        var serviceOrder = OpenAndAdvanceToFinalized();

        serviceOrder.UpdateStatus(delivered: true);

        Assert.Equal(ServiceOrderStatus.Delivered, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_delivered_flag_is_ignored_before_Finalized()
    {
        var serviceOrder = OpenAndAdvanceToInExecution();

        serviceOrder.UpdateStatus(delivered: true);

        Assert.Equal(ServiceOrderStatus.InExecution, serviceOrder.Status);
    }

    [Fact]
    public void UpdateStatus_throws_once_order_is_Delivered()
    {
        var serviceOrder = OpenAndAdvanceToFinalized();
        serviceOrder.UpdateStatus(delivered: true);

        Assert.Throws<InvalidOperationException>(() => serviceOrder.UpdateStatus());
    }

    [Fact]
    public void UpdateStatus_throws_once_order_is_Rejected()
    {
        var serviceOrder = OpenAndAdvanceToAwaitingApproval();
        serviceOrder.UpdateStatus(clientApproved: false);

        Assert.Throws<InvalidOperationException>(() => serviceOrder.UpdateStatus());
    }

    [Fact]
    public void ValidateUpdate_throws_for_Finalized_Delivered_and_Rejected_orders()
    {
        var finalized = OpenAndAdvanceToFinalized();
        var delivered = OpenAndAdvanceToFinalized();
        delivered.UpdateStatus(delivered: true);
        var rejected = OpenAndAdvanceToAwaitingApproval();
        rejected.UpdateStatus(clientApproved: false);

        Assert.Throws<InvalidOperationException>(() => finalized.ValidateUpdate(finalized.MechanicId, hasNewItems: false));
        Assert.Throws<InvalidOperationException>(() => delivered.ValidateUpdate(delivered.MechanicId, hasNewItems: false));
        Assert.Throws<InvalidOperationException>(() => rejected.ValidateUpdate(rejected.MechanicId, hasNewItems: false));
    }

    [Fact]
    public void ValidateUpdate_blocks_changing_the_mechanic_once_diagnosis_has_started()
    {
        var serviceOrder = OpenAndAdvanceToInDiagnosis();

        Assert.Throws<InvalidOperationException>(() =>
            serviceOrder.ValidateUpdate(Guid.NewGuid(), hasNewItems: false));
    }

    [Fact]
    public void ValidateUpdate_blocks_new_items_before_a_mechanic_is_assigned()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");

        Assert.Throws<InvalidOperationException>(() =>
            serviceOrder.ValidateUpdate(newMechanicId: null, hasNewItems: true));
    }

    [Fact]
    public void ValidateUpdate_blocks_new_items_while_in_execution()
    {
        var serviceOrder = OpenAndAdvanceToInExecution();

        Assert.Throws<InvalidOperationException>(() =>
            serviceOrder.ValidateUpdate(serviceOrder.MechanicId, hasNewItems: true));
    }

    [Fact]
    public void Full_lifecycle_only_ever_moves_forward_through_the_expected_sequence()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");
        var visited = new List<ServiceOrderStatus?> { serviceOrder.Status };

        AddChecklist(serviceOrder);
        serviceOrder.UpdateStatus();
        visited.Add(serviceOrder.Status);

        AssignMechanic(serviceOrder, Guid.NewGuid());
        serviceOrder.UpdateStatus();
        visited.Add(serviceOrder.Status);

        AddWorkshopService(serviceOrder);
        serviceOrder.UpdateStatus();
        visited.Add(serviceOrder.Status);

        serviceOrder.UpdateStatus(clientApproved: true);
        visited.Add(serviceOrder.Status);

        serviceOrder.UpdateStatus(finalized: true);
        visited.Add(serviceOrder.Status);

        serviceOrder.UpdateStatus(delivered: true);
        visited.Add(serviceOrder.Status);

        Assert.Equal(
            new ServiceOrderStatus?[]
            {
                null,
                ServiceOrderStatus.Received,
                ServiceOrderStatus.InDiagnosis,
                ServiceOrderStatus.AwaitingApproval,
                ServiceOrderStatus.InExecution,
                ServiceOrderStatus.Finalized,
                ServiceOrderStatus.Delivered,
            },
            visited);

        // Once delivered, nothing can move the order again - forward or backward.
        Assert.Throws<InvalidOperationException>(() => serviceOrder.UpdateStatus(clientApproved: true));
        Assert.Throws<InvalidOperationException>(() => serviceOrder.UpdateStatus(finalized: true));
        Assert.Equal(ServiceOrderStatus.Delivered, serviceOrder.Status);
    }

    private static ServiceOrder OpenAndAdvanceToInDiagnosis()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Initial description");
        AddChecklist(serviceOrder);
        serviceOrder.UpdateStatus();
        AssignMechanic(serviceOrder, Guid.NewGuid());
        serviceOrder.UpdateStatus();
        return serviceOrder;
    }

    private static ServiceOrder OpenAndAdvanceToAwaitingApproval()
    {
        var serviceOrder = OpenAndAdvanceToInDiagnosis();
        AddWorkshopService(serviceOrder);
        serviceOrder.UpdateStatus();
        return serviceOrder;
    }

    private static ServiceOrder OpenAndAdvanceToInExecution()
    {
        var serviceOrder = OpenAndAdvanceToAwaitingApproval();
        serviceOrder.UpdateStatus(clientApproved: true);
        return serviceOrder;
    }

    private static ServiceOrder OpenAndAdvanceToFinalized()
    {
        var serviceOrder = OpenAndAdvanceToInExecution();
        serviceOrder.UpdateStatus(finalized: true);
        return serviceOrder;
    }

    private static void AddChecklist(ServiceOrder serviceOrder) =>
        serviceOrder.Update(serviceOrder.MechanicId, description: null, checkList: "Checklist ok", parts: null, workshopServices: null);

    private static void AssignMechanic(ServiceOrder serviceOrder, Guid mechanicId) =>
        serviceOrder.Update(mechanicId, description: null, checkList: null, parts: null, workshopServices: null);

    private static void AddPart(ServiceOrder serviceOrder) =>
        serviceOrder.Update(
            serviceOrder.MechanicId,
            description: null,
            checkList: null,
            parts: new[] { ServiceOrderPart.Create(Guid.NewGuid(), serviceOrder.Id, 1) },
            workshopServices: null);

    private static void AddWorkshopService(ServiceOrder serviceOrder) =>
        serviceOrder.Update(
            serviceOrder.MechanicId,
            description: null,
            checkList: null,
            parts: null,
            workshopServices: new[] { ServiceOrderWorkshop.Create(serviceOrder.Id, Guid.NewGuid()) });
}
