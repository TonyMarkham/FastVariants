using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FastVariants.PrefabFingerprint
{
    public static class PrefabHierarchyPathKeyEditorUtility
    {
        public static bool TryCreate(Transform transform, Transform root, out PrefabHierarchyPathKey pathKey)
        {
            pathKey = null;

            if (transform == null || root == null)
                return false;

            var siblingIndices = new List<int>();
            var current = transform;

            while (current != null && current != root)
            {
                siblingIndices.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            if (current != root)
                return false;

            siblingIndices.Reverse();
            pathKey = new PrefabHierarchyPathKey(BuildValue(siblingIndices));
            return true;
        }

        static string BuildValue(IReadOnlyList<int> siblingIndices)
        {
            if (siblingIndices == null || siblingIndices.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < siblingIndices.Count; i++)
            {
                if (i > 0)
                    builder.Append(PrefabHierarchyPathKey.Delimiter);

                builder.Append(siblingIndices[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }
}
