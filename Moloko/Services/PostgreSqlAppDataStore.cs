using System.IO;
using System.Text.Json;
using Moloko.Models;
using Npgsql;
using NpgsqlTypes;

namespace Moloko.Services;

public sealed class PostgreSqlAppDataStore : IAppDataStore
{
    private readonly string _connectionString;
    private readonly AppDataStore _localFiles = new();

    public PostgreSqlAppDataStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string DataDirectory => _localFiles.DataDirectory;
    public string BackupDirectory => _localFiles.BackupDirectory;
    public string ExportDirectory => _localFiles.ExportDirectory;
    public string ProviderName => "PostgreSQL";

    public void EnsureReady()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Database", "postgresql_schema.sql"));
        command.ExecuteNonQuery();
    }

    public AppData Load()
    {
        EnsureReady();

        using var connection = OpenConnection();
        var data = new AppData
        {
            Users = LoadUsers(connection),
            AnimalGroups = LoadAnimalGroups(connection),
            ProductTypes = LoadProductTypes(connection),
            StorageTanks = LoadStorageTanks(connection),
            Customers = LoadCustomers(connection),
            Vehicles = LoadVehicles(connection),
            Batches = LoadBatches(connection),
            MilkYields = LoadMilkYields(connection),
            QualityTests = LoadQualityTests(connection),
            StockMovements = LoadStockMovements(connection),
            Shipments = LoadShipments(connection),
            ShipmentItems = LoadShipmentItems(connection),
            AuditLog = LoadAudit(connection),
            Norms = LoadNorms(connection)
        };

        if (IsEmpty(data))
        {
            data = SeedData.Create();
            Save(data);
        }

        return data;
    }

    public void Save(AppData data)
    {
        EnsureReady();

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            ClearData(connection, transaction);
            SaveDirectories(connection, transaction, data);
            SaveOperationalData(connection, transaction, data);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public string CreateBackup(AppData data)
    {
        Directory.CreateDirectory(BackupDirectory);
        Save(data);

        var backupPath = Path.Combine(BackupDirectory, $"postgres-export-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(backupPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        return backupPath;
    }

    private NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static bool IsEmpty(AppData data)
    {
        return data.Users.Count == 0 &&
            data.AnimalGroups.Count == 0 &&
            data.ProductTypes.Count == 0 &&
            data.StorageTanks.Count == 0 &&
            data.Batches.Count == 0;
    }

    private static void ClearData(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        Execute(connection, transaction, """
            delete from audit_log;
            delete from shipment_items;
            delete from shipments;
            delete from stock_movements;
            delete from batch_quality_tests;
            update milk_yields set batch_id = null;
            delete from milk_yields;
            delete from batches;
            delete from quality_norms;
            delete from vehicles;
            delete from customers;
            delete from storage_tanks;
            delete from product_types;
            delete from animal_groups;
            delete from users;
            """);
    }

    private static void SaveDirectories(NpgsqlConnection connection, NpgsqlTransaction transaction, AppData data)
    {
        var roles = new (string Id, string Name)[]
        {
            ("Administrator", "Администратор"),
            ("Manager", "Глава КФХ"),
            ("Operator", "Оператор учета"),
            ("LabTechnician", "Лаборант")
        };

        foreach (var role in roles)
        {
            Execute(connection, transaction, """
                insert into roles (id, name)
                values (@id, @name)
                on conflict (id) do update set name = excluded.name
                """,
                ("id", RoleId(role.Id)),
                ("name", role.Name));
        }

        foreach (var user in data.Users)
        {
            Execute(connection, transaction, """
                insert into users (id, login, password_hash, full_name, role, role_id, is_active)
                values (@id, @login, @password_hash, @full_name, @role, @role_id, @is_active)
                """,
                ("id", user.Id),
                ("login", user.Login),
                ("password_hash", user.PasswordHash),
                ("full_name", user.FullName),
                ("role", user.Role.ToString()),
                ("role_id", RoleId(user.Role.ToString())),
                ("is_active", user.IsActive));
        }

        foreach (var group in data.AnimalGroups)
        {
            Execute(connection, transaction, """
                insert into animal_groups (id, name, location)
                values (@id, @name, @location)
                """,
                ("id", group.Id), ("name", group.Name), ("location", group.Location));
        }

        foreach (var product in data.ProductTypes)
        {
            Execute(connection, transaction, """
                insert into product_types (id, name, shelf_life_hours)
                values (@id, @name, @shelf_life_hours)
                """,
                ("id", product.Id), ("name", product.Name), ("shelf_life_hours", product.ShelfLifeHours));
        }

        foreach (var tank in data.StorageTanks)
        {
            Execute(connection, transaction, """
                insert into storage_tanks (id, name, capacity_liters, location)
                values (@id, @name, @capacity_liters, @location)
                """,
                ("id", tank.Id), ("name", tank.Name), ("capacity_liters", tank.CapacityLiters), ("location", tank.Location));
        }

        foreach (var customer in data.Customers)
        {
            Execute(connection, transaction, """
                insert into customers (id, name, inn, address)
                values (@id, @name, @inn, @address)
                """,
                ("id", customer.Id), ("name", customer.Name), ("inn", customer.Inn), ("address", customer.Address));
        }

        foreach (var vehicle in data.Vehicles)
        {
            Execute(connection, transaction, """
                insert into vehicles (id, name, plate_number, driver)
                values (@id, @name, @plate_number, @driver)
                """,
                ("id", vehicle.Id), ("name", vehicle.Name), ("plate_number", vehicle.PlateNumber), ("driver", vehicle.Driver));
        }

        Execute(connection, transaction, """
            insert into quality_norms (id, min_fat_percent, min_protein_percent, max_acidity, min_density, max_temperature)
            values (@id, @min_fat_percent, @min_protein_percent, @max_acidity, @min_density, @max_temperature)
            """,
            ("id", Guid.NewGuid()),
            ("min_fat_percent", data.Norms.MinFatPercent),
            ("min_protein_percent", data.Norms.MinProteinPercent),
            ("max_acidity", data.Norms.MaxAcidity),
            ("min_density", data.Norms.MinDensity),
            ("max_temperature", data.Norms.MaxTemperature));
    }

    private static void SaveOperationalData(NpgsqlConnection connection, NpgsqlTransaction transaction, AppData data)
    {
        foreach (var batch in data.Batches)
        {
            Execute(connection, transaction, """
                insert into batches (
                    id, batch_number, created_at, source_description, volume_liters, weight_kg,
                    remaining_liters, status, storage_tank_id, product_type_id, expiration_date,
                    created_by, documents, notes
                )
                values (
                    @id, @batch_number, @created_at, @source_description, @volume_liters, @weight_kg,
                    @remaining_liters, @status, @storage_tank_id, @product_type_id, @expiration_date,
                    @created_by, @documents, @notes
                )
                """,
                ("id", batch.Id),
                ("batch_number", batch.BatchNumber),
                ("created_at", batch.CreatedAt),
                ("source_description", batch.SourceDescription),
                ("volume_liters", batch.VolumeLiters),
                ("weight_kg", batch.WeightKg),
                ("remaining_liters", batch.RemainingLiters),
                ("status", batch.Status.ToString()),
                ("storage_tank_id", batch.StorageTankId),
                ("product_type_id", batch.ProductTypeId),
                ("expiration_date", batch.ExpirationDate),
                ("created_by", batch.CreatedBy),
                ("documents", batch.Documents),
                ("notes", batch.Notes));
        }

        foreach (var milkYield in data.MilkYields)
        {
            Execute(connection, transaction, """
                insert into milk_yields (
                    id, milked_at, animal_group_id, farm, room, operator_name,
                    volume_liters, weight_kg, temperature, batch_id
                )
                values (
                    @id, @milked_at, @animal_group_id, @farm, @room, @operator_name,
                    @volume_liters, @weight_kg, @temperature, @batch_id
                )
                """,
                ("id", milkYield.Id),
                ("milked_at", milkYield.MilkedAt),
                ("animal_group_id", milkYield.AnimalGroupId),
                ("farm", milkYield.Farm),
                ("room", milkYield.Room),
                ("operator_name", milkYield.OperatorName),
                ("volume_liters", milkYield.VolumeLiters),
                ("weight_kg", milkYield.WeightKg),
                ("temperature", milkYield.Temperature),
                ("batch_id", milkYield.BatchId));
        }

        foreach (var test in data.QualityTests)
        {
            Execute(connection, transaction, """
                insert into batch_quality_tests (
                    id, batch_id, test_date, fat_percent, protein_percent, acidity, density,
                    temperature, visual_result, has_foreign_impurities, express_tests,
                    conclusion, tested_by, comment
                )
                values (
                    @id, @batch_id, @test_date, @fat_percent, @protein_percent, @acidity, @density,
                    @temperature, @visual_result, @has_foreign_impurities, @express_tests,
                    @conclusion, @tested_by, @comment
                )
                """,
                ("id", test.Id),
                ("batch_id", test.BatchId),
                ("test_date", test.TestDate),
                ("fat_percent", test.FatPercent),
                ("protein_percent", test.ProteinPercent),
                ("acidity", test.Acidity),
                ("density", test.Density),
                ("temperature", test.Temperature),
                ("visual_result", test.VisualResult),
                ("has_foreign_impurities", test.HasForeignImpurities),
                ("express_tests", test.ExpressTests),
                ("conclusion", test.Conclusion.ToString()),
                ("tested_by", test.TestedBy),
                ("comment", test.Comment));
        }

        foreach (var movement in data.StockMovements)
        {
            Execute(connection, transaction, """
                insert into stock_movements (
                    id, batch_id, operation_type, volume_liters, from_tank_id,
                    to_tank_id, operation_date, responsible_user, reason
                )
                values (
                    @id, @batch_id, @operation_type, @volume_liters, @from_tank_id,
                    @to_tank_id, @operation_date, @responsible_user, @reason
                )
                """,
                ("id", movement.Id),
                ("batch_id", movement.BatchId),
                ("operation_type", movement.OperationType.ToString()),
                ("volume_liters", movement.VolumeLiters),
                ("from_tank_id", movement.FromTankId),
                ("to_tank_id", movement.ToTankId),
                ("operation_date", movement.OperationDate),
                ("responsible_user", movement.ResponsibleUser),
                ("reason", movement.Reason));
        }

        foreach (var shipment in data.Shipments)
        {
            Execute(connection, transaction, """
                insert into shipments (
                    id, shipment_number, shipped_at, customer_id, vehicle_id,
                    temperature, basis_document, responsible_user
                )
                values (
                    @id, @shipment_number, @shipped_at, @customer_id, @vehicle_id,
                    @temperature, @basis_document, @responsible_user
                )
                """,
                ("id", shipment.Id),
                ("shipment_number", shipment.ShipmentNumber),
                ("shipped_at", shipment.ShippedAt),
                ("customer_id", shipment.CustomerId),
                ("vehicle_id", shipment.VehicleId),
                ("temperature", shipment.Temperature),
                ("basis_document", shipment.BasisDocument),
                ("responsible_user", shipment.ResponsibleUser));
        }

        foreach (var item in data.ShipmentItems)
        {
            Execute(connection, transaction, """
                insert into shipment_items (id, shipment_id, batch_id, volume_liters)
                values (@id, @shipment_id, @batch_id, @volume_liters)
                """,
                ("id", item.Id), ("shipment_id", item.ShipmentId), ("batch_id", item.BatchId), ("volume_liters", item.VolumeLiters));
        }

        foreach (var entry in data.AuditLog)
        {
            Execute(connection, transaction, """
                insert into audit_log (id, created_at, user_name, action, details)
                values (@id, @created_at, @user_name, @action, @details)
                """,
                ("id", entry.Id),
                ("created_at", entry.CreatedAt),
                ("user_name", entry.UserName),
                ("action", entry.Action),
                ("details", entry.Details));
        }
    }

    private static List<UserAccount> LoadUsers(NpgsqlConnection connection)
    {
        var result = new List<UserAccount>();
        using var reader = Read(connection, "select id, login, coalesce(password_hash, ''), full_name, role, is_active from users order by full_name");
        while (reader.Read())
        {
            result.Add(new UserAccount
            {
                Id = reader.GetGuid(0),
                Login = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                FullName = reader.GetString(3),
                Role = ParseEnum(reader.GetString(4), UserRole.Operator),
                IsActive = reader.GetBoolean(5)
            });
        }

        return result;
    }

    private static List<AnimalGroup> LoadAnimalGroups(NpgsqlConnection connection)
    {
        var result = new List<AnimalGroup>();
        using var reader = Read(connection, "select id, name, location from animal_groups order by name");
        while (reader.Read())
        {
            result.Add(new AnimalGroup { Id = reader.GetGuid(0), Name = reader.GetString(1), Location = reader.GetString(2) });
        }

        return result;
    }

    private static List<ProductType> LoadProductTypes(NpgsqlConnection connection)
    {
        var result = new List<ProductType>();
        using var reader = Read(connection, "select id, name, shelf_life_hours from product_types order by name");
        while (reader.Read())
        {
            result.Add(new ProductType { Id = reader.GetGuid(0), Name = reader.GetString(1), ShelfLifeHours = reader.GetInt32(2) });
        }

        return result;
    }

    private static List<StorageTank> LoadStorageTanks(NpgsqlConnection connection)
    {
        var result = new List<StorageTank>();
        using var reader = Read(connection, "select id, name, capacity_liters, location from storage_tanks order by name");
        while (reader.Read())
        {
            result.Add(new StorageTank { Id = reader.GetGuid(0), Name = reader.GetString(1), CapacityLiters = reader.GetDecimal(2), Location = reader.GetString(3) });
        }

        return result;
    }

    private static List<Customer> LoadCustomers(NpgsqlConnection connection)
    {
        var result = new List<Customer>();
        using var reader = Read(connection, "select id, name, inn, address from customers order by name");
        while (reader.Read())
        {
            result.Add(new Customer { Id = reader.GetGuid(0), Name = reader.GetString(1), Inn = reader.GetString(2), Address = reader.GetString(3) });
        }

        return result;
    }

    private static List<Vehicle> LoadVehicles(NpgsqlConnection connection)
    {
        var result = new List<Vehicle>();
        using var reader = Read(connection, "select id, name, plate_number, driver from vehicles order by name");
        while (reader.Read())
        {
            result.Add(new Vehicle { Id = reader.GetGuid(0), Name = reader.GetString(1), PlateNumber = reader.GetString(2), Driver = reader.GetString(3) });
        }

        return result;
    }

    private static List<Batch> LoadBatches(NpgsqlConnection connection)
    {
        var result = new List<Batch>();
        using var reader = Read(connection, """
            select id, batch_number, created_at, source_description, volume_liters, weight_kg,
                   remaining_liters, status, storage_tank_id, product_type_id, expiration_date,
                   created_by, documents, notes
            from batches
            order by created_at desc
            """);
        while (reader.Read())
        {
            result.Add(new Batch
            {
                Id = reader.GetGuid(0),
                BatchNumber = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2).ToLocalTime(),
                SourceDescription = reader.GetString(3),
                VolumeLiters = reader.GetDecimal(4),
                WeightKg = reader.GetDecimal(5),
                RemainingLiters = reader.GetDecimal(6),
                Status = ParseEnum(reader.GetString(7), BatchStatus.Created),
                StorageTankId = GetNullableGuid(reader, 8),
                ProductTypeId = GetNullableGuid(reader, 9),
                ExpirationDate = reader.GetDateTime(10).ToLocalTime(),
                CreatedBy = reader.GetString(11),
                Documents = reader.GetString(12),
                Notes = reader.GetString(13)
            });
        }

        return result;
    }

    private static List<MilkYield> LoadMilkYields(NpgsqlConnection connection)
    {
        var result = new List<MilkYield>();
        using var reader = Read(connection, """
            select id, milked_at, animal_group_id, farm, room, operator_name,
                   volume_liters, weight_kg, temperature, batch_id
            from milk_yields
            order by milked_at desc
            """);
        while (reader.Read())
        {
            result.Add(new MilkYield
            {
                Id = reader.GetGuid(0),
                MilkedAt = reader.GetDateTime(1).ToLocalTime(),
                AnimalGroupId = reader.GetGuid(2),
                Farm = reader.GetString(3),
                Room = reader.GetString(4),
                OperatorName = reader.GetString(5),
                VolumeLiters = reader.GetDecimal(6),
                WeightKg = reader.GetDecimal(7),
                Temperature = reader.GetDecimal(8),
                BatchId = GetNullableGuid(reader, 9)
            });
        }

        return result;
    }

    private static List<BatchQualityTest> LoadQualityTests(NpgsqlConnection connection)
    {
        var result = new List<BatchQualityTest>();
        using var reader = Read(connection, """
            select id, batch_id, test_date, fat_percent, protein_percent, acidity, density,
                   temperature, visual_result, has_foreign_impurities, express_tests,
                   conclusion, tested_by, comment
            from batch_quality_tests
            order by test_date desc
            """);
        while (reader.Read())
        {
            result.Add(new BatchQualityTest
            {
                Id = reader.GetGuid(0),
                BatchId = reader.GetGuid(1),
                TestDate = reader.GetDateTime(2).ToLocalTime(),
                FatPercent = reader.GetDecimal(3),
                ProteinPercent = reader.GetDecimal(4),
                Acidity = reader.GetDecimal(5),
                Density = reader.GetDecimal(6),
                Temperature = reader.GetDecimal(7),
                VisualResult = reader.GetString(8),
                HasForeignImpurities = reader.GetBoolean(9),
                ExpressTests = reader.GetString(10),
                Conclusion = ParseEnum(reader.GetString(11), QualityConclusion.Suitable),
                TestedBy = reader.GetString(12),
                Comment = reader.GetString(13)
            });
        }

        return result;
    }

    private static List<StockMovement> LoadStockMovements(NpgsqlConnection connection)
    {
        var result = new List<StockMovement>();
        using var reader = Read(connection, """
            select id, batch_id, operation_type, volume_liters, from_tank_id, to_tank_id,
                   operation_date, responsible_user, reason
            from stock_movements
            order by operation_date desc
            """);
        while (reader.Read())
        {
            result.Add(new StockMovement
            {
                Id = reader.GetGuid(0),
                BatchId = reader.GetGuid(1),
                OperationType = ParseEnum(reader.GetString(2), StockOperationType.Receipt),
                VolumeLiters = reader.GetDecimal(3),
                FromTankId = GetNullableGuid(reader, 4),
                ToTankId = GetNullableGuid(reader, 5),
                OperationDate = reader.GetDateTime(6).ToLocalTime(),
                ResponsibleUser = reader.GetString(7),
                Reason = reader.GetString(8)
            });
        }

        return result;
    }

    private static List<Shipment> LoadShipments(NpgsqlConnection connection)
    {
        var result = new List<Shipment>();
        using var reader = Read(connection, """
            select id, shipment_number, shipped_at, customer_id, vehicle_id,
                   temperature, basis_document, responsible_user
            from shipments
            order by shipped_at desc
            """);
        while (reader.Read())
        {
            result.Add(new Shipment
            {
                Id = reader.GetGuid(0),
                ShipmentNumber = reader.GetString(1),
                ShippedAt = reader.GetDateTime(2).ToLocalTime(),
                CustomerId = reader.GetGuid(3),
                VehicleId = reader.GetGuid(4),
                Temperature = reader.GetDecimal(5),
                BasisDocument = reader.GetString(6),
                ResponsibleUser = reader.GetString(7)
            });
        }

        return result;
    }

    private static List<ShipmentItem> LoadShipmentItems(NpgsqlConnection connection)
    {
        var result = new List<ShipmentItem>();
        using var reader = Read(connection, "select id, shipment_id, batch_id, volume_liters from shipment_items");
        while (reader.Read())
        {
            result.Add(new ShipmentItem
            {
                Id = reader.GetGuid(0),
                ShipmentId = reader.GetGuid(1),
                BatchId = reader.GetGuid(2),
                VolumeLiters = reader.GetDecimal(3)
            });
        }

        return result;
    }

    private static List<AuditEntry> LoadAudit(NpgsqlConnection connection)
    {
        var result = new List<AuditEntry>();
        using var reader = Read(connection, "select id, created_at, user_name, action, details from audit_log order by created_at desc");
        while (reader.Read())
        {
            result.Add(new AuditEntry
            {
                Id = reader.GetGuid(0),
                CreatedAt = reader.GetDateTime(1).ToLocalTime(),
                UserName = reader.GetString(2),
                Action = reader.GetString(3),
                Details = reader.GetString(4)
            });
        }

        return result;
    }

    private static QualityNorms LoadNorms(NpgsqlConnection connection)
    {
        using var reader = Read(connection, """
            select min_fat_percent, min_protein_percent, max_acidity, min_density, max_temperature
            from quality_norms
            order by valid_from desc
            limit 1
            """);
        if (!reader.Read())
        {
            return new QualityNorms();
        }

        return new QualityNorms
        {
            MinFatPercent = reader.GetDecimal(0),
            MinProteinPercent = reader.GetDecimal(1),
            MaxAcidity = reader.GetDecimal(2),
            MinDensity = reader.GetDecimal(3),
            MaxTemperature = reader.GetDecimal(4)
        };
    }

    private static NpgsqlDataReader Read(NpgsqlConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteReader();
    }

    private static void Execute(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(CreateParameter(parameter.Name, parameter.Value));
        }

        command.ExecuteNonQuery();
    }

    private static NpgsqlParameter CreateParameter(string name, object? value)
    {
        if (value is null)
        {
            return new NpgsqlParameter(name, DBNull.Value);
        }

        if (value is Guid guid)
        {
            return new NpgsqlParameter<Guid>(name, NpgsqlDbType.Uuid) { TypedValue = guid };
        }

        if (value is DateTime dateTime)
        {
            return new NpgsqlParameter<DateTime>(name, NpgsqlDbType.TimestampTz)
            {
                TypedValue = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime()
            };
        }

        return new NpgsqlParameter(name, value);
    }

    private static Guid? GetNullableGuid(NpgsqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, out var parsed) ? parsed : defaultValue;
    }

    private static Guid RoleId(string role)
    {
        return role switch
        {
            "Administrator" => Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Manager" => Guid.Parse("10000000-0000-0000-0000-000000000002"),
            "Operator" => Guid.Parse("10000000-0000-0000-0000-000000000003"),
            "LabTechnician" => Guid.Parse("10000000-0000-0000-0000-000000000004"),
            _ => Guid.Parse("10000000-0000-0000-0000-000000000003")
        };
    }
}
