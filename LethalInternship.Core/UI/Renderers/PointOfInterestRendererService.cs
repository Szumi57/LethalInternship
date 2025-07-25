﻿using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.UI.Icons;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers
{
    public class PointOfInterestRendererService
    {
        private readonly InterestPointRendererRegistery registery;
        private readonly Dictionary<string, IIconUIInfos> dictIconInfos;

        private readonly List<Type> priorityOrder = new List<Type>()
        {
            typeof(DefaultInterestPoint),
            typeof(VehicleInterestPoint),
            typeof(ShipInterestPoint)
        };

        public PointOfInterestRendererService(InterestPointRendererRegistery registery)
        {
            this.registery = registery;
            dictIconInfos = new Dictionary<string, IIconUIInfos>();
        }

        public IIconUIInfos GetIconUIInfos(IPointOfInterest pointOfInterest)
        {
            string key = string.Empty;
            var imagesPrefabs = new List<GameObject>();
            foreach (var interestPoint in pointOfInterest.GetListInterestPoints())
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

        public Vector3 GetUIIcon(IPointOfInterest pointOfInterest)
        {
            Dictionary<Type, IInterestPoint> dictTypeInterestPoint = pointOfInterest.GetDictTypeInterestPoints();
            foreach (var type in priorityOrder)
            {
                if (dictTypeInterestPoint.TryGetValue(type, out var interestPoint))
                {
                    return registery.GetUIPosOffset(interestPoint);
                }
            }

            foreach (IInterestPoint interestPoint in dictTypeInterestPoint.Values)
            {
                return registery.GetUIPosOffset(interestPoint);
            }

            return Vector3.zero;
        }
    }
}
