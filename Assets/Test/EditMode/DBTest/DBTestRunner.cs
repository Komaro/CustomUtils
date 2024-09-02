using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using UnityEngine;

public class DBTestRunner {

    [Test]
    public async Task InMemoryDatabaseTest() {
        var dbSet = Service.GetService<InMemoryDatabaseService>().Get<SampleEntity>();
        Assert.IsTrue(dbSet != null);
        
        if (dbSet.Any() == false) {
            await dbSet.AddRangeAsync(new SampleEntity { index = 1, text = "tte2" }, new SampleEntity { index = 2, text = "tte1" });
            await Service.GetService<InMemoryDatabaseService>().SaveChangesAsync();
        }
        
        var select = await dbSet.FirstOrDefaultAsync(x => x.index == 1);
        if (select != null) {
            Logger.TraceLog(select.ToStringAllFields());
        }
        
        var find = await dbSet.FindAsync(2);
        if (find != null) {
            Logger.TraceLog(find.ToStringAllFields());
        }
        
        if (dbSet.Any()) {
            dbSet.RemoveRange(dbSet);
            await Service.GetService<InMemoryDatabaseService>().SaveChangesAsync();
        }

        Service.RemoveService<InMemoryDatabaseService>();
    }
}

public class SampleEntity : IAutoMappingEntity {

    [Key]
    public int index { get; set; }
    public string text { get; set; }
}