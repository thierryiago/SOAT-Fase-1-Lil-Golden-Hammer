using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Application;

public sealed class CustomerServiceTests
{
    /*
    [Fact]
    public async Task Customer_crud_should_create_update_list_and_deactivate_customer()
    {
        var service = new CustomerService(new InMemoryCustomerRepository());
        var created = await service.CreateAsync(
            new CreateCustomerRequest("Maria Silva", "maria@email.com", "123", "529.982.247-25"),
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateCustomerRequest("Maria Souza", "maria.souza@email.com", "123", "529.982.247-25"),
            CancellationToken.None);
        var page = await service.ListAsync(new PageRequest("Souza", 1, 20), CancellationToken.None);
        var deleted = await service.DeleteAsync(created.Id, CancellationToken.None);
        var afterDelete = await service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.Equal("Maria Souza", updated.Name);
        Assert.Single(page.Items);
        Assert.True(deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task Customer_create_should_reject_duplicate_document()
    {
        var service = new CustomerService(new InMemoryCustomerRepository());
        await service.CreateAsync(
            new CreateCustomerRequest("Maria Silva", "maria@email.com", "123", "529.982.247-25"),
            CancellationToken.None);

        var act = () => service.CreateAsync(
            new CreateCustomerRequest("Outra Pessoa", "outra@email.com", "456", "52998224725"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(act);
    }
    */
}
