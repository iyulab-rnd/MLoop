namespace Microsoft.Extensions.Configuration
{
    internal static class ConfigurationExtensions
    {
        public static T Resolve<T>(this IConfiguration configuration, string? name = null)
        {
            return LoadSection<T>(configuration, name) ?? throw new Exception($"Required Configuration, {name}");
        }

        public static T? LoadSection<T>(this IConfiguration configuration, string? name = null)
        {
            name ??= typeof(T).Name;
            var section = configuration.GetSection(name);
            return section.Get<T>();
        }
    }
}
