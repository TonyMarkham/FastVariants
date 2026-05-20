using System;
using FastVariants.Core.Feature;

namespace FastVariants.GameObjectVisibilityVariant
{
    [Serializable]
    public class GameObjectVisibilityVariantSet : FeatureSet
    {
        public GameObjectVisibilityVariantSet()
        {
        }

        public GameObjectVisibilityVariantSet(string name, string code) : base(name, code)
        {
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(name)
                   && !string.IsNullOrEmpty(code);
        }

        public override bool TryAddFeature(Feature feature, bool makeActive = false)
        {
            if (feature is not FastVariants.GameObjectVisibilityVariant.GameObjectVisibilityVariant gameObjectVisibilityVariant
                || !base.TryAddFeature(feature))
            {
                return false;
            }
            
            if(featureSets.Count == 1)
            {
                SetActiveFeature(gameObjectVisibilityVariant);
                Apply();
            }
            
            if(makeActive)
            {
                SetActiveFeature(gameObjectVisibilityVariant);
                Apply();
            }
            
            return true;
        }

        protected override bool Apply()
        {
            if (!IsValid()
                || activeFeature is not FastVariants.GameObjectVisibilityVariant.GameObjectVisibilityVariant activeVariant)
                return false;
            
            foreach (var feature in featureSets)
            {
                if (feature is not FastVariants.GameObjectVisibilityVariant.GameObjectVisibilityVariant variant
                    || variant.value == null)
                    continue;

                foreach (var gameObject in variant.value)
                {
                    if (gameObject != null)
                        gameObject.SetActive(variant.id == activeVariant.id);
                }
            }
            
            return true;
        }
    }
}
