using Oficina.Domain.Customers;
using Oficina.Domain.Vehicles;

namespace Oficina.Tests.Domain;

public sealed class CustomerTests
{
    [Fact]
    public void Create_should_normalize_email_and_document()
    {
        var customer = Customer.Create("Maria Silva", "  MARIA@EMAIL.COM  ", "11999990000", " 529.982.247-25 ");

        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("maria@email.com", customer.Email);
        Assert.Equal("529.982.247-25", customer.Document);
    }

    [Fact]
    public void Create_should_reject_empty_name()
    {
        var act = () => Customer.Create(" ", "cliente@email.com", "123", "529.982.247-25");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_repeated_digit_cpf()
    {
        var act = () => Customer.Create("Maria Silva", "maria@email.com", "123", "000.000.000-00");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_repeated_digit_cnpj()
    {
        var act = () => Customer.Create("Maria Silva", "maria@email.com", "123", "00.000.000/0000-00");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Vehicle_create_should_normalize_plate_with_hyphen()
    {
        var customer = Customer.Create("Maria Silva", "maria@email.com", "123", "529.982.247-25");

        var vehicle = Vehicle.Create(customer.Id, "abc-1234", "Honda", "Civic", 2021, EnumVehicleCategory.Car);
        var vehicleWithoutHyphen = Vehicle.Create(customer.Id, "abc1234", "Honda", "Civic", 2021, EnumVehicleCategory.Car);

        Assert.Equal("ABC-1234", vehicle.Plate);
        Assert.Equal("ABC-1234", vehicleWithoutHyphen.Plate);
    }

    [Fact]
    public void Vehicle_create_should_link_vehicle_to_customer()
    {
        var customer = Customer.Create("Maria Silva", "maria@email.com", "123", "529.982.247-25");

        var vehicle = Vehicle.Create(customer.Id, "ABC1D23", "Honda", "Civic", 2021, EnumVehicleCategory.Car);

        Assert.Equal(customer.Id, vehicle.CustomerId);
        Assert.Equal("ABC1D23", vehicle.Plate);
        Assert.Equal("Honda", vehicle.Brand);
        Assert.Equal("Civic", vehicle.Model);
        Assert.Equal(2021, vehicle.Year);
    }

    [Fact]
    public void Vehicle_create_should_reject_invalid_year()
    {
        var customer = Customer.Create("Maria Silva", "maria@email.com", "123", "529.982.247-25");

        var act = () => Vehicle.Create(customer.Id, "ABC1D23", "Honda", "Civic", 1800, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }
}
