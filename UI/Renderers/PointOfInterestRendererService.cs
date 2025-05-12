using LethalInternship.Interfaces;
using LethalInternship.UI.Icons;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.UI.Renderers
{
    public class PointOfInterestRendererService
    {
        private readonly InterestPointRendererRegistery registery;

        public PointOfInterestRendererService(InterestPointRendererRegistery registery)
        {
            this.registery = registery;
        }

        public IIconUIInfos GetIconUIInfos(IPointOfInterest pointOfInterest)
        {
            var imagesPrefabs = new List<GameObject>();
            foreach (var interestPoint in pointOfInterest.GetInterestPoints())
            {
                GameObject? imagePrefab = registery.GetImagePrefab(interestPoint);
                if (imagePrefab != null)
                {
                    imagesPrefabs.Add(imagePrefab);
                }
            }

            return new IconUIInfos(pointOfInterest.GetPoint().ToString(), imagesPrefabs);
        }
    }
}
