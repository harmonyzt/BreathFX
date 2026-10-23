using UnityEngine;
using UnityEngine.UI;

namespace BreathFX.Hud
{
    internal sealed class BreathingVignette : MonoBehaviour
    {
        private const float FadeInSpeed = 0.8f;
        private const float FadeOutSpeed = 2.5f;
        private const int TextureSize = 256;

        private Canvas _canvas;
        private Image _image;
        private Sprite _sprite;
        private float _alpha;
        private float _appliedRange = -1f;

        internal void Initialize()
        {
            if (_image != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "BreathFX_Vignette",
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.layer = 5;

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            GameObject imageObject = new GameObject(
                "Vignette",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            imageObject.layer = 5;

            RectTransform rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _appliedRange = Mathf.Clamp01(Settings.VignetteRange.Value);
            _sprite = CreateVignetteSprite(_appliedRange);
            _image = imageObject.GetComponent<Image>();
            _image.sprite = _sprite;
            _image.type = Image.Type.Simple;
            _image.preserveAspect = false;
            _image.raycastTarget = false;
            _image.color = new Color(0f, 0f, 0f, 0f);
        }

        internal void Tick(bool holding, float breath)
        {
            if (_image == null)
            {
                return;
            }

            EnsureSprite();

            float target = 0f;
            if (Settings.ShowBreathVignette.Value && holding)
            {
                float t = Mathf.InverseLerp(1f, 0f, Mathf.Clamp01(breath));
                target = Mathf.SmoothStep(0f, 1f, t) * Mathf.Clamp01(Settings.VignetteStrength.Value);
            }

            float speed = target > _alpha ? FadeInSpeed : FadeOutSpeed;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime * speed);

            Color color = _image.color;
            color.a = _alpha;
            _image.color = color;
        }

        internal void Hide()
        {
            _alpha = 0f;
            if (_image == null)
            {
                return;
            }

            Color color = _image.color;
            color.a = 0f;
            _image.color = color;
        }

        private void EnsureSprite()
        {
            float range = Mathf.Clamp01(Settings.VignetteRange.Value);
            if (_sprite != null && Mathf.Approximately(range, _appliedRange))
            {
                return;
            }

            Sprite previous = _sprite;
            _sprite = CreateVignetteSprite(range);
            _appliedRange = range;
            _image.sprite = _sprite;

            if (previous != null)
            {
                Texture2D previousTexture = previous.texture;
                Destroy(previous);
                if (previousTexture != null)
                {
                    Destroy(previousTexture);
                }
            }
        }

        private static Sprite CreateVignetteSprite(float range)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "BreathFX_VignetteTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            float inner = Mathf.Lerp(0.95f, 0.05f, range);
            float outer = Mathf.Lerp(0.95f, 1f, range);

            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                float ny = (y + 0.5f) / TextureSize * 2f - 1f;
                for (int x = 0; x < TextureSize; x++)
                {
                    float nx = (x + 0.5f) / TextureSize * 2f - 1f;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny) / 1.41421356f;
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, radius));
                    pixels[y * TextureSize + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "BreathFX_VignetteSprite";
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        private void OnDestroy()
        {
            if (_sprite != null)
            {
                Texture2D texture = _sprite.texture;
                Destroy(_sprite);
                if (texture != null)
                {
                    Destroy(texture);
                }

                _sprite = null;
            }

            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
            }
        }
    }
}