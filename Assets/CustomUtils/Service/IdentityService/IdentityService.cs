using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

public class IdentityService : IAsyncService {

    private ImmutableHashSet<Type> _providerTypeSet;

    async Task IAsyncService.InitAsync() {
        _providerTypeSet = ReflectionProvider.GetInterfaceTypes<IIdentityProvider>().ToImmutableHashSet();
        await Task.CompletedTask;
    }

    async Task IAsyncService.StartAsync() {
        foreach (var type in _providerTypeSet) {
            
        }

        await Task.CompletedTask;
    }

    async Task IAsyncService.StopAsync() {
        await Task.CompletedTask;   
    }

    public void LoginIn() {
        
    }
}

public interface IIdentityProvider {

    public IAuthToken GetAuthToken();
}

public interface IAuthToken {
    
}

public abstract class AuthTokenAcquirer {
    
}

public class NullIdentityProvider : IIdentityProvider {

    public IAuthToken GetAuthToken() => new NullAuthToken();

    public class NullAuthToken : IAuthToken {
        
    }
}

public class EditorIdentityProvider : IIdentityProvider {

    public IAuthToken GetAuthToken() => new EditorAuthToken();

    public class EditorAuthToken : IAuthToken {
        
    }
}