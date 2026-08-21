using UnityEngine;

namespace ActionRPG.Camera
{
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Camera Positioning")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

        private Vector3 currentVelocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

            Vector3 focusPoint = target.position + lookAtOffset;
            transform.rotation = Quaternion.LookRotation(focusPoint - transform.position);
        }
    }
}
