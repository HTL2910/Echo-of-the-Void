using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI
{
    /// <summary>
    /// Boss health bar at the bottom of the screen. Boss code only raises <see cref="BossEvents"/>;
    /// this bar shows on Engaged, follows HealthChanged, and hides on Defeated / Reset.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField] private float smoothing = 8f;

        private Canvas _canvas;
        private GameObject _root;
        private Image _fill;
        private Image _ghost;   // the "damage taken" trail that lags behind the fill
        private Text _name;
        private string _bossId;
        private float _target = 1f;
        private float _shown = 1f;
        private float _hideAt = -1f;

        public bool IsVisible => _root != null && _root.activeSelf;
        public string BossId => _bossId;
        public float FillAmount => _fill != null ? _fill.fillAmount : 0f;
        public string DisplayedName => _name != null ? _name.text : string.Empty;

        public void SetFont(Font value) => font = value;

        private void Awake()
        {
            Build();
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            BossEvents.Engaged += OnEngaged;
            BossEvents.HealthChanged += OnHealthChanged;
            BossEvents.Defeated += OnDefeated;
            BossEvents.Reset += OnReset;
        }

        private void OnDisable()
        {
            BossEvents.Engaged -= OnEngaged;
            BossEvents.HealthChanged -= OnHealthChanged;
            BossEvents.Defeated -= OnDefeated;
            BossEvents.Reset -= OnReset;
        }

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void Build()
        {
            _canvas = UiKit.CreateCanvas("BossBarCanvas", 300);
            _root = new GameObject("BossBar", typeof(RectTransform));
            _root.transform.SetParent(_canvas.transform, false);
            var rect = (RectTransform)_root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 70f);
            rect.sizeDelta = new Vector2(1000f, 90f);

            _name = UiKit.CreateLabel(_root.transform, "", 30, font, UiKit.TextColor, TextAnchor.LowerCenter);
            var nameRect = _name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.sizeDelta = Vector2.zero;
            Destroy(_name.GetComponent<LayoutElement>());

            var back = MakeImage("Back", new Color(0.05f, 0.06f, 0.1f, 0.9f), 0f, 0.5f);
            _ghost = MakeFilled("Ghost", new Color(1f, 0.85f, 0.3f, 0.9f));
            _fill = MakeFilled("Fill", new Color(0.85f, 0.15f, 0.25f, 1f));
            back.transform.SetAsFirstSibling();
        }

        private Image MakeImage(string name, Color color, float yMin, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, yMin);
            rect.anchorMax = new Vector2(1f, yMax);
            rect.sizeDelta = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Image MakeFilled(string name, Color color)
        {
            var image = MakeImage(name, color, 0f, 0.5f);
            image.sprite = WhiteSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1f;
            return image;
        }

        private static Sprite _white;
        private static Sprite WhiteSprite()
        {
            if (_white == null)
            {
                var texture = new Texture2D(2, 2);
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                _white = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            }
            return _white;
        }

        private void OnEngaged(string id, string displayName, int maxHealth)
        {
            _bossId = id;
            _name.text = displayName;
            _target = _shown = 1f;
            _fill.fillAmount = _ghost.fillAmount = 1f;
            _hideAt = -1f;
            _root.SetActive(true);
        }

        private void OnHealthChanged(string id, int current, int max)
        {
            if (id != _bossId || max <= 0) return;
            _target = Mathf.Clamp01((float)current / max);
            _fill.fillAmount = _target;
        }

        private void OnDefeated(string id)
        {
            if (id != _bossId) return;
            _target = 0f;
            _fill.fillAmount = 0f;
            _hideAt = Time.unscaledTime + 1.5f; // let the last hit register before the bar goes
        }

        private void OnReset(string id)
        {
            if (id != _bossId) return;
            _root.SetActive(false);
            _bossId = null;
        }

        private void Update()
        {
            if (!IsVisible) return;

            _shown = Mathf.MoveTowards(_shown, _target, smoothing * Time.unscaledDeltaTime * 0.25f);
            if (_shown < _target) _shown = _target;
            _ghost.fillAmount = _shown;

            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                _root.SetActive(false);
                _bossId = null;
                _hideAt = -1f;
            }
        }
    }
}
