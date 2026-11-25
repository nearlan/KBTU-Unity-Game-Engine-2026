using System.Collections.Generic;
using UnityEngine;

namespace RUSH.Core
{
    public static class CollectionExtensions
    {
        public static T GetRandom<T>(this IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                return default;

            return items[Random.Range(0, items.Count)];
        }
    }
}
