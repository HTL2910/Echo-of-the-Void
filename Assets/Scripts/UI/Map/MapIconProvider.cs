using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI.Map
{
    /// <summary>
    /// Maps ability requirements to UI icons for displaying locked doors on blueprint map.
    /// </summary>
    [CreateAssetMenu(menuName = "EchoOfTheVoid/Map/MapIconProvider", fileName = "MapIconProvider")]
    public class MapIconProvider : ScriptableObject
    {
        [SerializeField] private Sprite iconWallJump;
        [SerializeField] private Sprite iconGravityInversion;
        [SerializeField] private Sprite iconEchoAnchor;
        [SerializeField] private Sprite iconPhaseShift;

        public Sprite GetIconForAbility(AbilityFlags ability)
        {
            return ability switch
            {
                AbilityFlags.WallJump => iconWallJump,
                AbilityFlags.GravityInversion => iconGravityInversion,
                AbilityFlags.EchoAnchor => iconEchoAnchor,
                AbilityFlags.RealityShift => iconPhaseShift,
                _ => null
            };
        }
    }
}
