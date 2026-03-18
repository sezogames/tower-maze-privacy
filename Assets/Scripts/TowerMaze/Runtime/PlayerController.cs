using UnityEngine;

namespace TowerMaze
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private HeroVisualController heroVisual;

        private GameConfig config;
        private TowerGenerator towerGenerator;
        private AudioManager audioManager;
        private CameraFollowController cameraFollowController;
        private Transform towerReference;
        private float angleAroundTower;
        private float heightOnTower;
        private float lastStableHeight;
        private float blockedFeedbackCooldown;
        private float horizontalSpeedMultiplier = 1f;
        private float climbSpeedMultiplier = 1f;
        private bool initialized;

        public float AngleAroundTower => angleAroundTower;
        public float HeightOnTower => heightOnTower;
        public float LastStableHeight => lastStableHeight;
        public bool HasStartIntent => inputHandler != null && inputHandler.HasStartIntent;
        public float BottomHeightOnTower => heightOnTower - HeroVisualController.BallRadius;
        public float WorldBottomHeight => transform.position.y - HeroVisualController.BallRadius;

        public void Initialize(GameConfig gameConfig, TowerGenerator generator, Transform towerTransform, ThemeDefinition theme, AudioManager runAudioManager, CameraFollowController cameraFollow = null)
        {
            config = gameConfig;
            towerGenerator = generator;
            towerReference = towerTransform;
            audioManager = runAudioManager;
            cameraFollowController = cameraFollow;
            inputHandler ??= GetComponent<PlayerInputHandler>();
            heroVisual ??= GetComponentInChildren<HeroVisualController>();
            inputHandler.Initialize(gameConfig);

            if (heroVisual != null)
            {
                heroVisual.Initialize(theme);
            }

            initialized = true;
        }

        public void SetPosition(float angleDegrees, float towerHeight)
        {
            angleAroundTower = Mathf.Repeat(angleDegrees, 360f);
            heightOnTower = Mathf.Max(0f, towerHeight);
            lastStableHeight = Mathf.Max(lastStableHeight, heightOnTower);
            SyncTransform();
        }

        public void ResetRunPosition(float angleDegrees, float towerHeight)
        {
            angleAroundTower = Mathf.Repeat(angleDegrees, 360f);
            heightOnTower = Mathf.Max(0f, towerHeight);
            lastStableHeight = heightOnTower;
            SyncTransform();
        }

        public void Tick(bool canMove)
        {
            if (!initialized || towerReference == null || config == null)
            {
                return;
            }

            float previousAngle = angleAroundTower;
            float previousHeight = heightOnTower;

            if (canMove)
            {
                ApplyMovement(Time.deltaTime);
            }
            else
            {
                blockedFeedbackCooldown = Mathf.Max(0f, blockedFeedbackCooldown - Time.deltaTime);
            }

            SyncTransform();

            if (heroVisual != null)
            {
                heroVisual.Tick(
                    canMove && inputHandler != null && inputHandler.ClimbHeld,
                    inputHandler != null ? inputHandler.HorizontalInput : 0f,
                    Mathf.DeltaAngle(previousAngle, angleAroundTower),
                    heightOnTower - previousHeight,
                    config.HeroLaneRadius);
            }
        }

        public void SetHeat(float intensity)
        {
            heroVisual?.SetHeat(intensity);
        }

        public void ApplySkin(BallSkinDefinition skin)
        {
            heroVisual?.ApplySkin(skin);
        }

        public void SetControlsFlipped(bool flipped)
        {
            inputHandler?.SetControlsFlipped(flipped);
        }

        public void SetMovementMultipliers(float horizontalMultiplier, float climbMultiplier)
        {
            horizontalSpeedMultiplier = Mathf.Max(0.35f, horizontalMultiplier);
            climbSpeedMultiplier = Mathf.Max(0.35f, climbMultiplier);
        }

        public void LiftToSafety(float liftHeight)
        {
            float targetHeight = Mathf.Max(heightOnTower + liftHeight, lastStableHeight);
            if (towerGenerator.TryFindOpenPosition(angleAroundTower, targetHeight, out float safeAngle, out float safeHeight))
            {
                angleAroundTower = safeAngle;
                heightOnTower = safeHeight;
            }
            else
            {
                heightOnTower = targetHeight;
            }

            lastStableHeight = Mathf.Max(lastStableHeight, heightOnTower);
            SyncTransform();
        }

        private void ApplyMovement(float deltaTime)
        {
            if (inputHandler == null || towerGenerator == null)
            {
                return;
            }

            float horizontalDelta = inputHandler.HorizontalInput * config.horizontalSpeedDegrees * horizontalSpeedMultiplier * deltaTime;
            float verticalDelta = 0f;
            blockedFeedbackCooldown = Mathf.Max(0f, blockedFeedbackCooldown - deltaTime);

            if (inputHandler.ClimbHeld)
            {
                verticalDelta = inputHandler.VerticalInput * config.climbSpeed * climbSpeedMultiplier * deltaTime;
            }

            bool horizontalMoved = TryMoveIncremental(horizontalDelta, 0f);
            bool verticalMoved = TryMoveIncremental(0f, verticalDelta);

            if (!horizontalMoved && Mathf.Abs(inputHandler.HorizontalInput) > 0.05f)
            {
                TriggerBlockedFeedback(Mathf.Sign(inputHandler.HorizontalInput));
            }
            else if (!verticalMoved && inputHandler.ClimbHeld && Mathf.Abs(inputHandler.VerticalInput) > 0.01f)
            {
                TriggerBlockedFeedback(0f);
            }
            else if (horizontalMoved || verticalMoved)
            {
                ApplyCorridorCentering(deltaTime);
            }

            lastStableHeight = Mathf.Max(lastStableHeight, heightOnTower);
        }

        private bool TryMove(float targetAngle, float targetHeight)
        {
            float angleClearance = config.AnglePerCell * config.heroCollisionWidthCells * 0.46f;
            float heightClearance = config.CellHeight * config.heroCollisionHeightCells * 0.46f;
            if (!towerGenerator.IsPathOpen(targetAngle, targetHeight, angleClearance, heightClearance))
            {
                return false;
            }

            angleAroundTower = Mathf.Repeat(targetAngle, 360f);
            heightOnTower = Mathf.Max(0f, targetHeight);
            return true;
        }

        private bool TryMoveIncremental(float angleDelta, float heightDelta)
        {
            if (Mathf.Approximately(angleDelta, 0f) && Mathf.Approximately(heightDelta, 0f))
            {
                return true;
            }

            float angleStepLimit = Mathf.Max(0.5f, config.AnglePerCell * 0.35f);
            float heightStepLimit = Mathf.Max(0.02f, config.CellHeight * 0.35f);
            int steps = Mathf.Max(
                1,
                Mathf.CeilToInt(Mathf.Max(
                    Mathf.Abs(angleDelta) / angleStepLimit,
                    Mathf.Abs(heightDelta) / heightStepLimit)));

            float stepAngle = angleDelta / steps;
            float stepHeight = heightDelta / steps;
            bool moved = false;

            for (int i = 0; i < steps; i++)
            {
                if (!TryMove(angleAroundTower + stepAngle, heightOnTower + stepHeight))
                {
                    return moved;
                }

                moved = true;
            }

            return moved;
        }

        private void TriggerBlockedFeedback(float horizontalDirection)
        {
            if (blockedFeedbackCooldown > 0f)
            {
                return;
            }

            blockedFeedbackCooldown = 0.12f;
            heroVisual?.TriggerWallBump(horizontalDirection);
            audioManager?.PlayWallBump();
            cameraFollowController?.Shake(0.08f, 0.035f);
        }

        private void ApplyCorridorCentering(float deltaTime)
        {
            if (inputHandler == null || towerGenerator == null)
            {
                return;
            }

            if (!towerGenerator.TryGetOpenCellCenter(angleAroundTower, heightOnTower, out float centerAngle, out float centerHeight))
            {
                return;
            }

            float horizontalAbs = Mathf.Abs(inputHandler.HorizontalInput);
            float verticalAbs = inputHandler.ClimbHeld ? Mathf.Abs(inputHandler.VerticalInput) : 0f;

            if (verticalAbs >= horizontalAbs)
            {
                angleAroundTower = Mathf.MoveTowardsAngle(
                    angleAroundTower,
                    centerAngle,
                    config.horizontalSpeedDegrees * horizontalSpeedMultiplier * 0.18f * deltaTime);
                return;
            }

            heightOnTower = Mathf.MoveTowards(
                heightOnTower,
                centerHeight,
                config.climbSpeed * climbSpeedMultiplier * 0.2f * deltaTime);
        }

        private void SyncTransform()
        {
            float radius = config.HeroLaneRadius;
            float radians = angleAroundTower * Mathf.Deg2Rad;
            Vector3 localPosition = new(Mathf.Sin(radians) * radius, heightOnTower, Mathf.Cos(radians) * radius);
            Vector3 outward = new(localPosition.x, 0f, localPosition.z);
            outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;

            transform.position = towerReference.TransformPoint(localPosition);
            transform.rotation = towerReference.rotation * Quaternion.LookRotation(-outward, Vector3.up);
        }
    }
}
