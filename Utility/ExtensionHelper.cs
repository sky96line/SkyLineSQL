using System.Collections.Generic;

namespace SkyLineSQL.Utility
{
    public static class ExtensionHelper
    {
        public static string AddUpdate(this Dictionary<string, int> dic, string key)
        {
            if (dic.ContainsKey(key))
            {
                dic[key]++;
            }
            else
            {
                dic.Add(key, 1);
            }
            return key;
        }
    }
}
