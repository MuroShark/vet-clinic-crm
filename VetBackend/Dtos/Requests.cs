namespace VetClinicBackend.Dtos
{
    /// <summary>
    /// Содержит DTO (Data Transfer Objects) для тел запросов от фронтенда.
    /// Имена полей совпадают с JSON-payload; привязка параметров регистронезависима.
    /// </summary>

    public record BranchReq(string? Id, string Name, string Address, string? Phone, bool IsActive);

    public record EmployeeReq(string? Id, string Name, string PositionId, string? Phone, string? Email,
        decimal KPIRate, string[] BranchIds, string Status);

    public record UserReq(string? Id, string Username, string? EmployeeId, string[] Roles, string Status);

    public record ClientReq(string? Id, string Name, string Phone, string? Email, bool ConsentSigned);

    public record PatientReq(string? Id, string ClientId, string Name, string Species, string? Breed,
        string Gender, decimal Weight, string? Color, string BirthDate, string? ChipNumber);

    public record DiagnosisReq(string? Id, string Code, string Name, string Category);

    public record PriceReq(string ServiceId, decimal Price);

    public record BatchReq(string? Id, string MaterialId, string? Supplier, decimal PurchasePrice,
        decimal ClientPrice, int TotalQuantity, int RemainingQuantity, string ExpiryDate, string ReceivedAt, string BranchId);

    public record PaymentReq(string AppointmentId, decimal Amount, string Method, string? Notes);

    public record AuditReq(string Username, string Action, string Details);

    public record ApptServiceReq(string ServiceId, int Quantity, decimal PriceSnapshot);
    public record ApptMaterialReq(string MaterialId, string BatchId, int Quantity, decimal UnitCostSnapshot, decimal ClientPriceSnapshot);
    public record ApptDiagnosisReq(string DiagnosisId, bool IsPreliminary, string DoctorComment);

    public record AppointmentReq(string? Id, string ClientId, string PatientId, string VetId, string BranchId,
        string AppointmentDate, string TimeSlot, string Status, string? Notes,
        ApptServiceReq[] Services, ApptMaterialReq[] Materials, ApptDiagnosisReq[] Diagnoses, decimal TotalAmount);
}
