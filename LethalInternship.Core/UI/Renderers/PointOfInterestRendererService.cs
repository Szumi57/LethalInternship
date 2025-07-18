using LethalInternship.Core.UI.Icons;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.UI;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers
{
    public class PointOfInterestRendererService
    {
        private readonly InterestPointRendererRegistery registery;
        private readonly Dictionary<string, IIconUIInfos> dictIconInfos;

        public PointOfInterestRendererService(InterestPointRendererRegistery registery)
        {
            this.registery = registery;
            dictIconInfos = new Dictionary<string, IIconUIInfos>();
        }

        public IIconUIInfos GetIconUIInfos(IPointOfInterest pointOfInterest)
        {
            string key = string.Empty;
            var imagesPrefabs = new List<GameObject>();
            foreach (var interestPoint in pointOfInterest.GetInterestPoints())
            {
                GameObject? imagePrefab = registery.GetImagePrefab(interestPoint);
                if (imagePrefab != null)
                {
                    imagesPrefabs.Add(imagePrefab);
                    key += imagePrefab.name;
                }
            }

            if (dictIconInfos.TryGetValue(key, out IIconUIInfos iconUIInfos))
            {
                return iconUIInfos;
            }

            dictIconInfos[key] = new IconUIInfos(key, imagesPrefabs);
            return dictIconInfos[key];
        }
    }
}
