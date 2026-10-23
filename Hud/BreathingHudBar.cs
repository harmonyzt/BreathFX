using BreathFX.Breathing;
using EFT;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BreathFX.Hud
{
    internal sealed class BreathingHudBar : MonoBehaviour
    {
        private const string BarObjectName = "BreathFX_Bar";
        private const float BarGap = -4f;
        private static readonly Color FillColor = new Color(0.78f, 0.65f, 1f, 1f);

        private Player _player;
        private Image _fill;
        private Image _background;
        private RectTransform _rect;
        private Color _fillColor;
        private Color _exhaustedFillColor;
        private Sprite _fillSprite;
        private Sprite _exhaustedSprite;

        internal static void Attach(BattleStancePanel panel, Player player)
        {
            if (panel == null || player == null || !player.IsYourPlayer || player.IsAI)
            {
                return;
            }

            BreathingHudBar existingBar = panel.GetComponentInChildren<BreathingHudBar>(true);
            if (!Settings.ShowBreathingBar.Value)
            {
                if (existingBar != null)
                {
                    existingBar.gameObject.SetActive(false);
                }

                return;
            }

            RectTransform sprint = panel._sprintFillImage != null ? panel._sprintFillImage.rectTransform : null;
            RectTransform hands = panel._handsFillImage != null ? panel._handsFillImage.rectTransform : null;
            RectTransform reference = ChooseLowerBar(sprint, hands);
            if (reference == null)
            {
                return;
            }

            Transform parent = panel.transform;
            GameObject root;
            if (existingBar != null)
            {
                root = existingBar.gameObject;
                root.transform.SetParent(parent, false);
            }
            else
            {
                root = new GameObject(BarObjectName, typeof(RectTransform), typeof(CanvasRenderer));
                root.transform.SetParent(parent, false);
                root.layer = panel.gameObject.layer;
            }

            BreathingHudBar bar = existingBar;
            if (bar == null)
            {
                bar = root.AddComponent<BreathingHudBar>();
            }

            bar.Initialize(panel, player, reference);
        }

        private static RectTransform ChooseLowerBar(RectTransform sprint, RectTransform hands)
        {
            if (sprint == null)
            {
                return hands;
            }

            if (hands == null)
            {
                return sprint;
            }

            return sprint.position.y <= hands.position.y ? sprint : hands;
        }

        private void Initialize(BattleStancePanel panel, Player player, RectTransform reference)
        {
            _player = player;
            _rect = (RectTransform)transform;
            gameObject.layer = panel.gameObject.layer;
            gameObject.SetActive(true);

            _exhaustedSprite = panel._exhaustedFillSprite;
            _fillSprite = panel._defaultFillSpriteForHands != null
                ? panel._defaultFillSpriteForHands
                : panel._defaultFillSprite;

            if (_fillSprite == null)
            {
                _fillSprite = CreateFallbackSprite();
            }

            ResolveNativeParts(reference, out RectTransform nativeBar, out Image nativeBackground,
                out Image nativeFill);
            _fillColor = FillColor;
            _exhaustedFillColor = nativeFill != null ? nativeFill.color : Color.white;

            ConfigureLayout(reference, nativeBar);

            Sprite backgroundSprite = nativeBackground != null && nativeBackground.sprite != null
                ? nativeBackground.sprite
                : _fillSprite;

            Color backgroundColor = nativeBackground != null ? nativeBackground.color : Color.white;
            Image.Type backgroundType = nativeBackground != null ? nativeBackground.type : Image.Type.Simple;

            _background = EnsureImage(transform, "Background", backgroundColor, backgroundSprite, backgroundType, 0);
            if (nativeBackground != null && nativeBackground.material != null)
            {
                _background.material = nativeBackground.material;
            }

            Image.Type fillType = nativeFill != null ? nativeFill.type : Image.Type.Filled;
            Image.FillMethod fillMethod = nativeFill != null ? nativeFill.fillMethod : Image.FillMethod.Horizontal;
            int fillOrigin = nativeFill != null ? nativeFill.fillOrigin : 0;
            bool fillClockwise = nativeFill == null || nativeFill.fillClockwise;

            _fill = EnsureImage(_background.transform, "Fill", _fillColor, _fillSprite, fillType, fillOrigin);
            _fill.fillMethod = fillMethod;
            _fill.fillOrigin = fillOrigin;
            _fill.fillClockwise = fillClockwise;
            _fill.fillAmount = 1f;

            if (nativeFill != null)
            {
                _fill.material = nativeFill.material;
                CopyLayout(nativeFill.rectTransform, (RectTransform)_fill.transform);
            }

            UpdateVisuals();
        }

        private static void ResolveNativeParts(
            RectTransform reference,
            out RectTransform nativeBar,
            out Image nativeBackground,
            out Image nativeFill)
        {
            nativeBar = reference != null ? reference.parent as RectTransform : null;
            nativeBackground = nativeBar != null ? nativeBar.GetComponent<Image>() : null;
            nativeFill = reference != null ? reference.GetComponent<Image>() : null;

            if (reference == null ||
                (nativeBackground != null && nativeFill != null && nativeFill.type == Image.Type.Filled))
            {
                return;
            }

            Image referenceImage = reference.GetComponent<Image>();
            Image childFill = null;
            for (int i = 0; i < reference.childCount; i++)
            {
                Image candidate = reference.GetChild(i).GetComponent<Image>();
                if (candidate != null && candidate.type == Image.Type.Filled)
                {
                    childFill = candidate;
                    break;
                }
            }

            if (referenceImage != null && childFill != null)
            {
                nativeBar = reference;
                nativeBackground = referenceImage;
                nativeFill = childFill;
            }
        }

        private void ConfigureLayout(RectTransform reference, RectTransform nativeBar)
        {
            RectTransform anchorBar = nativeBar != null ? nativeBar : reference;
            Vector3[] corners = new Vector3[4];
            anchorBar.GetWorldCorners(corners);
            Vector3 bottomWorld = (corners[0] + corners[3]) * 0.5f;
            Transform parent = _rect.parent;
            Vector3 bottomLocal = parent != null ? parent.InverseTransformPoint(bottomWorld) : bottomWorld;

            float width = anchorBar.rect.width;
            if (width < 1f)
            {
                width = 160f;
            }

            float height = Mathf.Max(2f, anchorBar.rect.height);

            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(width, height);
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one;
            _rect.localPosition = bottomLocal + new Vector3(0f, -height * 0.5f - BarGap, 0f);
            _rect.SetAsLastSibling();
        }

        private static void CopyLayout(RectTransform source, RectTransform target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
        }

        private Image EnsureImage(Transform parent, string name, Color color, Sprite sprite, Image.Type type,
            int fillOrigin)
        {
            Transform existing = parent.Find(name);
            if (existing == null && parent != transform)
            {
                existing = transform.Find(name);
            }

            GameObject imageObject;
            if (existing != null)
            {
                imageObject = existing.gameObject;
                imageObject.transform.SetParent(parent, false);
            }
            else
            {
                imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(parent, false);
                imageObject.layer = gameObject.layer;
            }

            Image image = imageObject.GetComponent<Image>();
            if (image == null)
            {
                image = imageObject.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.type = type;
            if (type == Image.Type.Filled)
            {
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = fillOrigin;
            }

            RectTransform rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        private void Update()
        {
            if (!Settings.ShowBreathingBar.Value)
            {
                gameObject.SetActive(false);
                return;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_player == null || _fill == null)
            {
                return;
            }

            if (!BreathingManager.TryGet(_player, out BreathingController controller) || !controller.IsActive)
            {
                SetFillSprite(_fillSprite);
                _fill.fillAmount = 1f;
                _fill.color = _fillColor;
                return;
            }

            float breath = Mathf.Clamp01(controller.Breath);
            bool exhausted = controller.IsGasping || breath <= 0.15f;
            SetFillSprite(exhausted && _exhaustedSprite != null ? _exhaustedSprite : _fillSprite);
            _fill.fillAmount = breath;
            _fill.color = exhausted ? _exhaustedFillColor : _fillColor;
        }

        private void SetFillSprite(Sprite sprite)
        {
            if (_fill != null && sprite != null && _fill.sprite != sprite)
            {
                _fill.sprite = sprite;
            }
        }

        private static Sprite CreateFallbackSprite()
        {
            Texture2D texture = Texture2D.whiteTexture;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }
    }
}