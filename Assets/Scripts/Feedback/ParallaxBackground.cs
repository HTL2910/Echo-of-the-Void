using UnityEngine;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>
    /// Smooth multi-layer parallax background controller for Aether-punk atmospheric depth.
    /// Supports both manually structured sprite layers and automated camera tracking.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform transform;
            [Tooltip("0 = fixed to camera, 1 = static in world. Typical: 0.05 for distant sky, 0.25 for distant silhouettes, 0.5 for mid structures")]
            [Range(0f, 1f)] public float parallaxFactorX = 0.2f;
            [Range(0f, 1f)] public float parallaxFactorY = 0.1f;
            public bool infiniteRepeatX = true;
            public float repeatWidth = 32f;

            [HideInInspector] public Vector3 startPosition;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private ParallaxLayer[] layers;

        private Vector3 _lastCameraPosition;
        private bool _isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null) return;

            _lastCameraPosition = targetCamera.transform.position;

            if (layers != null)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i].transform != null)
                    {
                        layers[i].startPosition = layers[i].transform.position;
                    }
                }
            }

            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!_isInitialized || targetCamera == null) return;

            Vector3 currentCamPos = targetCamera.transform.position;
            Vector3 delta = currentCamPos - _lastCameraPosition;

            if (layers != null)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];
                    if (layer.transform == null) continue;

                    // Parallax movement: movement is scaled down relative to camera motion
                    Vector3 newPos = layer.transform.position;
                    newPos.x += delta.x * (1f - layer.parallaxFactorX);
                    newPos.y += delta.y * (1f - layer.parallaxFactorY);

                    // Optional infinite repeat on X axis
                    if (layer.infiniteRepeatX && layer.repeatWidth > 0f)
                    {
                        float diffX = currentCamPos.x - newPos.x;
                        if (Mathf.Abs(diffX) >= layer.repeatWidth)
                        {
                            newPos.x += Mathf.Sign(diffX) * layer.repeatWidth;
                        }
                    }

                    layer.transform.position = newPos;
                }
            }

            _lastCameraPosition = currentCamPos;
        }

        /// <summary>
        /// Creates a beautiful procedural ambient dust mote particle effect around the camera.
        /// </summary>
        public static GameObject CreateAmbientDustParticles(Transform parent, Color moteColor)
        {
            GameObject motesObj = new GameObject("AmbientDustMotes");
            motesObj.transform.SetParent(parent, false);
            motesObj.transform.localPosition = new Vector3(0f, 0f, 1f);

            var ps = motesObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor = moteColor;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(70f, 24f, 0f);
            shape.scale = new Vector3(220f, 50f, 1f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.35f); // Gently drift upward
            var zCurve = new ParticleSystem.MinMaxCurve();
            zCurve.mode = ParticleSystemCurveMode.TwoConstants;
            zCurve.constantMin = 0f;
            zCurve.constantMax = 0f;
            velocity.z = zCurve;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(moteColor, 0f), new GradientColorKey(moteColor, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var renderer = motesObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 5; // In front of background, behind gameplay
            return motesObj;
        }
    }
}
