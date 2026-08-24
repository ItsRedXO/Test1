using System;
using System.Collections.Generic;
using ActionRPG.Combat;
using UnityEngine;

namespace ActionRPG.UI
{
    /// <summary>
    /// Simple screen-space enemy health bar. It follows the Health component
    /// that is actually receiving damage, which also handles enemies with
    /// multiple colliders or nested Health components.
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

        private readonly List<Health> healthSources = new List<Health>();
        private readonly Dictionary<Health, Action<float, float>> healthChangedHandlers = new Dictionary<Health, Action<float, float>>();
        private readonly Dictionary<Health, Action> deathHandlers = new Dictionary<Health, Action>();

        private Health displayHealth;
        private UnityEngine.Camera mainCamera;
        private Texture2D borderTexture;
        private Texture2D backgroundTexture;
        private Texture2D fillTexture;
        private bool died;

        private void Awake()
        {
            mainCamera = UnityEngine.Camera.main;
            CreateTextures();
        }

        private void OnEnable()
        {
            BindHealthSources();
        }

        private void Start()
        {
            if (healthSources.Count == 0)
            {
                BindHealthSources();
            }

            if (displayHealth == null)
            {
                displayHealth = GetComponent<Health>();
            }
        }

        private void OnDisable()
        {
            UnbindHealthSources();
        }

        private void BindHealthSources()
        {
            UnbindHealthSources();

            // Include the enemy root first, then any nested Health components.
            Health rootHealth = GetComponent<Health>();
            if (rootHealth != null)
            {
                AddHealthSource(rootHealth);
            }

            foreach (Health health in GetComponentsInChildren<Health>(true))
            {
                AddHealthSource(health);
            }

            if (displayHealth == null && healthSources.Count > 0)
            {
                displayHealth = healthSources[0];
            }
        }

        private void AddHealthSource(Health source)
        {
            if (source == null || healthSources.Contains(source)) return;

            healthSources.Add(source);

            Action<float, float> healthChangedHandler = (current, max) =>
            {
                displayHealth = source;
                died = false;
            };

            Action deathHandler = () =>
            {
                if (displayHealth == source)
                {
                    died = true;
                }
            };

            healthChangedHandlers[source] = healthChangedHandler;
            deathHandlers[source] = deathHandler;
            source.OnHealthChanged += healthChangedHandler;
            source.OnDeath += deathHandler;
        }

        private void UnbindHealthSources()
        {
            foreach (Health source in healthSources)
            {
                if (source == null) continue;

                if (healthChangedHandlers.TryGetValue(source, out Action<float, float> healthChangedHandler))
                {
                    source.OnHealthChanged -= healthChangedHandler;
                }

                if (deathHandlers.TryGetValue(source, out Action deathHandler))
                {
                    source.OnDeath -= deathHandler;
                }
            }

            healthSources.Clear();
            healthChangedHandlers.Clear();
            deathHandlers.Clear();
        }

        private void OnGUI()
        {
            if (displayHealth == null || died || !displayHealth.IsAlive || displayHealth.MaxHealth <= 0f) return;

            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(transform.position + worldOffset);
            if (screenPosition.z <= 0f) return;

            float healthPercent = Mathf.Clamp01(displayHealth.CurrentHealth / displayHealth.MaxHealth);

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
