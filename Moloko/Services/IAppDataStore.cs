using Moloko.Models;

namespace Moloko.Services;

public interface IAppDataStore
{
    string DataDirectory { get; }
    string BackupDirectory { get; }
    string ExportDirectory { get; }
    string ProviderName { get; }

    AppData Load();
    void Save(AppData data);
    string CreateBackup(AppData data);
}
