
using System.Text.Json.Nodes;

namespace BlazorFlow.Helpers
{
    public static class JsonObjectExtensions
    {
        /// <summary>
        /// Deep-merges <paramref name="source"/> into <paramref name="target"/>.
        /// - If both values are JsonObject, merge recursively
        /// - Otherwise, source overwrites target for that key
        /// </summary>
        public static void Merge(this JsonObject target, JsonObject? source)
        {
            if (source is null) return;

            foreach (var kvp in source)
            {
                var key = kvp.Key;
                var src = kvp.Value;

                if (src is null)
                {
                    // You can choose to skip or explicitly set null
                    target[key] = null;
                    continue;
                }

                if (target[key] is JsonObject tgtObj && src is JsonObject srcObj)
                {
                    // Both are objects -> deep merge
                    tgtObj.Merge(srcObj);
                }
                else
                {
                    // Overwrite (or add) with a clone to avoid shared references
                    target[key] = src.DeepClone();
                }
            }
        }
    }
}