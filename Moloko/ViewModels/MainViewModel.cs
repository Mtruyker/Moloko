using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using Moloko.Infrastructure;
using Moloko.Models;
using Moloko.Services;

namespace Moloko.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IAppDataStore _store;
    private readonly UserAccount _currentUser;
    private AppData _data;
    private Batch? _selectedBatch;
    private StorageTank? _selectedTank;
    private StorageTank? _targetTank;
    private AnimalGroup? _selectedAnimalGroup;
    private ProductType? _selectedProductType;
    private Customer? _selectedCustomer;
    private Vehicle? _selectedVehicle;
    private string _statusMessage = "Система готова к работе";
    private string _reportText = string.Empty;
    private decimal _yieldVolume = 100;
    private decimal _yieldWeight = 103;
    private decimal _yieldTemperature = 6;
    private decimal _qualityFat = 3.4m;
    private decimal _qualityProtein = 3.1m;
    private decimal _qualityAcidity = 18;
    private decimal _qualityDensity = 1028;
    private decimal _qualityTemperature = 6;
    private bool _hasForeignImpurities;
    private decimal _operationVolume = 50;
    private decimal _shipmentTemperature = 6;

    public MainViewModel(IAppDataStore store, UserAccount currentUser)
    {
        _store = store;
        _currentUser = currentUser;
        _data = _store.Load();

        Users = new ObservableCollection<UserAccount>(_data.Users);
        AnimalGroups = new ObservableCollection<AnimalGroup>(_data.AnimalGroups);
        ProductTypes = new ObservableCollection<ProductType>(_data.ProductTypes);
        StorageTanks = new ObservableCollection<StorageTank>(_data.StorageTanks);
        Customers = new ObservableCollection<Customer>(_data.Customers);
        Vehicles = new ObservableCollection<Vehicle>(_data.Vehicles);
        MilkYields = new ObservableCollection<MilkYield>(_data.MilkYields);
        Batches = new ObservableCollection<Batch>(_data.Batches);
        QualityTests = new ObservableCollection<BatchQualityTest>(_data.QualityTests);
        StockMovements = new ObservableCollection<StockMovement>(_data.StockMovements);
        Shipments = new ObservableCollection<Shipment>(_data.Shipments);
        ShipmentItems = new ObservableCollection<ShipmentItem>(_data.ShipmentItems);
        AuditLog = new ObservableCollection<AuditEntry>(_data.AuditLog);

        SelectedAnimalGroup = AnimalGroups.FirstOrDefault();
        SelectedProductType = ProductTypes.FirstOrDefault();
        SelectedTank = StorageTanks.FirstOrDefault();
        TargetTank = StorageTanks.FirstOrDefault();
        SelectedCustomer = Customers.FirstOrDefault();
        SelectedVehicle = Vehicles.FirstOrDefault();
        SelectedBatch = Batches.FirstOrDefault();

        CreateBatchCommand = new RelayCommand(CreateYieldAndBatch);
        SaveQualityCommand = new RelayCommand(SaveQualityTest, () => SelectedBatch is not null);
        TransferCommand = new RelayCommand(TransferBatch, () => SelectedBatch is not null);
        WriteOffCommand = new RelayCommand(WriteOffBatch, () => SelectedBatch is not null);
        CreateShipmentCommand = new RelayCommand(CreateShipment, () => SelectedBatch is not null);
        ExportCsvCommand = new RelayCommand(ExportCsv);
        BackupCommand = new RelayCommand(CreateBackup);
        AddDirectoryCommand = new RelayCommand(AddDirectoryItems);
        FillDemoDataCommand = new RelayCommand(FillDemoData);
        GenerateReportCommand = new RelayCommand(GenerateReport);

        GenerateReport();
        RefreshDashboard();
        StatusMessage = $"Пользователь: {_currentUser.FullName} ({RussianText.Role(_currentUser.Role)}). Хранилище данных: {_store.ProviderName}";
    }

    public ObservableCollection<UserAccount> Users { get; }
    public ObservableCollection<AnimalGroup> AnimalGroups { get; }
    public ObservableCollection<ProductType> ProductTypes { get; }
    public ObservableCollection<StorageTank> StorageTanks { get; }
    public ObservableCollection<Customer> Customers { get; }
    public ObservableCollection<Vehicle> Vehicles { get; }
    public ObservableCollection<MilkYield> MilkYields { get; }
    public ObservableCollection<Batch> Batches { get; }
    public ObservableCollection<BatchQualityTest> QualityTests { get; }
    public ObservableCollection<StockMovement> StockMovements { get; }
    public ObservableCollection<Shipment> Shipments { get; }
    public ObservableCollection<ShipmentItem> ShipmentItems { get; }
    public ObservableCollection<AuditEntry> AuditLog { get; }

    public RelayCommand CreateBatchCommand { get; }
    public RelayCommand SaveQualityCommand { get; }
    public RelayCommand TransferCommand { get; }
    public RelayCommand WriteOffCommand { get; }
    public RelayCommand CreateShipmentCommand { get; }
    public RelayCommand ExportCsvCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand AddDirectoryCommand { get; }
    public RelayCommand FillDemoDataCommand { get; }
    public RelayCommand GenerateReportCommand { get; }

    public Batch? SelectedBatch
    {
        get => _selectedBatch;
        set
        {
            if (SetProperty(ref _selectedBatch, value))
            {
                SaveQualityCommand?.RaiseCanExecuteChanged();
                TransferCommand?.RaiseCanExecuteChanged();
                WriteOffCommand?.RaiseCanExecuteChanged();
                CreateShipmentCommand?.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedBatchQualityHistory));
                OnPropertyChanged(nameof(SelectedBatchMovementHistory));
                OnPropertyChanged(nameof(SelectedBatchStatusText));
            }
        }
    }

    public StorageTank? SelectedTank
    {
        get => _selectedTank;
        set => SetProperty(ref _selectedTank, value);
    }

    public StorageTank? TargetTank
    {
        get => _targetTank;
        set => SetProperty(ref _targetTank, value);
    }

    public AnimalGroup? SelectedAnimalGroup
    {
        get => _selectedAnimalGroup;
        set => SetProperty(ref _selectedAnimalGroup, value);
    }

    public ProductType? SelectedProductType
    {
        get => _selectedProductType;
        set => SetProperty(ref _selectedProductType, value);
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set => SetProperty(ref _selectedVehicle, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ReportText
    {
        get => _reportText;
        set => SetProperty(ref _reportText, value);
    }

    public decimal YieldVolume
    {
        get => _yieldVolume;
        set => SetProperty(ref _yieldVolume, value);
    }

    public decimal YieldWeight
    {
        get => _yieldWeight;
        set => SetProperty(ref _yieldWeight, value);
    }

    public decimal YieldTemperature
    {
        get => _yieldTemperature;
        set => SetProperty(ref _yieldTemperature, value);
    }

    public decimal QualityFat
    {
        get => _qualityFat;
        set => SetProperty(ref _qualityFat, value);
    }

    public decimal QualityProtein
    {
        get => _qualityProtein;
        set => SetProperty(ref _qualityProtein, value);
    }

    public decimal QualityAcidity
    {
        get => _qualityAcidity;
        set => SetProperty(ref _qualityAcidity, value);
    }

    public decimal QualityDensity
    {
        get => _qualityDensity;
        set => SetProperty(ref _qualityDensity, value);
    }

    public decimal QualityTemperature
    {
        get => _qualityTemperature;
        set => SetProperty(ref _qualityTemperature, value);
    }

    public bool HasForeignImpurities
    {
        get => _hasForeignImpurities;
        set => SetProperty(ref _hasForeignImpurities, value);
    }

    public decimal OperationVolume
    {
        get => _operationVolume;
        set => SetProperty(ref _operationVolume, value);
    }

    public decimal ShipmentTemperature
    {
        get => _shipmentTemperature;
        set => SetProperty(ref _shipmentTemperature, value);
    }

    public string SelectedBatchStatusText => SelectedBatch is null
        ? "партия не выбрана"
        : RussianText.BatchStatus(SelectedBatch.Status);

    public IEnumerable<BatchQualityTest> SelectedBatchQualityHistory =>
        SelectedBatch is null
            ? []
            : QualityTests.Where(test => test.BatchId == SelectedBatch.Id).OrderByDescending(test => test.TestDate);

    public IEnumerable<StockMovement> SelectedBatchMovementHistory =>
        SelectedBatch is null
            ? []
            : StockMovements.Where(movement => movement.BatchId == SelectedBatch.Id).OrderByDescending(movement => movement.OperationDate);

    public int TotalBatches { get; private set; }
    public decimal TotalStockLiters { get; private set; }
    public int InAnalysisCount { get; private set; }
    public int BlockedCount { get; private set; }
    public int ExpiredCount { get; private set; }
    public int TodayShipmentsCount { get; private set; }

    private void CreateYieldAndBatch()
    {
        if (YieldVolume <= 0 || YieldWeight <= 0)
        {
            ShowWarning("Объем и масса должны быть положительными.");
            return;
        }

        if (SelectedAnimalGroup is null || SelectedTank is null || SelectedProductType is null)
        {
            ShowWarning("Выберите группу животных, тип продукции и емкость хранения.");
            return;
        }

        var batch = new Batch
        {
            BatchNumber = NextBatchNumber(),
            CreatedAt = DateTime.Now,
            SourceDescription = $"{SelectedAnimalGroup.Name}, {SelectedAnimalGroup.Location}",
            VolumeLiters = YieldVolume,
            WeightKg = YieldWeight,
            RemainingLiters = YieldVolume,
            Status = BatchStatus.InAnalysis,
            StorageTankId = SelectedTank.Id,
            ProductTypeId = SelectedProductType.Id,
            ExpirationDate = DateTime.Now.AddHours(SelectedProductType.ShelfLifeHours),
            CreatedBy = _currentUser.FullName
        };

        var milkYield = new MilkYield
        {
            MilkedAt = DateTime.Now,
            AnimalGroupId = SelectedAnimalGroup.Id,
            Farm = "КФХ Жукушева К.Н.",
            Room = SelectedAnimalGroup.Location,
            OperatorName = _currentUser.FullName,
            VolumeLiters = YieldVolume,
            WeightKg = YieldWeight,
            Temperature = YieldTemperature,
            BatchId = batch.Id
        };

        Batches.Insert(0, batch);
        MilkYields.Insert(0, milkYield);
        AddMovement(batch, StockOperationType.Receipt, YieldVolume, null, SelectedTank.Id, "Регистрация надоя");
        AddAudit("Создание партии", $"{batch.BatchNumber}, {batch.VolumeLiters:N1} л");
        SelectedBatch = batch;
        SaveAll("Партия создана и поставлена на анализ.");
    }

    private void SaveQualityTest()
    {
        if (SelectedBatch is null)
        {
            return;
        }

        if (QualityFat <= 0 || QualityProtein <= 0 || QualityAcidity <= 0 || QualityDensity <= 0)
        {
            ShowWarning("Показатели качества должны быть положительными.");
            return;
        }

        var conclusion = CalculateConclusion();
        var test = new BatchQualityTest
        {
            BatchId = SelectedBatch.Id,
            TestDate = DateTime.Now,
            FatPercent = QualityFat,
            ProteinPercent = QualityProtein,
            Acidity = QualityAcidity,
            Density = QualityDensity,
            Temperature = QualityTemperature,
            HasForeignImpurities = HasForeignImpurities,
            ExpressTests = HasForeignImpurities ? "обнаружены отклонения" : "экспресс-тесты без отклонений",
            Conclusion = conclusion,
            TestedBy = _currentUser.FullName,
            Comment = BuildQualityComment(conclusion)
        };

        SelectedBatch.Status = conclusion switch
        {
            QualityConclusion.Suitable => BatchStatus.Approved,
            QualityConclusion.ConditionallySuitable => BatchStatus.Approved,
            QualityConclusion.Blocked => BatchStatus.Blocked,
            QualityConclusion.WrittenOff => BatchStatus.WrittenOff,
            _ => SelectedBatch.Status
        };

        QualityTests.Insert(0, test);
        AddAudit("Лабораторный контроль", $"{SelectedBatch.BatchNumber}: {RussianText.QualityConclusion(conclusion)}");
        SaveAll($"Результат контроля: {RussianText.QualityConclusion(conclusion)}.");
        OnPropertyChanged(nameof(SelectedBatchStatusText));
        OnPropertyChanged(nameof(SelectedBatchQualityHistory));
    }

    private void TransferBatch()
    {
        if (SelectedBatch is null || TargetTank is null)
        {
            return;
        }

        if (!ValidateVolume(OperationVolume, SelectedBatch))
        {
            return;
        }

        var fromTank = SelectedBatch.StorageTankId;
        SelectedBatch.StorageTankId = TargetTank.Id;
        AddMovement(SelectedBatch, StockOperationType.Transfer, OperationVolume, fromTank, TargetTank.Id, "Перемещение между емкостями");
        AddAudit("Перемещение партии", $"{SelectedBatch.BatchNumber}, {OperationVolume:N1} л");
        SaveAll("Перемещение зарегистрировано.");
        OnPropertyChanged(nameof(SelectedBatchMovementHistory));
    }

    private void WriteOffBatch()
    {
        if (SelectedBatch is null)
        {
            return;
        }

        if (!ValidateVolume(OperationVolume, SelectedBatch))
        {
            return;
        }

        SelectedBatch.RemainingLiters -= OperationVolume;
        SelectedBatch.Status = SelectedBatch.RemainingLiters == 0 ? BatchStatus.WrittenOff : SelectedBatch.Status;
        AddMovement(SelectedBatch, StockOperationType.WriteOff, OperationVolume, SelectedBatch.StorageTankId, null, "Списание");
        AddAudit("Списание", $"{SelectedBatch.BatchNumber}, {OperationVolume:N1} л");
        SaveAll("Списание зарегистрировано.");
    }

    private void CreateShipment()
    {
        if (SelectedBatch is null || SelectedCustomer is null || SelectedVehicle is null)
        {
            ShowWarning("Выберите партию, покупателя и транспорт.");
            return;
        }

        if (SelectedBatch.Status is BatchStatus.Blocked or BatchStatus.WrittenOff)
        {
            ShowWarning("Заблокированную или списанную партию нельзя отгрузить.");
            return;
        }

        if (SelectedBatch.Status != BatchStatus.Approved && SelectedBatch.Status != BatchStatus.PartiallyShipped)
        {
            ShowWarning("Партия должна быть допущена лабораторией перед отгрузкой.");
            return;
        }

        if (!ValidateVolume(OperationVolume, SelectedBatch))
        {
            return;
        }

        var shipment = new Shipment
        {
            ShipmentNumber = NextShipmentNumber(),
            CustomerId = SelectedCustomer.Id,
            VehicleId = SelectedVehicle.Id,
            Temperature = ShipmentTemperature,
            BasisDocument = $"Накладная {DateTime.Now:dd.MM.yyyy}",
            ResponsibleUser = _currentUser.FullName
        };
        var item = new ShipmentItem
        {
            ShipmentId = shipment.Id,
            BatchId = SelectedBatch.Id,
            VolumeLiters = OperationVolume
        };

        SelectedBatch.RemainingLiters -= OperationVolume;
        SelectedBatch.Status = SelectedBatch.RemainingLiters == 0
            ? BatchStatus.FullyShipped
            : BatchStatus.PartiallyShipped;

        Shipments.Insert(0, shipment);
        ShipmentItems.Insert(0, item);
        AddMovement(SelectedBatch, StockOperationType.Shipment, OperationVolume, SelectedBatch.StorageTankId, null, shipment.ShipmentNumber);
        AddAudit("Отгрузка", $"{shipment.ShipmentNumber}, {SelectedBatch.BatchNumber}, {OperationVolume:N1} л");
        SaveAll("Отгрузка оформлена. Данные готовы для переноса в ФГИС Меркурий.");
    }

    private void AddDirectoryItems()
    {
        var group = new AnimalGroup { Name = $"Группа животных {AnimalGroups.Count + 1}", Location = "Ферма" };
        var tank = new StorageTank { Name = $"Емкость {StorageTanks.Count + 1}", CapacityLiters = 500, Location = "Склад" };
        var customer = new Customer { Name = $"Покупатель {Customers.Count + 1}", Inn = "", Address = "Саратовская область" };
        var vehicle = new Vehicle { Name = $"Транспорт {Vehicles.Count + 1}", PlateNumber = "", Driver = "" };

        AnimalGroups.Add(group);
        StorageTanks.Add(tank);
        Customers.Add(customer);
        Vehicles.Add(vehicle);
        AddAudit("Справочники", "Добавлены типовые записи для последующего редактирования");
        SaveAll("Справочники пополнены типовыми записями.");
    }

    private void FillDemoData()
    {
        var result = MessageBox.Show(
            "Текущие записи будут заменены демонстрационными данными. Продолжить?",
            "Заполнение базы",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var demo = SeedData.Create();
        ReplaceCollection(Users, demo.Users);
        ReplaceCollection(AnimalGroups, demo.AnimalGroups);
        ReplaceCollection(ProductTypes, demo.ProductTypes);
        ReplaceCollection(StorageTanks, demo.StorageTanks);
        ReplaceCollection(Customers, demo.Customers);
        ReplaceCollection(Vehicles, demo.Vehicles);
        ReplaceCollection(MilkYields, demo.MilkYields);
        ReplaceCollection(Batches, demo.Batches);
        ReplaceCollection(QualityTests, demo.QualityTests);
        ReplaceCollection(StockMovements, demo.StockMovements);
        ReplaceCollection(Shipments, demo.Shipments);
        ReplaceCollection(ShipmentItems, demo.ShipmentItems);
        ReplaceCollection(AuditLog, demo.AuditLog);

        _data = demo;
        SelectedAnimalGroup = AnimalGroups.FirstOrDefault();
        SelectedProductType = ProductTypes.FirstOrDefault();
        SelectedTank = StorageTanks.FirstOrDefault();
        TargetTank = StorageTanks.FirstOrDefault();
        SelectedCustomer = Customers.FirstOrDefault();
        SelectedVehicle = Vehicles.FirstOrDefault();
        SelectedBatch = Batches.FirstOrDefault();

        SaveAll("База заполнена демонстрационными данными.");
    }

    private void ExportCsv()
    {
        Directory.CreateDirectory(_store.ExportDirectory);
        var path = Path.Combine(_store.ExportDirectory, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        var csv = new StringBuilder();
        csv.AppendLine("Номер партии;Дата;Статус;Объем;Остаток;Срок годности;Источник");
        foreach (var batch in Batches.OrderByDescending(batch => batch.CreatedAt))
        {
            csv.AppendLine($"{batch.BatchNumber};{batch.CreatedAt:dd.MM.yyyy HH:mm};{RussianText.BatchStatus(batch.Status)};{batch.VolumeLiters};{batch.RemainingLiters};{batch.ExpirationDate:dd.MM.yyyy HH:mm};{batch.SourceDescription}");
        }

        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        AddAudit("Экспорт CSV", path);
        SaveAll($"CSV-отчет сохранен: {path}");
    }

    private void CreateBackup()
    {
        var path = _store.CreateBackup(SyncData());
        AddAudit("Резервное копирование", path);
        SaveAll($"Резервная копия создана: {path}");
    }

    private void GenerateReport()
    {
        var today = DateTime.Today;
        var shippedToday = Shipments.Count(shipment => shipment.ShippedAt.Date == today);
        var blocked = Batches.Where(batch => batch.Status == BatchStatus.Blocked).ToList();
        var expired = Batches.Where(batch => batch.ExpirationDate < DateTime.Now && batch.RemainingLiters > 0).ToList();

        ReportText =
            $"Сводный отчет на {DateTime.Now:dd.MM.yyyy HH:mm}\n\n" +
            $"Всего партий: {Batches.Count}\n" +
            $"Остаток молока: {Batches.Sum(batch => batch.RemainingLiters):N1} л\n" +
            $"Партии на анализе: {Batches.Count(batch => batch.Status == BatchStatus.InAnalysis)}\n" +
            $"Заблокированные партии: {blocked.Count}\n" +
            $"Просроченные партии: {expired.Count}\n" +
            $"Отгрузки за день: {shippedToday}\n\n" +
            "Данные для ФГИС Меркурий:\n" +
            string.Join("\n", Batches.Take(8).Select(batch => $"- {batch.BatchNumber}: {batch.RemainingLiters:N1} л, дата производства {batch.CreatedAt:dd.MM.yyyy}, статус {RussianText.BatchStatus(batch.Status)}"));
    }

    private QualityConclusion CalculateConclusion()
    {
        var norms = _data.Norms;
        var hardViolation = HasForeignImpurities ||
            QualityTemperature > norms.MaxTemperature + 4 ||
            QualityAcidity > norms.MaxAcidity + 4;
        var softViolation =
            QualityFat < norms.MinFatPercent ||
            QualityProtein < norms.MinProteinPercent ||
            QualityAcidity > norms.MaxAcidity ||
            QualityDensity < norms.MinDensity ||
            QualityTemperature > norms.MaxTemperature;

        if (hardViolation)
        {
            return QualityConclusion.Blocked;
        }

        return softViolation ? QualityConclusion.ConditionallySuitable : QualityConclusion.Suitable;
    }

    private string BuildQualityComment(QualityConclusion conclusion)
    {
        if (conclusion == QualityConclusion.Suitable)
        {
            return "Показатели соответствуют заданным нормам.";
        }

        var issues = new List<string>();
        var norms = _data.Norms;
        if (QualityFat < norms.MinFatPercent) issues.Add("низкая жирность");
        if (QualityProtein < norms.MinProteinPercent) issues.Add("низкий белок");
        if (QualityAcidity > norms.MaxAcidity) issues.Add("повышенная кислотность");
        if (QualityDensity < norms.MinDensity) issues.Add("низкая плотность");
        if (QualityTemperature > norms.MaxTemperature) issues.Add("повышенная температура");
        if (HasForeignImpurities) issues.Add("посторонние примеси");

        return issues.Count == 0 ? "Заключение сформировано автоматически." : string.Join(", ", issues);
    }

    private bool ValidateVolume(decimal volume, Batch batch)
    {
        if (volume <= 0)
        {
            ShowWarning("Объем операции должен быть положительным.");
            return false;
        }

        if (volume > batch.RemainingLiters)
        {
            ShowWarning("Нельзя выполнить операцию на объем больше остатка партии.");
            return false;
        }

        return true;
    }

    private void AddMovement(Batch batch, StockOperationType operation, decimal volume, Guid? fromTank, Guid? toTank, string reason)
    {
        StockMovements.Insert(0, new StockMovement
        {
            BatchId = batch.Id,
            OperationType = operation,
            VolumeLiters = volume,
            FromTankId = fromTank,
            ToTankId = toTank,
            ResponsibleUser = _currentUser.FullName,
            Reason = reason
        });
    }

    private void AddAudit(string action, string details)
    {
        AuditLog.Insert(0, new AuditEntry
        {
            UserName = _currentUser.Login,
            Action = action,
            Details = details
        });
    }

    private void SaveAll(string message)
    {
        _store.Save(SyncData());
        StatusMessage = message;
        RefreshDashboard();
        GenerateReport();
        OnPropertyChanged(nameof(SelectedBatchQualityHistory));
        OnPropertyChanged(nameof(SelectedBatchMovementHistory));
        OnPropertyChanged(nameof(SelectedBatchStatusText));
    }

    private AppData SyncData()
    {
        _data = new AppData
        {
            Users = Users.ToList(),
            AnimalGroups = AnimalGroups.ToList(),
            ProductTypes = ProductTypes.ToList(),
            StorageTanks = StorageTanks.ToList(),
            Customers = Customers.ToList(),
            Vehicles = Vehicles.ToList(),
            MilkYields = MilkYields.ToList(),
            Batches = Batches.ToList(),
            QualityTests = QualityTests.ToList(),
            StockMovements = StockMovements.ToList(),
            Shipments = Shipments.ToList(),
            ShipmentItems = ShipmentItems.ToList(),
            AuditLog = AuditLog.ToList(),
            Norms = _data.Norms
        };

        return _data;
    }

    private void RefreshDashboard()
    {
        TotalBatches = Batches.Count;
        TotalStockLiters = Batches.Sum(batch => batch.RemainingLiters);
        InAnalysisCount = Batches.Count(batch => batch.Status == BatchStatus.InAnalysis);
        BlockedCount = Batches.Count(batch => batch.Status == BatchStatus.Blocked);
        ExpiredCount = Batches.Count(batch => batch.ExpirationDate < DateTime.Now && batch.RemainingLiters > 0);
        TodayShipmentsCount = Shipments.Count(shipment => shipment.ShippedAt.Date == DateTime.Today);

        OnPropertyChanged(nameof(TotalBatches));
        OnPropertyChanged(nameof(TotalStockLiters));
        OnPropertyChanged(nameof(InAnalysisCount));
        OnPropertyChanged(nameof(BlockedCount));
        OnPropertyChanged(nameof(ExpiredCount));
        OnPropertyChanged(nameof(TodayShipmentsCount));
    }

    private string NextBatchNumber()
    {
        return $"М-{DateTime.Now:yyyyMMdd}-{Batches.Count(batch => batch.CreatedAt.Date == DateTime.Today) + 1:000}";
    }

    private string NextShipmentNumber()
    {
        return $"РН-{DateTime.Now:yyyyMMdd}-{Shipments.Count(shipment => shipment.ShippedAt.Date == DateTime.Today) + 1:000}";
    }

    private void ShowWarning(string message)
    {
        StatusMessage = message;
        MessageBox.Show(message, "Контроль данных", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
