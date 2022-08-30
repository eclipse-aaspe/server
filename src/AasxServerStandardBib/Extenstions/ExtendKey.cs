using AasCore.Aas3_0_RC02;
using System.Collections.Generic;
using System.Linq;

namespace Extenstions
{
    public static class ExtendKey
    {
        public static bool Matches(this Key key, Key otherKey)
        {
            if (otherKey == null)
            {
                return false;
            }

            if (key.Type == otherKey.Type && key.Value.Equals(otherKey.Value))
            {
                return true;
            }

            return false;
        }

        public static bool StartsWith(this List<Key> keyList, List<Key> otherKeyList)
        {
            if (otherKeyList == null || otherKeyList.Count == 0)
                return false;

            // simply test element-wise
            for (int i = 0; i < otherKeyList.Count; i++)
            {
                // does head have more elements than this list?
                if (i >= keyList.Count)
                    return false;

                if (!otherKeyList[i].Matches(keyList[i]))
                    return false;
            }

            // ok!
            return true;
        }

        public static string ToStringExtended(this Key key)
        {
            return $"[{key.Type}, {key.Value}]";
        }

        public static string ToStringExtended(this List<Key> keys)
        {
            return string.Join(",", keys.Select((x) => x.ToStringExtended()));
        }
    }
}
