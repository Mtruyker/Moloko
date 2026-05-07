namespace Moloko.Services;

public static class AppDataStoreFactory
{
    public static IAppDataStore Create()
    {
        var settings = DatabaseSettings.LoadOrCreate();
        if (!settings.UsePostgreSql)
        {
            return new AppDataStore();
        }

        try
        {
            var store = new PostgreSqlAppDataStore(settings.ConnectionString);
            store.EnsureReady();
            return store;
        }
        catch (Exception ex)
        {
            var fallback = new AppDataStore();
            fallback.LastProviderError = ex.Message;
            return fallback;
        }
    }
}
