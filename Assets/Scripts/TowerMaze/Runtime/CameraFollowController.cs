using UnityEngine;

namespace TowerMaze
{
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private float verticalOffset = 1.7f;
        [SerializeField] private float outwardDistance = 6.6f;
        [SerializeField] private float sideOffset = 0.6f;
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float lookAtHeight = 0.7f;
        [SerializeField] private float upwardLookAhead = 1.2f;
        [SerializeField] private float inwardLookOffset = 1.5f;
        [SerializeField] private float rotationLerpSpeed = 10f;
        [Header("Portrait Framing")]
        [SerializeField] private float portraitVerticalOffset = 2.1f;
        [SerializeField] private float portraitOutwardDistance = 8.5f;
        [SerializeField] private float portraitSideOffset = 0.18f;
        [SerializeField] private float portraitLookAtHeight = 0.9f;
        [SerializeField] private float portraitUpwardLookAhead = 1.45f;
        [SerializeField] private float portraitInwardLookOffset = 1.2f;
        [SerializeField] private float landscapeFieldOfView = 54f;
        [SerializeField] private float portraitFieldOfView = 62f;

        private Transform target;
        private Vector3 velocity;
        private Camera attachedCamera;
        private float shakeDuration;
        private float shakeMagnitude;
        private float shakeElapsed;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
            attachedCamera ??= GetComponent<Camera>();
            ForceSnap();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = GetDesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime);

            Vector3 lookTarget = GetLookTarget();
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerpSpeed * Time.deltaTime);
            ApplyLens();

            if (shakeElapsed < shakeDuration)
            {
                shakeElapsed += Time.deltaTime;
                float t = 1f - shakeElapsed / shakeDuration;
                transform.position += (Vector3)(Random.insideUnitCircle * (shakeMagnitude * t));
            }
        }

        public void Shake(float duration, float magnitude)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
            shakeElapsed = 0f;
        }

        public void ForceSnap()
        {
            if (target == null)
            {
                return;
            }

            transform.position = GetDesiredPosition();
            transform.rotation = Quaternion.LookRotation(GetLookTarget() - transform.position, Vector3.up);
            ApplyLens();
        }

        private Vector3 GetDesiredPosition()
        {
            float portraitBlend = GetPortraitBlend();
            Vector3 outward = GetOutward();
            Vector3 sideways = Vector3.Cross(Vector3.up, outward).normalized;
            float blendedVerticalOffset = Mathf.Lerp(verticalOffset, portraitVerticalOffset, portraitBlend);
            float blendedOutwardDistance = Mathf.Lerp(outwardDistance, portraitOutwardDistance, portraitBlend);
            float blendedSideOffset = Mathf.Lerp(sideOffset, portraitSideOffset, portraitBlend);
            return target.position + (Vector3.up * blendedVerticalOffset) + (outward * blendedOutwardDistance) + (sideways * blendedSideOffset);
        }

        private Vector3 GetLookTarget()
        {
            float portraitBlend = GetPortraitBlend();
            Vector3 outward = GetOutward();
            float blendedLookAtHeight = Mathf.Lerp(lookAtHeight, portraitLookAtHeight, portraitBlend);
            float blendedUpwardLookAhead = Mathf.Lerp(upwardLookAhead, portraitUpwardLookAhead, portraitBlend);
            float blendedInwardLookOffset = Mathf.Lerp(inwardLookOffset, portraitInwardLookOffset, portraitBlend);
            return target.position + (Vector3.up * blendedLookAtHeight) + (Vector3.up * blendedUpwardLookAhead) - (outward * blendedInwardLookOffset);
        }

        private Vector3 GetOutward()
        {
            Vector3 outward = -target.forward;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = Vector3.back;
            }

            outward.Normalize();
            return outward;
        }

        private void ApplyLens()
        {
            if (attachedCamera == null)
            {
                return;
            }

            attachedCamera.fieldOfView = Mathf.Lerp(landscapeFieldOfView, portraitFieldOfView, GetPortraitBlend());
        }

        private static float GetPortraitBlend()
        {
            float aspect = Mathf.Max(0.01f, (float)Screen.width / Mathf.Max(1f, Screen.height));
            return Mathf.Clamp01(Mathf.InverseLerp(1.15f, 0.68f, aspect));
        }
    }
}
