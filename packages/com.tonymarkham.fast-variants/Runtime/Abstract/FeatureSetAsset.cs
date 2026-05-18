using System;
using FastVariants.Core.Feature;
using Google.Protobuf;
using UnityEngine;

namespace FastVariants.Abstract
{
    public abstract class FeatureSetAsset : ScriptableObject
    {
        [SerializeField] protected string m_ItemName;
        [SerializeField] protected string m_ItemCode;
        [SerializeField] protected byte[] m_Protobuf = Array.Empty<byte>();

        public bool hasProtobuf
        {
            get
            {
                Sync();
                return m_Protobuf is { Length: > 0 };
            }
        }

        public byte[] protobufBytes
        {
            get
            {
                Sync();
                return m_Protobuf;
            }
        }

        protected virtual void OnValidate()
        {
            Sync();
        }

        protected void Sync()
        {
            EnsureFeatureSet();
            SyncFeatureSet();
            m_Protobuf = BuildProtoMessage().ToByteArray();
        }

        protected void SyncFeatureSetIdentity(FeatureSet featureSet)
        {
            if (featureSet == null)
                return;

            if (string.IsNullOrEmpty(featureSet.name)
                || !featureSet.name.Equals(m_ItemName, StringComparison.Ordinal))
            {
                featureSet.SetName(m_ItemName);
            }

            if (string.IsNullOrEmpty(featureSet.code)
                || !featureSet.code.Equals(m_ItemCode, StringComparison.Ordinal))
            {
                featureSet.SetCode(m_ItemCode);
            }
        }

        protected abstract void EnsureFeatureSet();
        protected abstract void SyncFeatureSet();
        protected abstract IMessage BuildProtoMessage();
    }
}
