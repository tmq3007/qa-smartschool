using System;

using System.Linq;

using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using QASmartClass.Data;

using QASmartClass.Helpers;

using QASmartClass.Services;

using Xunit;



[assembly: CollectionBehavior(DisableTestParallelization = true)]



namespace QASmartClass.Tests

{

    public class V54OfflineSyncAndTicketTests : IDisposable

    {

        private readonly Microsoft.Data.Sqlite.SqliteConnection _ramConnection;

        private readonly AppDbContext _db;



        public V54OfflineSyncAndTicketTests()

        {

            _ramConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Foreign Keys=True");

            _ramConnection.Open();

            AppDbContext.FallbackInMemoryConnection = _ramConnection;



            AppDbContext.IsSeedingOrMigrating = true;

            _db = new AppDbContext();

            DbMigrator.Migrate(_db, "6.04.0");

            AppDbContext.IsSeedingOrMigrating = false;

        }



        public void Dispose()

        {

            _db.Dispose();

            AppDbContext.FallbackInMemoryConnection = null;

            _ramConnection.Close();

            _ramConnection.Dispose();

        }

        [Fact]

        public async Task OfflineSyncManager_QueueItem_SavesToDatabase()

        {

            // Clean up existing items to have a clean slate

            using (var db = new AppDbContext())

            {

                var existing = await db.OfflineSyncItems.ToListAsync();

                db.OfflineSyncItems.RemoveRange(existing);

                await db.SaveChangesAsync();

            }



            // Queue a dummy task

            var payload = new { StudentId = 42, Score = 9.5 };

            await OfflineSyncManager.QueueItemAsync("GRADE", payload);



            // Verify database contains the item

            using (var db = new AppDbContext())

            {

                var item = await db.OfflineSyncItems.FirstOrDefaultAsync(x => x.ActionType == "GRADE");

                Assert.NotNull(item);

                Assert.Equal("Pending", item.Status);

                Assert.Equal(0, item.RetryCount);

                Assert.Contains("42", item.PayloadJson);

            }

        }



        [Fact]

        public async Task OfflineSyncManager_QueueMultipleItems_MaintainsChronologicalFIFOOrder()

        {

            // Clean up

            using (var db = new AppDbContext())

            {

                var existing = await db.OfflineSyncItems.ToListAsync();

                db.OfflineSyncItems.RemoveRange(existing);

                await db.SaveChangesAsync();

            }



            // Queue three tasks sequentially

            await OfflineSyncManager.QueueItemAsync("ATTENDANCE", new { RosterId = 1, Status = "Absent" });

            await Task.Delay(100);

            await OfflineSyncManager.QueueItemAsync("GRADE", new { StudentId = 1, Score = 8.0 });

            await Task.Delay(100);

            await OfflineSyncManager.QueueItemAsync("TICKET", new { Category = "Thiết bị", Description = "Lỗi projector" });



            // Read from DB ordered by CreatedAt

            using (var db = new AppDbContext())

            {

                var items = await db.OfflineSyncItems.OrderBy(x => x.CreatedAt).ToListAsync();

                Assert.Equal(3, items.Count);

                Assert.Equal("ATTENDANCE", items[0].ActionType);

                Assert.Equal("GRADE", items[1].ActionType);

                Assert.Equal("TICKET", items[2].ActionType);

            }

        }

    }

}

