using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Renderers
{
    public class InterestPointRendererRegistery
    {
        private readonly Dictionary<Type, IInterestPointRendererWrapper> wrappers = new Dictionary<Type, IInterestPointRendererWrapper>();

        public void Register<T>(IInterestPointRenderer<T> renderer) where T : IInterestPoint
        {
            wrappers[typeof(T)] = new InterestPointRendererWrapper<T>(renderer);
        }

        public GameObject? GetImagePrefab(IInterestPoint interestPoint)
        {
            if (wrappers.TryGetValue(interestPoint.GetType(), out var interestPointRendererWrapper))
            {
                return interestPointRendererWrapper.GetImagePrefab(interestPoint);
            }

            return null;
        }

        public Vector3 GetUIPosOffset(IInterestPoint interestPoint)
        {
            if (wrappers.TryGetValue(interestPoint.GetType(), out var interestPointRendererWrapper))
            {
                return interestPointRendererWrapper.GetUIPos(interestPoint);
            }
            
            return Vector3.zero;
        }
    }
}
