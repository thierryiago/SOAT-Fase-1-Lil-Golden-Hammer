using Oficina.Domain.Customers;
using Oficina.Domain.Vehicles;

namespace Oficina.Tests.Domain;

public sealed class VehicleTests
{
    [Fact]
    public void Create_should_reject_empty_customer_id()
    {
        var act = () => Vehicle.Create(Guid.Empty, "ABC1D23", "Honda", "Civic", 2021, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_empty_brand()
    {
        var act = () => Vehicle.Create(Guid.NewGuid(), "ABC1D23", " ", "Civic", 2021, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_empty_model()
    {
        var act = () => Vehicle.Create(Guid.NewGuid(), "ABC1D23", "Honda", " ", 2021, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_year_before_1900()
    {
        var act = () => Vehicle.Create(Guid.NewGuid(), "ABC1D23", "Honda", "Civic", 1899, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_year_too_far_in_the_future()
    {
        var invalidYear = DateTime.UtcNow.Year + 2;

        var act = () => Vehicle.Create(Guid.NewGuid(), "ABC1D23", "Honda", "Civic", invalidYear, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_accept_year_one_year_ahead()
    {
        var validYear = DateTime.UtcNow.Year + 1;

        var vehicle = Vehicle.Create(Guid.NewGuid(), "ABC1D23", "Honda", "Civic", validYear, EnumVehicleCategory.Car);

        Assert.Equal(validYear, vehicle.Year);
    }

    [Fact]
    public void Create_should_reject_invalid_plate_format()
    {
        var act = () => Vehicle.Create(Guid.NewGuid(), "1234ABC", "Honda", "Civic", 2021, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_normalize_mercosul_plate()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), " abc1d23 ", "Honda", "Civic", 2021, EnumVehicleCategory.Car);

        Assert.Equal("ABC1D23", vehicle.Plate);
    }

    [Fact]
    public void Update_should_change_plate_brand_model_year_and_category()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);

        vehicle.Update("XYZ9876", "Yamaha", "Fazer", 2022, EnumVehicleCategory.Motorcycle);

        Assert.Equal("XYZ-9876", vehicle.Plate);
        Assert.Equal("Yamaha", vehicle.Brand);
        Assert.Equal("Fazer", vehicle.Model);
        Assert.Equal(2022, vehicle.Year);
        Assert.Equal(EnumVehicleCategory.Motorcycle, vehicle.Category);
    }

    [Fact]
    public void Update_should_reject_invalid_plate()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);

        var act = () => vehicle.Update("INVALID", "Honda", "Civic", 2020, EnumVehicleCategory.Car);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Deactivate_should_set_is_active_to_false()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);

        vehicle.Deactivate();

        Assert.False(vehicle.IsActive);
    }

    [Theory]
    [InlineData("ABC1234", true)]
    [InlineData("ABC-1234", true)]
    [InlineData("ABC1D23", true)]
    [InlineData("ABCD123", false)]
    [InlineData("AB1234", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void IsValidPlate_should_validate_old_and_mercosul_formats(string plate, bool expected)
    {
        Assert.Equal(expected, Vehicle.IsValidPlate(plate));
    }
}
