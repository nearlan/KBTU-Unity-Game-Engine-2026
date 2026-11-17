using System.Collections.Generic;
using KBTU.Core;
using KBTU.Players;
using KBTU.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace KBTU.UI
{
    public class PlayerHud : MonoBehaviour
    {
        private class FloatingDamageText
        {
            public RectTransform Rect;
            public Text Label;
            public float RemainingTime;
            public float Duration;
            public float RiseSpeed;
        }

        [Header("References")]
        [SerializeField] private Player player;
        [SerializeField] private PlayerWeaponHandler weaponHandler;
        [SerializeField] private AudioSource audioSource;

        [Header("Health UI")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private RectTransform healthBarContainer;

        [Header("Juicy Health Bar")]
        [SerializeField] private bool useCrispRectSprites = true;
        [SerializeField, Min(1f)] private float smoothDrainSpeed = 10f;
        [SerializeField, Min(0f)] private float chunkDelay = 0.4f;
        [SerializeField, Min(1f)] private float chunkDrainSpeed = 7f;
        [SerializeField] private Color damageChunkColor = new(1f, 0.94f, 0.88f, 1f);
        [SerializeField] private Color highHealthColor = new(0.45f, 0.90f, 0.52f, 1f);
        [SerializeField] private Color midHealthColor = new(0.95f, 0.78f, 0.25f, 1f);
        [SerializeField] private Color lowHealthColor = new(0.92f, 0.25f, 0.22f, 1f);

        [Header("Health Bar Hit Feel")]
        [SerializeField, Min(0f)] private float shakeAmplitude = 5f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.15f;
        [SerializeField, Min(1f)] private float punchScaleY = 1.25f;
        [SerializeField, Min(1f)] private float punchReturnSpeed = 12f;
        [SerializeField, Min(1f)] private float flashFadeSpeed = 9f;

        [Header("Floating Damage Numbers")]
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private int damageNumberFontSize = 22;
        [SerializeField, Min(0.1f)] private float damageNumberLifetime = 0.65f;
        [SerializeField] private float damageNumberRiseSpeed = 55f;
        [SerializeField] private Color damageNumberColor = new(1f, 0.88f, 0.35f, 1f);

        [Header("Stamina UI (Placeholder)")]
        [SerializeField] private Image staminaFillImage;
        [SerializeField, Range(0f, 1f)] private float defaultStaminaNormalized = 1f;

        [Header("Ammunition UI")]
        [SerializeField] private Text ammoText;
        [SerializeField] private string noWeaponAmmoText = "-- / --";

        [Header("Damage Feedback")]
        [SerializeField] private CanvasGroup damageOverlayGroup;
        [SerializeField] private bool useProceduralVignette = true;
        [SerializeField] private Color vignetteColor = new(0.84f, 0.07f, 0.07f, 1f);
        [SerializeField, Range(0f, 1f)] private float damageFlashAlpha = 0.75f;
        [SerializeField, Min(0.1f)] private float damageFadeSpeed = 2.5f;
        [SerializeField] private AudioClip[] damageClips;
        [SerializeField, Range(0f, 1f)] private float damageSoundVolume = 0.8f;

        [Header("Game Over UI")]
        [SerializeField] private CanvasGroup gameOverGroup;
        [SerializeField, Min(0.1f)] private float gameOverFadeSpeed = 4f;

        private readonly List<FloatingDamageText> floatingNumbers = new();

        private Image damageChunkImage;
        private Sprite solidSprite;
        private Vector2 baseContainerPosition;
        private Vector3 baseContainerScale = Vector3.one;

        private float targetHealthNormalized = 1f;
        private float shownHealthNormalized = 1f;
        private float chunkHealthNormalized = 1f;
        private float chunkDelayTimer;
        private float shakeTimer;
        private float flashAmount;
        private float currentScaleY = 1f;
        private bool isGameOver;

        private void Awake()
        {
            solidSprite = CreateSolidSprite();
            SetupHealthBar();

            if (damageOverlayGroup != null)
            {
                damageOverlayGroup.alpha = 0f;
                damageOverlayGroup.interactable = false;
                damageOverlayGroup.blocksRaycasts = false;

                if (useProceduralVignette && damageOverlayGroup.TryGetComponent(out Image damageImage))
                {
                    damageImage.sprite = CreateVignetteSprite();
                    damageImage.type = Image.Type.Simple;
                    damageImage.color = Color.white;
                }
            }

            if (gameOverGroup != null)
            {
                gameOverGroup.alpha = 0f;
                gameOverGroup.interactable = false;
                gameOverGroup.blocksRaycasts = false;
            }

            if (staminaFillImage != null)
            {
                if (useCrispRectSprites)
                    ApplySolidFill(staminaFillImage);

                staminaFillImage.fillAmount = defaultStaminaNormalized;
            }
        }

        private void OnEnable()
        {
            if (player == null)
                return;

            player.Damaged += HandlePlayerDamaged;
            player.Died += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (player == null)
                return;

            player.Damaged -= HandlePlayerDamaged;
            player.Died -= HandlePlayerDied;
        }

        private void Start()
        {
            SyncHealthImmediate();
            UpdateAmmoUi();
        }

        private void Update()
        {
            UpdateJuicyHealthBar();
            UpdateFloatingNumbers();
            UpdateAmmoUi();
            UpdateDamageOverlay();
            UpdateGameOverScreen();
        }

        public void SetStamina(float current, float max)
        {
            if (staminaFillImage != null && max > 0f)
                staminaFillImage.fillAmount = Mathf.Clamp01(current / max);
        }

        private void SetupHealthBar()
        {
            if (healthFillImage == null)
                return;

            if (useCrispRectSprites)
                ApplySolidFill(healthFillImage);

            if (healthBarContainer == null)
                healthBarContainer = healthFillImage.rectTransform.parent as RectTransform;

            if (healthBarContainer != null)
            {
                baseContainerPosition = healthBarContainer.anchoredPosition;
                baseContainerScale = healthBarContainer.localScale;
            }

            CreateDamageChunkLayer();
        }

        private void CreateDamageChunkLayer()
        {
            if (healthFillImage == null)
                return;

            GameObject chunkObject = new("HealthDamageChunk", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform chunkRect = chunkObject.GetComponent<RectTransform>();
            RectTransform fillRect = healthFillImage.rectTransform;

            chunkRect.SetParent(fillRect.parent, false);
            chunkRect.SetSiblingIndex(fillRect.GetSiblingIndex());
            chunkRect.anchorMin = fillRect.anchorMin;
            chunkRect.anchorMax = fillRect.anchorMax;
            chunkRect.pivot = fillRect.pivot;
            chunkRect.anchoredPosition = fillRect.anchoredPosition;
            chunkRect.sizeDelta = fillRect.sizeDelta;

            damageChunkImage = chunkObject.GetComponent<Image>();
            damageChunkImage.sprite = healthFillImage.sprite != null ? healthFillImage.sprite : solidSprite;
            damageChunkImage.type = Image.Type.Filled;
            damageChunkImage.fillMethod = Image.FillMethod.Horizontal;
            damageChunkImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            damageChunkImage.fillAmount = 1f;
            damageChunkImage.color = damageChunkColor;
            damageChunkImage.raycastTarget = false;
        }

        private void HandlePlayerDamaged(Entity entity, DamageInfo damage)
        {
            targetHealthNormalized = GetNormalizedHealth();
            UpdateHealthText();

            chunkDelayTimer = chunkDelay;
            shakeTimer = shakeDuration;
            flashAmount = 1f;
            currentScaleY = punchScaleY;

            if (showDamageNumbers && damage.Amount > 0f)
                SpawnDamageNumber(damage.Amount);

            if (damageOverlayGroup != null)
                damageOverlayGroup.alpha = damageFlashAlpha;

            if (audioSource != null && damageClips != null && damageClips.Length > 0)
            {
                AudioClip clip = damageClips[Random.Range(0, damageClips.Length)];
                if (clip != null)
                    audioSource.PlayOneShot(clip, damageSoundVolume);
            }
        }

        private void HandlePlayerDied(Entity entity)
        {
            targetHealthNormalized = 0f;
            UpdateHealthText();
            isGameOver = true;
        }

        private void SyncHealthImmediate()
        {
            targetHealthNormalized = GetNormalizedHealth();
            shownHealthNormalized = targetHealthNormalized;
            chunkHealthNormalized = targetHealthNormalized;

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = shownHealthNormalized;
                healthFillImage.color = EvaluateHealthColor(shownHealthNormalized);
            }

            if (damageChunkImage != null)
                damageChunkImage.fillAmount = chunkHealthNormalized;

            UpdateHealthText();
        }

        private void UpdateJuicyHealthBar()
        {
            if (player == null)
                return;

            targetHealthNormalized = GetNormalizedHealth();

            float dt = Time.deltaTime;
            shownHealthNormalized = Mathf.Lerp(shownHealthNormalized, targetHealthNormalized, smoothDrainSpeed * dt);
            if (Mathf.Abs(shownHealthNormalized - targetHealthNormalized) < 0.001f)
                shownHealthNormalized = targetHealthNormalized;

            if (chunkHealthNormalized < shownHealthNormalized)
            {
                chunkHealthNormalized = shownHealthNormalized;
            }
            else if (chunkDelayTimer > 0f)
            {
                chunkDelayTimer -= dt;
            }
            else
            {
                chunkHealthNormalized = Mathf.Lerp(chunkHealthNormalized, shownHealthNormalized, chunkDrainSpeed * dt);
                if (Mathf.Abs(chunkHealthNormalized - shownHealthNormalized) < 0.001f)
                    chunkHealthNormalized = shownHealthNormalized;
            }

            if (flashAmount > 0f)
                flashAmount = Mathf.Max(0f, flashAmount - flashFadeSpeed * dt);

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = shownHealthNormalized;
                Color stateColor = EvaluateHealthColor(shownHealthNormalized);
                healthFillImage.color = Color.Lerp(stateColor, Color.white, flashAmount);
            }

            if (damageChunkImage != null)
                damageChunkImage.fillAmount = chunkHealthNormalized;

            UpdateHealthTextColor(shownHealthNormalized);
            UpdateContainerHitFeel(dt);
        }

        private void UpdateContainerHitFeel(float dt)
        {
            if (healthBarContainer == null)
                return;

            currentScaleY = Mathf.Lerp(currentScaleY, 1f, punchReturnSpeed * dt);
            healthBarContainer.localScale = new Vector3(
                baseContainerScale.x,
                baseContainerScale.y * currentScaleY,
                baseContainerScale.z);

            if (shakeTimer > 0f)
            {
                shakeTimer = Mathf.Max(0f, shakeTimer - dt);
                float strength = shakeAmplitude * (shakeTimer / Mathf.Max(0.001f, shakeDuration));
                Vector2 offset = Random.insideUnitCircle * strength;
                healthBarContainer.anchoredPosition = baseContainerPosition + offset;
            }
            else
            {
                healthBarContainer.anchoredPosition = baseContainerPosition;
            }
        }

        private void SpawnDamageNumber(float amount)
        {
            if (healthFillImage == null)
                return;

            RectTransform fillRect = healthFillImage.rectTransform;
            Transform parent = healthBarContainer != null ? healthBarContainer : fillRect.parent;
            if (parent == null)
                return;

            GameObject numberObj = new("DamageNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            RectTransform rect = numberObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            float width = fillRect.rect.width > 0f ? fillRect.rect.width : 240f;
            float xPos = (shownHealthNormalized - 0.5f) * width;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 40f);
            rect.anchoredPosition = new Vector2(xPos, fillRect.rect.height * 0.6f + 12f);

            Text label = numberObj.GetComponent<Text>();
            label.text = $"-{Mathf.CeilToInt(amount)}";
            label.font = healthText != null ? healthText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = damageNumberFontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = damageNumberColor;
            label.raycastTarget = false;

            Outline outline = numberObj.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            floatingNumbers.Add(new FloatingDamageText
            {
                Rect = rect,
                Label = label,
                RemainingTime = damageNumberLifetime,
                Duration = damageNumberLifetime,
                RiseSpeed = damageNumberRiseSpeed
            });
        }

        private void UpdateFloatingNumbers()
        {
            if (floatingNumbers.Count == 0)
                return;

            float dt = Time.deltaTime;
            for (int i = floatingNumbers.Count - 1; i >= 0; i--)
            {
                FloatingDamageText item = floatingNumbers[i];
                item.RemainingTime -= dt;

                if (item.RemainingTime <= 0f)
                {
                    if (item.Rect != null)
                        Destroy(item.Rect.gameObject);
                    floatingNumbers.RemoveAt(i);
                    continue;
                }

                item.Rect.anchoredPosition += Vector2.up * (item.RiseSpeed * dt);
                Color color = item.Label.color;
                color.a = Mathf.Clamp01(item.RemainingTime / item.Duration);
                item.Label.color = color;
            }
        }

        private float GetNormalizedHealth()
        {
            if (player == null || player.MaxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(player.CurrentHealth / player.MaxHealth);
        }

        private Color EvaluateHealthColor(float normalized)
        {
            if (normalized >= 0.5f)
                return Color.Lerp(midHealthColor, highHealthColor, (normalized - 0.5f) * 2f);

            return Color.Lerp(lowHealthColor, midHealthColor, normalized * 2f);
        }

        private void UpdateHealthText()
        {
            if (player == null || healthText == null)
                return;

            healthText.text = $"{Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.MaxHealth)}";
        }

        private void UpdateHealthTextColor(float normalized)
        {
            if (healthText == null)
                return;

            healthText.color = normalized <= 0.3f ? lowHealthColor : Color.black;
        }

        private void UpdateAmmoUi()
        {
            if (ammoText == null)
                return;

            if (weaponHandler != null && weaponHandler.CurrentWeapon is Firearm firearm)
                ammoText.text = $"{firearm.Ammunition} / {firearm.MagazineSize}";
            else
                ammoText.text = noWeaponAmmoText;
        }

        private void UpdateDamageOverlay()
        {
            if (damageOverlayGroup == null || damageOverlayGroup.alpha <= 0f)
                return;

            damageOverlayGroup.alpha = Mathf.MoveTowards(
                damageOverlayGroup.alpha,
                0f,
                damageFadeSpeed * Time.deltaTime);
        }

        private void UpdateGameOverScreen()
        {
            if (!isGameOver || gameOverGroup == null)
                return;

            gameOverGroup.alpha = Mathf.MoveTowards(
                gameOverGroup.alpha,
                1f,
                gameOverFadeSpeed * Time.deltaTime);
        }

        private void ApplySolidFill(Image image)
        {
            image.sprite = solidSprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private static Sprite CreateSolidSprite()
        {
            const int size = 4;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateVignetteSprite()
        {
            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = (y / (float)(size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)(size - 1)) * 2f - 1f;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float t = Mathf.Clamp01((dist - 0.45f) / 0.8f);
                    float alpha = t * t * (3f - 2f * t);

                    pixels[y * size + x] = new Color(
                        vignetteColor.r,
                        vignetteColor.g,
                        vignetteColor.b,
                        alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
