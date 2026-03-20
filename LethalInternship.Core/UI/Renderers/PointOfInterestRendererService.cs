using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.UI.Icons;
using LethalInternship.SharedAbstractions.Enums;
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
        private readonly Dictionary<int, IIconUIInfos> dictIconInfos;

        private readonly List<Type> priorityOrder = new List<Type>()
        {
            typeof(PositionInterestPoint),
            typeof(VehicleInterestPoint),
            typeof(ShipInterestPoint)
        };

        public PointOfInterestRendererService(InterestPointRendererRegistery registery)
        {
            this.registery = registery;
            dictIconInfos = new Dictionary<int, IIconUIInfos>();
        }

        public IIconUIInfos GetIconUIInfos(IPointOfInterest pointOfInterest)
        {
            var imagesPrefabs = new List<GameObject>();
            EnumIconImagesTypes iconImagesTypes = EnumIconImagesTypes.None;

            Dictionary<Type, IInterestPoint> dictTypeInterestPoint = pointOfInterest.GetDictTypeInterestPoints();
            foreach (var type in priorityOrder)
            {
                if (dictTypeInterestPoint.TryGetValue(type, out var interestPoint))
                {
                    iconImagesTypes |= registery.GetIconImagesTypes(interestPoint);
                }
            }

            if (dictIconInfos.TryGetValue((int)iconImagesTypes, out IIconUIInfos iconUIInfos))
            {
                return iconUIInfos;
            }

            dictIconInfos[(int)iconImagesTypes] = new IconUIInfos(iconImagesTypes);
            return dictIconInfos[(int)iconImagesTypes];
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
