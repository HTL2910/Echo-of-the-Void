using System.Collections.Generic;
using UnityEngine;

namespace EchoOfTheVoid.Environment
{
    /// <summary>
    /// The area of one room (spec 2.1). A trigger box: the camera is confined to it while Kael is inside.
    /// Place one per room scene, sized to the room. <see cref="RoomManager"/> reads them; ids must be unique and stable.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomBounds : MonoBehaviour
    {
        [SerializeField] private string roomId = "room";
        [SerializeField] private string displayName = "";

        private static readonly List<RoomBounds> Registry = new List<RoomBounds>();
        private BoxCollider2D _box;

        public static IReadOnlyList<RoomBounds> All => Registry;

        public string RoomId => roomId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? roomId : displayName;

        public void Configure(string id, string name = "")
        {
            roomId = id;
            displayName = name;
        }

        /// <summary>World-space rectangle of the room.</summary>
        public Bounds WorldBounds
        {
            get
            {
                if (_box == null) _box = GetComponent<BoxCollider2D>();
                Vector3 center = transform.TransformPoint(_box.offset);
                Vector3 size = Vector3.Scale(_box.size, transform.lossyScale);
                return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 1f));
            }
        }

        public bool Contains(Vector2 point)
        {
            Bounds b = WorldBounds;
            return point.x >= b.min.x && point.x <= b.max.x && point.y >= b.min.y && point.y <= b.max.y;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnEnable() => Registry.Add(this);
        private void OnDisable() => Registry.Remove(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
            Bounds b = WorldBounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
