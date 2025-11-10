namespace AutoPit.Core;
public static class Validation
{
    public static (bool ok, string? error) Validate(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.Vin) || car.Vin.Length < 11) return (false, "VIN is required (>= 11 chars)");
        if (string.IsNullOrWhiteSpace(car.Make)) return (false, "Make is required");
        if (string.IsNullOrWhiteSpace(car.Model)) return (false, "Model is required");
        if (car.Year is < 1980 or > 2100) return (false, "Year must be 1980-2100");
        return (true, null);
    }
    public static (bool ok, string? error) Validate(ServiceRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Vin)) return (false, "VIN is required");
        if (string.IsNullOrWhiteSpace(req.Concern)) return (false, "Concern is required");
        if (req.Priority < 1 || req.Priority > 5) return (false, "Priority must be 1..5");
        return (true, null);
    }
}
