using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Ombi.Core.Helpers
{
    public static class DiscoveryReleaseFilter
    {
        private const int MaxSourcePages = 500;

        public static bool IsReleased(string releaseDate, DateTime today)
        {
            return TryParseMediaDate(releaseDate, out var parsed) && parsed.Date <= today.Date;
        }

        public static bool IsUpcoming(string releaseDate, DateTime today)
        {
            return TryParseMediaDate(releaseDate, out var parsed) && parsed.Date > today.Date;
        }

        public static async Task<List<T>> LoadFilteredPage<T>(
            int currentPosition,
            int amountToLoad,
            int sourcePageSize,
            Func<int, Task<List<T>>> pageLoader,
            Func<T, bool> include)
        {
            if (amountToLoad <= 0 || sourcePageSize <= 0)
            {
                return new List<T>();
            }

            var skip = Math.Max(0, currentPosition);
            var required = skip + amountToLoad;
            var matching = new List<T>(required);

            for (var page = 1; page <= MaxSourcePages && matching.Count < required; page++)
            {
                var sourcePage = await pageLoader(page) ?? new List<T>();
                if (sourcePage.Count == 0)
                {
                    break;
                }

                matching.AddRange(sourcePage.Where(include));

                if (sourcePage.Count < sourcePageSize)
                {
                    break;
                }
            }

            return matching.Skip(skip).Take(amountToLoad).ToList();
        }

        private static bool TryParseMediaDate(string value, out DateTime parsed)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                parsed = default;
                return false;
            }

            return DateTime.TryParseExact(
                       value,
                       "yyyy-MM-dd",
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out parsed) ||
                   DateTime.TryParse(
                       value,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.AllowWhiteSpaces,
                       out parsed);
        }
    }
}
