using ActionRPG.Combat;
using UnityEngine;

namespace ActionRPG.UI
{
    /// <summary>
    /// Simple world-space enemy health bar. Attach to an enemy and assign a
    /// child transform above the enemy as the bar position if desired.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
        [SerializeField] private Vector2 barSize = new Vector2(1.5f, 0.18f);

        [Header("Appearance")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.25f, 0.95f);
        [SerializeField] private Color borderColor = Color.black;
        [SerializeField] private float borderThickness = 0.03f;

        private Health health;
        private Camera mainCamera;
        private float healthPercent = 1f;
        private GUIStyle borderStyle;
        private GUIStyle backgroundStyle;
        private GUIStyle fillStyle;
        private Texture2D borderTexture;
        private Texture2D backgroundTexture;
        private Texture2D fillTexture;

        private void Awake()
        {
            health = GetComponent<Health>();
            healthPercent = health != null && health.MaxHealth > 0f
                ? health.CurrentHealth / health.MaxHealth
                : 1f;
            mainCamera = Camera.main;
            CreateStyles();
        }

        private void OnEnable()
        {
            if (health == null) health = GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthChanged += HandleHealthChanged;
                health.OnDeath += HandleDeath;
                HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
                health.OnDeath -= HandleDeath;
            }
        }

        private void OnGUI()
        {
            if (health == null || !health.IsAlive) return;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(transform.position + worldOffset);
            if (screenPosition.z <= 0f) return;

            float width = barSize.x * 100f;
            float height = barSize.y * 100f;
            Rect borderRect = new Rect(
                screenPosition.x - width * 0.5f,
                Screen.height - screenPosition.y - height * 0.5f,
                width,
                height);

            GUI.DrawTexture(borderRect, borderTexture);

            Rect innerRect = new Rect(
                borderRect.x + borderThickness * 100f,
                borderRect.y + borderThickness * 100f,
                Mathf.Max(0f, borderRect.width - borderThickness * 200f),
                Mathf.Max(0f, borderRect.height - borderThickness * 200f));

            GUI.DrawTexture(innerRect, backgroundTexture);

            float fillWidth = innerRect.width * Mathf.Clamp01(healthPercent);
            if (fillWidth > 0f)
            {
                GUI.DrawTexture(new Rect(innerRect.x, innerRect.y, fillWidth, innerRect.height), fillTexture);
            }
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            healthPercent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        }

        private void HandleDeath()
        {
            enabled = false;
        }

        private void CreateStyles()
        {
            borderTexture = CreateTexture(borderColor);
            backgroundTexture = CreateTexture(backgroundColor);
            fillTexture = CreateTexture(fillColor);
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            DestroyTexture(borderTexture);
            DestroyTexture(backgroundTexture);
            DestroyTexture(fillTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null) return;

            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
    }
}
