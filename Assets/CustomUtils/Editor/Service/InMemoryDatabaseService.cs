using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class InMemoryDatabaseService : IService {

    private DbContext _context;

    void IService.Init() => _context = new InMemoryDBContext();
    void IService.Start() { }
    void IService.Stop() { }
    void IService.Remove() => _context?.Dispose();

    private DbContext GetContext() => _context;
    
    public bool TryGet<T>(out DbSet<T> dbSet) where T : class => (dbSet = Get<T>()) != null;
    public DbSet<T> Get<T>() where T : class => GetContext().Set<T>();

    public void SaveChanges() => _context?.SaveChanges();
    
    public async Task SaveChangesAsync() {
        if (_context != null) {
            await _context.SaveChangesAsync();
        }
    }
}

public interface IAutoMappingEntity { }

public class InMemoryDBContext : DbContext {

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        foreach (var type in ReflectionProvider.GetInterfaceTypes<IAutoMappingEntity>()) {
            modelBuilder.Entity(type);
        }
        
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseInMemoryDatabase(nameof(InMemoryDBContext));
        base.OnConfiguring(optionsBuilder);
    }
}