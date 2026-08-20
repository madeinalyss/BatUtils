using System.Globalization;
using System.Threading;

namespace BatUtils.Services
{
    public static class LocalizationManager
    {
        public static void SetLanguage(string cultureName)
        {
            var culture = new CultureInfo(cultureName);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Properties.Strings.Culture = culture;
        }
    }
}
