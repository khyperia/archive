using System.Reflection;

namespace ProgComDotNet
{
    static class Ext
    {
        public static string Label(this MethodBase method)
        {
            return string.Format("__{0}_{1}", method.Name, method.MetadataToken);
        }
    }
}