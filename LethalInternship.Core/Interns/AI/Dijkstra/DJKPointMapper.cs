using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class DJKPointMapper
    {
        private readonly Dictionary<Type, Func<IInterestPoint, IDJKPoint>> _mappers = new Dictionary<Type, Func<IInterestPoint, IDJKPoint>>();

        public void Register<T>(Func<T, IDJKPoint> mapFunc) where T : IInterestPoint
        {
            _mappers[typeof(T)] = (ip) => mapFunc((T)ip);
        }

        public IDJKPoint Map(IInterestPoint interestPoint)
        {
            var type = interestPoint.GetType();
            if (_mappers.TryGetValue(type, out var func))
            {
                return func(interestPoint);
            }

            return new DJKStaticPoint(interestPoint.Point);
        }
    }
}
