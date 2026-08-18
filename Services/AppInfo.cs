using System.Reflection;

namespace BatUtils.Services
{
    public static class AppInfo
    {
        public static string Version
        {
            get
            {
                Assembly assembly =
                    Assembly.GetExecutingAssembly();

                AssemblyInformationalVersionAttribute attribute =
                    (AssemblyInformationalVersionAttribute)
                    assembly.GetCustomAttributes(
                        typeof(AssemblyInformationalVersionAttribute),
                        false)[0];

                return attribute.InformationalVersion;
            }
        }
    }
}