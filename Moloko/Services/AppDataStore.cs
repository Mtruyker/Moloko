using System.IO;
using System.Text.Json;
using Moloko.Models;

namespace Moloko.Services;

public sealed class AppDataStore : IAppDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string DataDirectory { get; }
    public string DataFilePath { get; }
    public string BackupDirectory { get; }
    public string ExportDirectory { get; }
    public string ProviderName => LastProviderError is null
        ? "локальный JSON"
        : $"локальный JSON, PostgreSQL недоступен: {LastProviderError}";

    public string? LastProviderError { get; set; }

    public AppDataStore()
    {
        DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Moloko");
        DataFilePath = Path.Combine(DataDirectory, "moloko-data.json");
        BackupDirectory = Path.Combine(DataDirectory, "Backups");
        ExportDirectory = Path.Combine(DataDirectory, "Exports");
    }

    public AppData Load()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(ExportDirectory);

        if (!File.Exists(DataFilePath))
        {
            var seed = SeedData.Create();
            Save(seed);
            return seed;
        }

        var json = File.ReadAllText(DataFilePath);
        return JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? SeedData.Create();
    }

    public void Save(AppData data)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(DataFilePath, json);
    }

    public string CreateBackup(AppData data)
    {
        Directory.CreateDirectory(BackupDirectory);
        Save(data);

        var backupPath = Path.Combine(BackupDirectory, $"moloko-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.Copy(DataFilePath, backupPath, true);
        return backupPath;
    }
}

