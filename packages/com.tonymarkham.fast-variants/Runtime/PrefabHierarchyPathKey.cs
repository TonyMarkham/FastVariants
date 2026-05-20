using System;
using UnityEngine;

namespace FastVariants.PrefabFingerprint
{
    [Serializable]
    public sealed class PrefabHierarchyPathKey : IEquatable<PrefabHierarchyPathKey>
    {
        public const char Delimiter = '|';

        [SerializeField] string m_Value = string.Empty;

        public string value => m_Value;

        public bool isEmpty => string.IsNullOrEmpty(m_Value);

        public PrefabHierarchyPathKey()
        {
        }

        public PrefabHierarchyPathKey(string value)
        {
            m_Value = value ?? string.Empty;
        }

        public bool Equals(PrefabHierarchyPathKey other)
        {
            return other != null && string.Equals(m_Value, other.m_Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PrefabHierarchyPathKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(m_Value);
        }

        public override string ToString()
        {
            return m_Value;
        }

        public static bool operator ==(PrefabHierarchyPathKey left, PrefabHierarchyPathKey right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(PrefabHierarchyPathKey left, PrefabHierarchyPathKey right)
        {
            return !(left == right);
        }
    }
}
