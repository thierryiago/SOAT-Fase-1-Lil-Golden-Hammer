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
        Assert.Equal("52998224725", customer.Document);
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
    public void Create_should_accept_valid_cnpj()
    {
        var customer = Customer.Create("Empresa LTDA", "contato@empresa.com", "1140028922", "11.222.333/0001-81");

        Assert.Equal("11222333000181", customer.Document);
    }

    [Fact]
    public void Create_should_reject_document_with_no_digits()
    {
        var act = () => Customer.Create("Maria Silva", "maria@email.com", "123", "---");

        Assert.Throws<ArgumentException>(act);
    }

    // Item 6 of docs/analise-gaps-e-cenarios-faltantes.md: unlike the "all digits repeated" or
    // "no digits at all" cases above, this CPF has the right amount of non-repeated digits and
    // *looks* well-formed, but its check digit is mathematically wrong (mirrors the HTTP-level
    // coverage already in tests/Oficina.Api.ContractTests/Domains/Customer.cs, at the faster
    // domain-unit level).
    [Fact]
    public void Create_should_reject_cpf_with_correct_digit_count_but_wrong_check_digit()
    {
        // 111.444.777-35 is a valid CPF; flipping only the last check digit keeps the length and
        // "non repeated digits" shape intact while making the mod-11 calculation fail.
        var act = () => Customer.Create("Maria Silva", "maria@email.com", "123", "111.444.777-36");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_cnpj_with_correct_digit_count_but_wrong_check_digit()
    {
        // 11.222.333/0001-81 is a valid CNPJ; flipping only the last check digit keeps the shape
        // intact while making the mod-11 calculation fail.
        var act = () => Customer.Create("Empresa LTDA", "contato@empresa.com", "123", "11.222.333/0001-82");

        Assert.Throws<ArgumentException>(act);
    }

[Fact]
    public void Update_should_change_name_email_phone_and_document()
    {
        var customer = Customer.Create("Maria Silva", "maria@email.com", "11999990000", "52998224725");

        customer.Update("Maria Souza", "  SOUZA@EMAIL.COM  ", "11988880000", "52998224725");

        Assert.Equal("Maria Souza", customer.Name);
        Assert.Equal("souza@email.com", customer.Email);
        Assert.Equal("11988880000", customer.TelephoneNumber);
        Assert.Equal("52998224725", customer.Document);
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
