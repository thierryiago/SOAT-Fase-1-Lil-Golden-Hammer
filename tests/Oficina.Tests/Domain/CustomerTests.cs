using Oficina.Domain.Customers;

namespace Oficina.Tests.Domain;

public sealed class CustomerTests
{
    [Fact]
    public void Create_should_normalize_email_and_document()
    {
        var customer = Customer.Create("Maria Silva", "  MARIA@EMAIL.COM  ", " 123.456.789-00 ");

        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("maria@email.com", customer.Email);
        Assert.Equal("123.456.789-00", customer.Document);
    }

    [Fact]
    public void Create_should_reject_empty_name()
    {
        var act = () => Customer.Create(" ", "cliente@email.com", "123");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Vehicle_create_should_link_vehicle_to_customer()
    {
        var customer = Customer.Create("Maria Silva", "maria@email.com", "123.456.789-00");

        var vehicle = Vehicle.Create(customer.Id, "ABC1D23", "Honda", "Civic", 2021);

        Assert.Equal(customer.Id, vehicle.CustomerId);
        Assert.Equal("ABC1D23", vehicle.Plate);
        Assert.Equal("Honda", vehicle.Brand);
        Assert.Equal("Civic", vehicle.Model);
        Assert.Equal(2021, vehicle.Year);
    }

    [Fact]
    public void Vehicle_create_should_reject_invalid_year()
    {
        var customer = Customer.Create("Maria Silva", "maria@email.com", "123.456.789-00");

        var act = () => Vehicle.Create(customer.Id, "ABC1D23", "Honda", "Civic", 1800);

        Assert.Throws<ArgumentException>(act);
    }
}
