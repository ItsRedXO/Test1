using ActionRPG.Combat;
using UnityEngine;

namespace ActionRPG.UI
{
    /// <summary>
    /// Simple screen-space enemy health bar driven directly by the existing Health component.
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
        private UnityEngine.Camera mainCamera;
        private Texture2D borderTexture;
        private Texture2D backgroundTexture;
        private Texture2D fillTexture;

        private void Awake()
        {
            health = GetComponent<Health>();
            mainCamera = UnityEngine.Camera.main;
            CreateTextures();
        }

        private void OnGUI()
        {
            if (health == null) health = GetComponent<Health>();
            if (health == null || !health.IsAlive || health.MaxHealth <= 0f) return;

            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(transform.position + worldOffset);
            if (screenPosition.z <= 0f) return;

            float healthPercent = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);

            float width = barSize.x * 100f;
            float height = barSize.y * 100f;
            Rect borderRect = new Rect(
                screenPosition.x - width * 0.5f,
                Screen.height - screenPosition.y - height * 0.5f,
                width,
                height);

            GUI.DrawTexture(borderRect, borderTexture);

            float borderPixels = borderThickness * 100f;
            Rect innerRect = new Rect(
                borderRect.x + borderPixels,
                borderRect.y + borderPixels,
                Mathf.Max(0f, borderRect.width - borderPixels * 2f),
                Mathf.Max(0f, borderRect.height - borderPixels * 2f));

            GUI.DrawTexture(innerRect, backgroundTexture);

            float fillWidth = innerRect.width * healthPercent;
            if (fillWidth > 0f)
            {
                GUI.DrawTexture(new Rect(innerRect.x, innerRect.y, fillWidth, innerRect.height), fillTexture);
            }
        }

        private void CreateTextures()
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
