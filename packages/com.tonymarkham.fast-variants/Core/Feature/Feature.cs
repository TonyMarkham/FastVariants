using System;
using System.Collections.Generic;
using FastVariants.Core.Utilities;
using UnityEngine;

namespace FastVariants.Core.Feature
{
    [Serializable]
    public abstract class Feature
    {
        [SerializeField] protected string m_Id;
        public string id => m_Id;
        
        [SerializeField] protected string m_Name;
        public string name => m_Name;
        
        [SerializeField] protected string m_Code;
        public string code => m_Code;
        
        [SerializeField] protected string m_FeatureSetId;
        public string featureSetId => m_FeatureSetId;

        public object value => GetValue();
        
        // ---------------------------------------------------------------------------------------------------------- //

        protected Feature()
        {
        }

        protected Feature(string name, string code)
        {
            m_Id = GuidUtilities.NewId();
            m_Name = name;
            m_Code = code;
        }
        
        // ---------------------------------------------------------------------------------------------------------- //

        public void SetId(string id)
        {
            TrySetProperty(ref m_Id, id);
        }
        
        public void SetName(string name)
        {
            TrySetProperty(ref m_Name, name);
        }
        
        public void SetCode(string code)
        {
            TrySetProperty(ref m_Code, code);
        }

        public void SetFeatureSetId(string featureSetId)
        {
            TrySetProperty(ref m_FeatureSetId, featureSetId);
        }

        public abstract object GetValue();

        public abstract bool IsValid();

        public virtual void Invalidate()
        {
        }

        protected bool TrySetProperty<T>(ref T storage, T value)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            
            return true;
        }
    }

    [Serializable]
    public abstract class Feature<TValue> : Feature
    {
        protected Feature()
        {
        }

        protected Feature(string name, string code) : base(name, code)
        {
        }

        public new abstract TValue value { get; }

        public sealed override object GetValue()
        {
            return value;
        }
    }
}
