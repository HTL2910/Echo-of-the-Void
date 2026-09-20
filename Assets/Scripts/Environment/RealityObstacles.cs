using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Environment
{
    /// <summary>Anything that becomes solid in exactly one realm (single platforms, whole tilemaps...).</summary>
    public interface IRealityObstacle
    {
        RealmType SolidInRealm { get; }

        /// <summary>True if this obstacle would occupy any part of <paramref name="worldBounds"/> when solid.</summary>
        bool Overlaps(Bounds worldBounds);
    }

    /// <summary>Registry used by the Reality Shift guard (spec 2.2).</summary>
    public static class RealityObstacles
    {
        private static readonly List<IRealityObstacle> All = new List<IRealityObstacle>();

        public static void Register(IRealityObstacle obstacle)
        {
            if (!All.Contains(obstacle)) All.Add(obstacle);
        }

        public static void Unregister(IRealityObstacle obstacle) => All.Remove(obstacle);

        /// <summary>True if shifting into <paramref name="realm"/> would embed something occupying <paramref name="occupant"/> in solid geometry.</summary>
        public static bool AnySolidOverlap(RealmType realm, Bounds occupant)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var obstacle = All[i];
                if (obstacle.SolidInRealm == realm && obstacle.Overlaps(occupant)) return true;
            }
            return false;
        }
    }
}
