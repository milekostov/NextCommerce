using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace NextCommerceShop.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            if (json == null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