public static class SeedData
{
    public static AppData Create()
    {
        var today = DateTime.Today;
        var users = new List<UserAccount>
        {
            new() { Login = "admin", PasswordHash = PasswordService.HashPassword("admin123"), FullName = "Жукушев Куанышкали Насимуллович", Role = UserRole.Administrator },
            new() { Login = "director", PasswordHash = PasswordService.HashPassword("director123"), FullName = "Жукушева Айгуль Кайратовна", Role = UserRole.Manager },
            new() { Login = "operator", PasswordHash = PasswordService.HashPassword("operator123"), FullName = "Сагинбаев Ерлан Муратович", Role = UserRole.Operator },
            new() { Login = "lab", PasswordHash = PasswordService.HashPassword("lab123"), FullName = "Петрова Наталья Сергеевна", Role = UserRole.LabTechnician }
        };

        var animalGroups = new List<AnimalGroup>
        {
            new() { Name = "Дойное стадо N1", Location = "Ферма, помещение 1" },
            new() { Name = "Дойное стадо N2", Location = "Ферма, помещение 2" },
            new() { Name = "Группа первотелок", Location = "Ферма, помещение 3" },
            new() { Name = "Вечерняя группа", Location = "Летний двор" }
        };

        var productTypes = new List<ProductType>
        {
            new() { Name = "Молоко сырое коровье", ShelfLifeHours = 36 },
            new() { Name = "Молоко охлажденное", ShelfLifeHours = 48 },
            new() { Name = "Сливки фермерские", ShelfLifeHours = 72 },
            new() { Name = "Творог фермерский", ShelfLifeHours = 96 }
        };

        var storageTanks = new List<StorageTank>
        {
            new() { Name = "Охладитель ОМ-1000", CapacityLiters = 1000, Location = "Молочный блок" },
            new() { Name = "Танк N2", CapacityLiters = 800, Location = "Молочный блок" },
            new() { Name = "Емкость приемки N1", CapacityLiters = 500, Location = "Приемка" },
            new() { Name = "Емкость приемки N2", CapacityLiters = 500, Location = "Приемка" },
            new() { Name = "Складская емкость N1", CapacityLiters = 1200, Location = "Холодильная камера" },
            new() { Name = "Складская емкость N2", CapacityLiters = 1200, Location = "Холодильная камера" }
        };

        var customers = new List<Customer>
        {
            new() { Name = "ООО Саратов-Молоко", Inn = "6453001142", Address = "г. Саратов, ул. Производственная, 12" },
            new() { Name = "ИП Абдуллин Р.М.", Inn = "645812345678", Address = "г. Новоузенск, ул. Рабочая, 8" },
            new() { Name = "СПК Рассвет", Inn = "6422004481", Address = "Новоузенский район, с. Куриловка" },
            new() { Name = "Магазин Фермерский двор", Inn = "6459007712", Address = "г. Саратов, ул. Астраханская, 45" },
            new() { Name = "ИП Мартынова Е.В.", Inn = "645701234567", Address = "г. Энгельс, ул. Полиграфическая, 3" },
            new() { Name = "ООО Продукты Поволжья", Inn = "6452110099", Address = "г. Саратов, ул. Складская, 19" },
            new() { Name = "Кафе Уют", Inn = "6422011122", Address = "г. Новоузенск, ул. Советская, 21" },
            new() { Name = "ИП Баймухаметов Т.А.", Inn = "645698765432", Address = "Саратовская область, п. Степной" }
        };

        var vehicles = new List<Vehicle>
        {
            new() { Name = "Молоковоз ГАЗель", PlateNumber = "А214КН64", Driver = "Сагинбаев Е.М." },
            new() { Name = "Рефрижератор Hyundai", PlateNumber = "М508ОК64", Driver = "Иванов А.П." },
            new() { Name = "Фургон Lada Largus", PlateNumber = "Т331УС64", Driver = "Жукушев К.Н." },
            new() { Name = "Молоковоз КамАЗ", PlateNumber = "Р770МР64", Driver = "Абишев Д.С." },
            new() { Name = "Прицеп-цистерна", PlateNumber = "В119РА64", Driver = "Сагинбаев Е.М." }
        };

        var batches = new List<Batch>
        {
            CreateBatch("001", today.AddHours(6).AddMinutes(30), "Утренний надой, дойное стадо N1", 420, 433, 300, BatchStatus.PartiallyShipped, storageTanks[0], productTypes[0], users[2]),
            CreateBatch("002", today.AddHours(7).AddMinutes(10), "Утренний надой, дойное стадо N2", 380, 391, 380, BatchStatus.Approved, storageTanks[1], productTypes[0], users[2]),
            CreateBatch("003", today.AddHours(8), "Группа первотелок", 210, 216, 210, BatchStatus.InAnalysis, storageTanks[2], productTypes[0], users[2]),
            CreateBatch("004", today.AddHours(18).AddMinutes(20), "Вечерний надой, дойное стадо N1", 350, 361, 350, BatchStatus.InAnalysis, storageTanks[3], productTypes[1], users[2]),
            CreateBatch("005", today.AddDays(-1).AddHours(6).AddMinutes(40), "Утренний надой, вечерняя группа", 290, 299, 0, BatchStatus.FullyShipped, storageTanks[4], productTypes[0], users[2]),
            CreateBatch("006", today.AddDays(-1).AddHours(18), "Вечерний надой, дойное стадо N2", 330, 340, 330, BatchStatus.Approved, storageTanks[5], productTypes[1], users[2]),
            CreateBatch("007", today.AddDays(-2).AddHours(7), "Утренний надой, дойное стадо N1", 260, 268, 260, BatchStatus.Blocked, storageTanks[0], productTypes[0], users[2]),
            CreateBatch("008", today.AddDays(-2).AddHours(18), "Сливки после сепарации", 80, 82, 80, BatchStatus.Approved, storageTanks[1], productTypes[2], users[2]),
            CreateBatch("009", today.AddDays(-3).AddHours(9), "Творог из партии М-009", 55, 55, 30, BatchStatus.PartiallyShipped, storageTanks[4], productTypes[3], users[2]),
            CreateBatch("010", today.AddDays(-4).AddHours(7), "Контрольная партия после инвентаризации", 120, 124, 0, BatchStatus.WrittenOff, storageTanks[5], productTypes[0], users[2])
        };

        var milkYields = batches
            .Where(batch => batch.ProductTypeId == productTypes[0].Id || batch.ProductTypeId == productTypes[1].Id)
            .Select(batch => new MilkYield
            {
                MilkedAt = batch.CreatedAt,
                AnimalGroupId = animalGroups[Math.Abs(batch.BatchNumber.GetHashCode()) % animalGroups.Count].Id,
                Farm = "КФХ Жукушева К.Н.",
                Room = batch.SourceDescription,
                OperatorName = users[2].FullName,
                VolumeLiters = batch.VolumeLiters,
                WeightKg = batch.WeightKg,
                Temperature = batch.Status == BatchStatus.Blocked ? 15 : 6,
                BatchId = batch.Id
            })
            .ToList();

        var tests = new List<BatchQualityTest>
        {
            CreateTest(batches[0], 3.5m, 3.1m, 18, 1029, 6, QualityConclusion.Suitable, users[3], "Показатели соответствуют нормам."),
            CreateTest(batches[1], 3.3m, 3.0m, 19, 1028, 7, QualityConclusion.Suitable, users[3], "Допущена к реализации."),
            CreateTest(batches[4], 3.4m, 3.1m, 18, 1028, 6, QualityConclusion.Suitable, users[3], "Партия реализована."),
            CreateTest(batches[5], 3.2m, 2.9m, 20, 1028, 7, QualityConclusion.ConditionallySuitable, users[3], "Повторный контроль перед отгрузкой."),
            CreateTest(batches[6], 2.6m, 2.7m, 27, 1024, 15, QualityConclusion.Blocked, users[3], "Повышенная кислотность и температура."),
            CreateTest(batches[7], 18.5m, 3.2m, 17, 1030, 5, QualityConclusion.Suitable, users[3], "Сливки соответствуют нормам."),
            CreateTest(batches[8], 9.0m, 14.5m, 19, 1032, 4, QualityConclusion.ConditionallySuitable, users[3], "Контроль органолептики без замечаний."),
            CreateTest(batches[9], 3.1m, 2.9m, 22, 1026, 11, QualityConclusion.WrittenOff, users[3], "Списана по сроку годности.")
        };

        var shipments = new List<Shipment>
        {
            CreateShipment("001", today.AddHours(11), customers[0], vehicles[0], 6, users[2]),
            CreateShipment("002", today.AddDays(-1).AddHours(12), customers[1], vehicles[1], 5, users[2]),
            CreateShipment("003", today.AddDays(-3).AddHours(15), customers[3], vehicles[2], 4, users[2])
        };

        var shipmentItems = new List<ShipmentItem>
        {
            new() { ShipmentId = shipments[0].Id, BatchId = batches[0].Id, VolumeLiters = 120 },
            new() { ShipmentId = shipments[1].Id, BatchId = batches[4].Id, VolumeLiters = 290 },
            new() { ShipmentId = shipments[2].Id, BatchId = batches[8].Id, VolumeLiters = 25 }
        };

        var movements = batches.Select(batch => new StockMovement
        {
            BatchId = batch.Id,
            OperationType = StockOperationType.Receipt,
            VolumeLiters = batch.VolumeLiters,
            ToTankId = batch.StorageTankId,
            OperationDate = batch.CreatedAt,
            ResponsibleUser = users[2].FullName,
            Reason = "Регистрация поступления"
        }).ToList();

        movements.AddRange(shipmentItems.Select(item => new StockMovement
        {
            BatchId = item.BatchId,
            OperationType = StockOperationType.Shipment,
            VolumeLiters = item.VolumeLiters,
            OperationDate = shipments.First(shipment => shipment.Id == item.ShipmentId).ShippedAt,
            ResponsibleUser = users[2].FullName,
            Reason = shipments.First(shipment => shipment.Id == item.ShipmentId).ShipmentNumber
        }));

        movements.Add(new StockMovement
        {
            BatchId = batches[9].Id,
            OperationType = StockOperationType.WriteOff,
            VolumeLiters = 120,
            FromTankId = batches[9].StorageTankId,
            OperationDate = today.AddDays(-2).AddHours(10),
            ResponsibleUser = users[1].FullName,
            Reason = "Списание по сроку годности"
        });

        return new AppData
        {
            Users = users,
            AnimalGroups = animalGroups,
            ProductTypes = productTypes,
            StorageTanks = storageTanks,
            Customers = customers,
            Vehicles = vehicles,
            MilkYields = milkYields,
            Batches = batches,
            QualityTests = tests,
            StockMovements = movements.OrderByDescending(movement => movement.OperationDate).ToList(),
            Shipments = shipments,
            ShipmentItems = shipmentItems,
            AuditLog =
            [
                new AuditEntry
                {
                    UserName = "system",
                    Action = "Создание демо-данных",
                    Details = "4 пользователя, 4 роли и около 30 справочных записей"
                }
            ]
        };
    }

