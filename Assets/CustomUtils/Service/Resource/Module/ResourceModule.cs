// using Object = UnityEngine.Object;
//
// [RequiresAttributeImplementation(typeof(ResourceProviderAttribute))]
// public abstract class ResourceModule<TProvider, TCacheProvider> : IResourceModule 
//     where TProvider : class, IResourceProvider, new()
//     where TCacheProvider : class, IResourceCacheProvider, new() {
//
//     protected TProvider resourceProvider;
//     protected TCacheProvider cacheProvider;
//
//     public ResourceModule() {
//         resourceProvider = new TProvider();
//         if (resourceProvider.IsReady() == false) {
//             Logger.TraceError($"{typeof(TProvider).Name} is invalid {nameof(IResourceProvider)}");
//         }
//         
//         cacheProvider = new TCacheProvider();
//         if (cacheProvider.IsReady() == false) {
//             Logger.TraceError($"{typeof(TCacheProvider).Name} is invalid {nameof(IResourceCacheProvider)}");
//         }
//     }
//
//     public virtual void Init() {
//         resourceProvider.Init();
//         cacheProvider.Init();
//     }
//     
//     public abstract void ExecuteOrder(ResourceOrder order);
//
//     public virtual void Load(ResourceOrder order) => resourceProvider?.Load(order);
//     
//     public virtual void Unload(ResourceOrder order) {
//         cacheProvider?.Unload(order);
//         resourceProvider?.Unload(order);
//     }
//
//     public virtual void Clear() {
//         cacheProvider?.Clear();
//         resourceProvider?.Clear();
//     }
//
//     public virtual Object Get(string name) {
//         var ob = cacheProvider.Get(name) ?? resourceProvider.Get(name);
//         if (ob == null) {
//             return null;
//         }
//         
//         cacheProvider.Add(name, ob);
//         return ob;
//     }
//
//     public virtual Object Get(ResourceOrder order) {
//        var ob = cacheProvider.Get(order) ?? resourceProvider.Get(order);
//        if (ob == null) {
//            return null;
//        }
//         
//        cacheProvider.Add(order, ob);
//        return ob;
//     }
//
//     public virtual string GetPath(string name) => resourceProvider.GetPath(name);
//     
//     public virtual bool IsReady() => (resourceProvider?.IsReady() ?? false) && (cacheProvider?.IsReady() ?? false);
// }