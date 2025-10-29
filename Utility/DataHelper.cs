using Recipe_Sharing_Platform_API.Data;
using Microsoft.EntityFrameworkCore;

namespace Recipe_Sharing_Platform_API.Utility
{
    public static class DataHelper
    {
        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            var dbContextSvc =  svcProvider.GetRequiredService<ApplicationDbContext>();
            await dbContextSvc.Database.MigrateAsync();
        }
    }
}