    private static Batch CreateBatch(
        string number,
        DateTime createdAt,
        string source,
        decimal volume,
        decimal weight,
        decimal remaining,
        BatchStatus status,
        StorageTank tank,
        ProductType product,
        UserAccount user)
    {
        return new Batch
        {
            BatchNumber = $"М-{createdAt:yyyyMMdd}-{number}",
            CreatedAt = createdAt,
            SourceDescription = source,
            VolumeLiters = volume,
            WeightKg = weight,
            RemainingLiters = remaining,
            Status = status,
            StorageTankId = tank.Id,
            ProductTypeId = product.Id,
            ExpirationDate = createdAt.AddHours(product.ShelfLifeHours),
            CreatedBy = user.FullName
        };
    }

    private static BatchQualityTest CreateTest(
        Batch batch,
        decimal fat,
        decimal protein,
        decimal acidity,
        decimal density,
        decimal temperature,
        QualityConclusion conclusion,
        UserAccount labUser,
        string comment)
    {
        return new BatchQualityTest
        {
            BatchId = batch.Id,
            TestDate = batch.CreatedAt.AddHours(1),
            FatPercent = fat,
            ProteinPercent = protein,
            Acidity = acidity,
            Density = density,
            Temperature = temperature,
            VisualResult = conclusion == QualityConclusion.Blocked ? "есть отклонения" : "без отклонений",
            HasForeignImpurities = false,
            ExpressTests = conclusion == QualityConclusion.Blocked ? "требуется блокировка" : "без отклонений",
            Conclusion = conclusion,
            TestedBy = labUser.FullName,
            Comment = comment
        };
    }

    private static Shipment CreateShipment(
        string number,
        DateTime shippedAt,
        Customer customer,
        Vehicle vehicle,
        decimal temperature,
        UserAccount user)
    {
        return new Shipment
        {
            ShipmentNumber = $"РН-{shippedAt:yyyyMMdd}-{number}",
            ShippedAt = shippedAt,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            Temperature = temperature,
            BasisDocument = $"Накладная от {shippedAt:dd.MM.yyyy}",
            ResponsibleUser = user.FullName
        };
    }
}
