using UnityEngine;

namespace ActionRPG.Player
{
    [DisallowMultipleComponent]
    public class PlayerBlockVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private float blockAngle = 140f;
        [SerializeField] private float height = 0.08f;
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private int segments = 24;
        [SerializeField] private Color blockColor = new Color(0.1f, 0.75f, 1f, 0.8f);

        private LineRenderer lineRenderer;
        private PlayerBlockSystem blockSystem;
        private PlayerController playerController;
        private Vector3[] points;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateVisualsForPlayers()
        {
            PlayerBlockSystem[] blockSystems = Object.FindObjectsByType<PlayerBlockSystem>(FindObjectsSortMode.None);

            foreach (PlayerBlockSystem system in blockSystems)
            {
                if (system.GetComponentInChildren<PlayerBlockVisual>(true) != null)
                {
                    continue;
                }

                GameObject visualObject = new GameObject("BlockVisual");
                visualObject.transform.SetParent(system.transform, false);
                visualObject.AddComponent<PlayerBlockVisual>();
            }
        }

        private void Awake()
        {
            blockSystem = GetComponentInParent<PlayerBlockSystem>();
            playerController = blockSystem != null ? blockSystem.GetComponent<PlayerController>() : null;
            EnsureRenderer();
            lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (blockSystem == null)
            {
                blockSystem = GetComponentInParent<PlayerBlockSystem>();
                if (blockSystem == null)
                {
                    return;
                }
            }

            if (playerController == null)
            {
                playerController = blockSystem.GetComponent<PlayerController>();
            }

            Vector3 forward = playerController != null
                ? playerController.CurrentFacingDirection
                : blockSystem.transform.forward;

            SetVisible(blockSystem.IsBlocking, blockSystem.transform.position, forward);
        }

        public void SetVisible(bool visible, Vector3 origin, Vector3 forward)
        {
            EnsureRenderer();
            lineRenderer.enabled = visible;

            if (!visible)
            {
                return;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            points = BuildArcPoints(origin, forward, radius, blockAngle, height, segments);
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }

        public static Vector3[] BuildArcPoints(Vector3 origin, Vector3 forward, float radius, float angleDegrees, float height, int segments)
        {
            segments = Mathf.Max(1, segments);
            radius = Mathf.Max(0.01f, radius);

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            float halfAngle = angleDegrees * 0.5f;
            Vector3[] arcPoints = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                arcPoints[i] = origin + direction * radius + Vector3.up * height;
            }

            return arcPoints;
        }

        private void EnsureRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.numCapVertices = 4;
            lineRenderer.startColor = blockColor;
            lineRenderer.endColor = blockColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                lineRenderer.material = new Material(shader);
                if (lineRenderer.material.HasProperty("_BaseColor"))
                {
                    lineRenderer.material.SetColor("_BaseColor", blockColor);
                }
            }
        }
    }
}
