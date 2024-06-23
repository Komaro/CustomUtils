public class Singleton<T> where T : class, new() 
{
    private static T _instance;
    public static T instance => _instance ??= new T();
    public static T inst => instance;
}