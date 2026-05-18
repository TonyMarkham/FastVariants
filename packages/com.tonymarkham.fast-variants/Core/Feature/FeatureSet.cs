using System;
using System.Collections.Generic;
using System.Linq;
using FastVariants.Core.Utilities;
using UnityEngine;

namespace FastVariants.Core.Feature
{
    [Serializable]
    public abstract class FeatureSet
    {
        [SerializeField] protected string m_Id;
        public string id => m_Id;
        
        [SerializeField] protected string m_Name;
        public string name => m_Name;
        
        [SerializeField] protected string m_Code;
        public string code => m_Code;
        
        [SerializeReference] protected List<Feature> m_Features; 
        public IReadOnlyList<Feature> featureSets => m_Features ??= new List<Feature>();

        [SerializeReference] protected Feature m_ActiveFeature;
        public Feature activeFeature => m_ActiveFeature;

        // ---------------------------------------------------------------------------------------------------------- //

        protected FeatureSet()
        {
            m_Features = new List<Feature>();
        }

        protected FeatureSet(string name, string code)
        {
            m_Id = GuidUtilities.NewId();
            m_Name = name;
            m_Code = code;
            m_Features = new List<Feature>();
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
        
        public virtual void SetActiveFeature(Feature feature)
        {
            TrySetProperty(ref m_ActiveFeature, feature);
            Apply();
        }

        public virtual bool TryAddFeature(Feature feature, bool makeActive = false)
        {
            if (feature == null || featureSets.Contains(feature))
                return false;
            
            feature.SetFeatureSetId(id);
            m_Features.Add(feature);
            return true;
        }

        public void ClearFeatures()
        {
            m_Features ??= new List<Feature>();
            m_Features.Clear();
            m_ActiveFeature = null;
        }

        public abstract bool IsValid();

        public virtual void Invalidate(){}

        protected bool TrySetProperty<T>(ref T storage, T value)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            
            return true;
        }

        protected abstract bool Apply();
    }
}
