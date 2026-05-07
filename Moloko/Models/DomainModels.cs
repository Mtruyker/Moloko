namespace Moloko.Models;

public enum UserRole
{
    Administrator,
    Manager,
    Operator,
    LabTechnician,
    Storekeeper
}

public enum BatchStatus
{
    Created,
    InAnalysis,
    Approved,
    Blocked,
    PartiallyShipped,
    FullyShipped,
    WrittenOff
}

public enum QualityConclusion
{
    Suitable,
    ConditionallySuitable,
    Blocked,
    WrittenOff
}

public enum StockOperationType
{
    Receipt,
    Transfer,
    Shipment,
    WriteOff,
    Return,
    InventoryCorrection
}

public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AnimalGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public sealed class ProductType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int ShelfLifeHours { get; set; }
}

public sealed class StorageTank
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal CapacityLiters { get; set; }
    public string Location { get; set; } = string.Empty;
}

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public sealed class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
}

public sealed class QualityNorms
{
    public decimal MinFatPercent { get; set; } = 2.8m;
    public decimal MinProteinPercent { get; set; } = 2.8m;
    public decimal MaxAcidity { get; set; } = 21m;
    public decimal MinDensity { get; set; } = 1027m;
    public decimal MaxTemperature { get; set; } = 10m;
}

public sealed class MilkYield
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime MilkedAt { get; set; } = DateTime.Now;
    public Guid AnimalGroupId { get; set; }
    public string Farm { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public decimal VolumeLiters { get; set; }
    public decimal WeightKg { get; set; }
    public decimal Temperature { get; set; }
    public Guid? BatchId { get; set; }
}

public sealed class Batch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string SourceDescription { get; set; } = string.Empty;
    public decimal VolumeLiters { get; set; }
    public decimal WeightKg { get; set; }
    public decimal RemainingLiters { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Created;
    public Guid? StorageTankId { get; set; }
    public Guid? ProductTypeId { get; set; }
    public DateTime ExpirationDate { get; set; } = DateTime.Now.AddHours(36);
    public string CreatedBy { get; set; } = string.Empty;
    public string Documents { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class BatchQualityTest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public DateTime TestDate { get; set; } = DateTime.Now;
    public decimal FatPercent { get; set; }
    public decimal ProteinPercent { get; set; }
    public decimal Acidity { get; set; }
    public decimal Density { get; set; }
    public decimal Temperature { get; set; }
    public string VisualResult { get; set; } = "без отклонений";
    public bool HasForeignImpurities { get; set; }
    public string ExpressTests { get; set; } = string.Empty;
    public QualityConclusion Conclusion { get; set; } = QualityConclusion.Suitable;
    public string TestedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

public sealed class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public StockOperationType OperationType { get; set; }
    public decimal VolumeLiters { get; set; }
    public Guid? FromTankId { get; set; }
    public Guid? ToTankId { get; set; }
    public DateTime OperationDate { get; set; } = DateTime.Now;
    public string ResponsibleUser { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShipmentNumber { get; set; } = string.Empty;
    public DateTime ShippedAt { get; set; } = DateTime.Now;
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public decimal Temperature { get; set; }
    public string BasisDocument { get; set; } = string.Empty;
    public string ResponsibleUser { get; set; } = string.Empty;
}

public sealed class ShipmentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShipmentId { get; set; }
    public Guid BatchId { get; set; }
    public decimal VolumeLiters { get; set; }
}

public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class AppData
{
    public List<UserAccount> Users { get; set; } = [];
    public List<AnimalGroup> AnimalGroups { get; set; } = [];
    public List<ProductType> ProductTypes { get; set; } = [];
    public List<StorageTank> StorageTanks { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];
    public List<MilkYield> MilkYields { get; set; } = [];
    public List<Batch> Batches { get; set; } = [];
    public List<BatchQualityTest> QualityTests { get; set; } = [];
    public List<StockMovement> StockMovements { get; set; } = [];
    public List<Shipment> Shipments { get; set; } = [];
    public List<ShipmentItem> ShipmentItems { get; set; } = [];
    public List<AuditEntry> AuditLog { get; set; } = [];
    public QualityNorms Norms { get; set; } = new();
}
