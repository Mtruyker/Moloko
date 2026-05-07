using Moloko.Models;

namespace Moloko.Infrastructure;

public static class RussianText
{
    public static string BatchStatus(BatchStatus status) => status switch
    {
        Models.BatchStatus.Created => "создана",
        Models.BatchStatus.InAnalysis => "на анализе",
        Models.BatchStatus.Approved => "допущена",
        Models.BatchStatus.Blocked => "заблокирована",
        Models.BatchStatus.PartiallyShipped => "частично отгружена",
        Models.BatchStatus.FullyShipped => "полностью отгружена",
        Models.BatchStatus.WrittenOff => "списана",
        _ => status.ToString()
    };

    public static string QualityConclusion(QualityConclusion conclusion) => conclusion switch
    {
        Models.QualityConclusion.Suitable => "годна",
        Models.QualityConclusion.ConditionallySuitable => "условно годна",
        Models.QualityConclusion.Blocked => "заблокирована",
        Models.QualityConclusion.WrittenOff => "списана",
        _ => conclusion.ToString()
    };

    public static string StockOperation(StockOperationType operation) => operation switch
    {
        StockOperationType.Receipt => "поступление",
        StockOperationType.Transfer => "перемещение",
        StockOperationType.Shipment => "отгрузка",
        StockOperationType.WriteOff => "списание",
        StockOperationType.Return => "возврат",
        StockOperationType.InventoryCorrection => "инвентаризация",
        _ => operation.ToString()
    };

    public static string Role(UserRole role) => role switch
    {
        UserRole.Administrator => "Администратор",
        UserRole.Manager => "Глава КФХ",
        UserRole.Operator => "Оператор учета",
        UserRole.LabTechnician => "Лаборант",
        UserRole.Storekeeper => "Кладовщик",
        _ => role.ToString()
    };
}
