using System;
using FastVariants.Core.Feature;
using Google.Protobuf;
using UnityEngine;

namespace FastVariants.Abstract
{
    public abstract class FeatureAsset : ScriptableObject
    {
        [SerializeField] protected string m_ItemName;
        [SerializeField] protected string m_ItemCode;
        [SerializeField] protected byte[] m_Protobuf = Array.Empty<byte>();
        [SerializeField] protected bool m_ItemIsValid;

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
            EnsureFeature();
            SyncFeature();
            m_ItemIsValid = IsFeatureValid();
            m_Protobuf = BuildProtoMessage().ToByteArray();
        }

        protected void SyncFeatureIdentity(Feature feature)
        {
            if (feature == null)
                return;

            if (string.IsNullOrEmpty(feature.name)
                || !feature.name.Equals(m_ItemName, StringComparison.Ordinal))
            {
                feature.SetName(m_ItemName);
            }

            if (string.IsNullOrEmpty(feature.code)
                || !feature.code.Equals(m_ItemCode, StringComparison.Ordinal))
            {
                feature.SetCode(m_ItemCode);
            }
        }

        protected abstract void EnsureFeature();
        protected abstract void SyncFeature();
        protected abstract bool IsFeatureValid();
        protected abstract IMessage BuildProtoMessage();
    }
}
