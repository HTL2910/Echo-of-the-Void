using UnityEngine;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Stable identity for anything whose state must survive a save (pickups, levers, bosses...).
    /// The id is generated once in the editor and serialized with the scene/prefab; do not change it afterwards.
    /// </summary>
    public class PersistentId : MonoBehaviour
    {
        [SerializeField] private string id;

        public string Id => id;

        public void SetId(string value) => id = value;

#if UNITY_EDITOR
        private void Reset()
        {
            if (string.IsNullOrEmpty(id)) id = System.Guid.NewGuid().ToString("N");
        }

        private void OnValidate()
        {
            // Prefab assets keep their id empty; every scene instance gets its own on first validate
            if (string.IsNullOrEmpty(id) && gameObject.scene.IsValid()) id = System.Guid.NewGuid().ToString("N");
        }
#endif
    }
}
