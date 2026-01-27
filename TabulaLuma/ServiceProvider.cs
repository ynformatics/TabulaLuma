namespace TabulaLuma
{
    public static class ServiceProvider
    {
        private static readonly Dictionary<Type, object> services = new();

        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public static T GetService<T>() where T : class
        {
            return services[typeof(T)] as T;
        }
    }
}
