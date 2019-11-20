using System;
using System.Linq;
using System.Reflection;

namespace WClocks
{
    class ResourceLoader
    {
        public static Assembly LoadResourceAssembly(string name)
        {
            var assemblyName = new AssemblyName(name).Name.Replace(".resources", "");
            Assembly thisAssembly = Assembly.GetEntryAssembly();
            String dllName = string.Format("{0}.{1}.dll",
                thisAssembly.EntryPoint.DeclaringType.Namespace, assemblyName);

            var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .FirstOrDefault(rn => rn.Contains(assemblyName));
            if (resourceName != null)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            }

            return null;
        }
    }
}
