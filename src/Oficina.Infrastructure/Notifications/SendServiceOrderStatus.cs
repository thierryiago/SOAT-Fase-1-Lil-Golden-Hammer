using Oficina.Application.Notifications;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Infrastructure.Notifications;

public sealed class SendServiceOrderStatus(IEmailTransport transport)
{
    public Task SendAsync(ServiceOrderStatusEmail context, CancellationToken cancellationToken) =>
        transport.SendAsync(context.CustomerEmail,
            $"OS {context.ServiceOrderId} - {StatusName(context.Status)}",
            BuildBody(context), false, cancellationToken);

    internal static string BuildBody(ServiceOrderStatusEmail context) =>
        $"Olá, {context.CustomerName}!\n\n" +
        $"Ordem de serviço: {context.ServiceOrderId}\n" +
        $"Veículo: {context.VehicleBrand} {context.VehicleModel} ({context.VehicleYear})\n" +
        $"- Placa: {context.VehiclePlate}\n" +
        $"- Marca: {context.VehicleBrand}\n- Modelo: {context.VehicleModel}\n- Ano: {context.VehicleYear}\n\n" +
        $"{EventDescription(context)}\nStatus atual: {StatusName(context.Status)}";

    private static string EventDescription(ServiceOrderStatusEmail context) => context.Status switch
    {
        ServiceOrderStatus.Created => "Sua ordem de serviço foi criada.",
        ServiceOrderStatus.Received => "Seu veículo foi recebido e a inspeção inicial foi registrada.",
        ServiceOrderStatus.InDiagnosis => "O mecânico iniciou o diagnóstico do seu veículo.",
        ServiceOrderStatus.AwaitingApproval when context.PreviousStatus == ServiceOrderStatus.InExecution =>
            "A composição de peças ou serviços foi alterada. Um novo orçamento exige sua aprovação para continuar a execução.",
        ServiceOrderStatus.AwaitingApproval => "O diagnóstico foi concluído. Seu orçamento está disponível para aprovação.",
        ServiceOrderStatus.InExecution => "Seu orçamento foi aprovado e os serviços estão em execução.",
        ServiceOrderStatus.Finalized => "Os serviços foram finalizados. Seu veículo está pronto para retirada na oficina.",
        ServiceOrderStatus.Delivered => "Seu veículo foi entregue. Obrigado por escolher nossa oficina!",
        ServiceOrderStatus.Rejected => "O orçamento foi rejeitado e a ordem de serviço foi encerrada sem prosseguir com os serviços.",
        _ => throw new ArgumentOutOfRangeException(nameof(context))
    };

    internal static string StatusName(ServiceOrderStatus status) => status switch
    {
        ServiceOrderStatus.Created => "Criada",
        ServiceOrderStatus.Received => "Recebida",
        ServiceOrderStatus.InDiagnosis => "Em diagnóstico",
        ServiceOrderStatus.AwaitingApproval => "Aguardando aprovação",
        ServiceOrderStatus.InExecution => "Em execução",
        ServiceOrderStatus.Finalized => "Finalizada",
        ServiceOrderStatus.Delivered => "Entregue",
        ServiceOrderStatus.Rejected => "Rejeitada",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
