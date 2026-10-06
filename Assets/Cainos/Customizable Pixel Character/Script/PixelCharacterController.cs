using Cainos.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cainos.CustomizablePixelCharacter
{
    // 2.5D: sprite billboard en mundo 3D, movimiento libre en XZ relativo a una cámara de ángulo fijo.
    // Motor: CharacterController. inputMove.x = horizontal de pantalla, inputMove.y = profundidad. inputTarget = punto del ratón en el suelo.
    [RequireComponent(typeof(CharacterController))]
    public class PixelCharacterController : MonoBehaviour
    {
        #region - STATIC PARAMETERS -

        private static readonly float PIXEL_SIZE = 0.03125f;
        private static readonly float CENTER_HEIGHT = PIXEL_SIZE * 30;
        private static readonly float MOVE_THRESHOLD = 0.05f;
        private static readonly float LAND_EVENT_THRESHOLD = 3.0f;

        #endregion

        #region - PARAMETERS -

        [FoldoutGroup("Movement")] public LayerMask groundCheckLayerMask;             // capas del entorno (para space checks / apuntado auxiliar). NO la del jugador
        [FoldoutGroup("Movement")] public Camera cam;                                 // null = Camera.main
        [Space]
        [FoldoutGroup("Movement")] public float walkSpeedMax = 2.5f;
        [FoldoutGroup("Movement")] public float walkAcc = 10.0f;
        [Space]
        [FoldoutGroup("Movement")] public float runSpeedMax = 5.0f;
        [FoldoutGroup("Movement")] public float runAcc = 15.0f;
        [Space]
        [FoldoutGroup("Movement")] public float crouchSpeedMax = 1.0f;
        [FoldoutGroup("Movement")] public float crouchAcc = 8.0f;
        [Space]
        [FoldoutGroup("Movement")] public float crawlSpeedMax = 1.0f;
        [FoldoutGroup("Movement")] public float crawlAcc = 8.0f;
        [Space]
        [FoldoutGroup("Movement")] public float airSpeedMax = 2.0f;
        [FoldoutGroup("Movement")] public float airAcc = 8.0f;
        [Space]
        [FoldoutGroup("Movement")] public float groundDrag = 10.0f;
        [FoldoutGroup("Movement")] public float airDrag = 0.75f;
        [Space]
        [FoldoutGroup("Movement")] public float gravity = -20.0f;                     // gravedad manual (m/s^2)
        [FoldoutGroup("Movement")] public float maxSlopeAngle = 46.0f;               // se copia a CharacterController.slopeLimit en Awake

        [FoldoutGroup("Collider")] public float standHeight = 1.9f;
        [FoldoutGroup("Collider")] public float crouchHeight = 1.2f;
        [FoldoutGroup("Collider")] public float crawlHeight = 0.6f;

        [FoldoutGroup("Billboard")] public Transform billboardTarget;                 // null = este transform
        [FoldoutGroup("Billboard")] public float billboardYawOffset = 0.0f;

        [FoldoutGroup("Attack")] public AttackActionType attackAction = AttackActionType.Swipe;
        [FoldoutGroup("Attack")] public AttackActionMeleeType attackActionMelee = AttackActionMeleeType.Swipe;
        [Space]
        [FoldoutGroup("Attack")] public float attackSpeedMul = 1.0f;
        [FoldoutGroup("Attack")] public float attackCooldown = 0.0f;
        [Space]
        [FoldoutGroup("Attack")] public float throwForce = 10.0f;
        [FoldoutGroup("Attack")] public float throwAngularSpeed = 200.0f;              // grados/s
        [Space]
        [FoldoutGroup("Attack")] public float projectileSpeed = 20.0f;
        [FoldoutGroup("Attack")] public GameObject projectilePrefab;

        #endregion

        #region - EVENTS -

        [FoldoutGroup("Event")] public UnityEvent onFootstep;
        [FoldoutGroup("Event")] public UnityEvent onLand;
        [FoldoutGroup("Event")] public UnityEvent onAttackStart;
        [FoldoutGroup("Event")] public UnityEvent onAttackHit;
        [FoldoutGroup("Event")] public UnityEvent onAttackCast;
        [FoldoutGroup("Event")] public UnityEvent onAttackEnd;
        [FoldoutGroup("Event")] public UnityEvent onBowPull;
        [FoldoutGroup("Event")] public UnityEvent onBowShoot;
        [FoldoutGroup("Event")] public UnityEvent onThrow;

        // compatibilidad con otros scripts de Cainos (PixelCharacterAudio, etc.): ya no se invocan
        [FoldoutGroup("Event (unused)")] public UnityEvent onJump;
        [FoldoutGroup("Event (unused)")] public UnityEvent onSwimHand;
        [FoldoutGroup("Event (unused)")] public UnityEvent onSwimFoot;
        [FoldoutGroup("Event (unused)")] public UnityEvent onDodgeStart;
        [FoldoutGroup("Event (unused)")] public UnityEvent onDodgeEnd;
        [FoldoutGroup("Event (unused)")] public UnityEvent onBubble;

        #endregion

        #region - RUNTIME INPUT -

        [FoldoutGroup("Input"), DisableInEditMode] public Vector3 inputMove = Vector3.zero;     // x: horizontal, y: profundidad
        [FoldoutGroup("Input"), DisableInEditMode] public bool inputMoveFast = false;
        [FoldoutGroup("Input"), DisableInEditMode] public bool inputCrounch = false;
        [FoldoutGroup("Input"), DisableInEditMode] public bool inputCrawl = false;
        [FoldoutGroup("Input"), DisableInEditMode] public bool inputAttack = false;
        [FoldoutGroup("Input"), DisableInEditMode] public bool inputMelee = false;
        [FoldoutGroup("Input"), DisableInEditMode] public bool inputLook = false;
        [FoldoutGroup("Input"), DisableInEditMode] public Vector3 inputTarget = Vector3.zero;   // punto en el suelo

        #endregion

        #region - DEBUG -
#if UNITY_EDITOR
        [FoldoutGroup("Debug"), LabelText("Draw Velocity")] public bool debug_drawVelocity;
        [FoldoutGroup("Debug"), LabelText("Draw Aim")] public bool debug_drawAim;
#endif
        #endregion

        #region - PRIVATE -

        private PixelCharacter character;
        private Animator animator;
        private CharacterController cc;
        private readonly List<Collider> selfColliders = new();
        private readonly Collider[] overlapBuffer = new Collider[16];

        private float idleTimer;
        private float airTimer;

        private Vector3 horizontalVel;      // velocidad en XZ (world)
        private float verticalVel;          // velocidad en Y (gravedad)

        #endregion

        #region - CAMERA HELPERS -

        private Camera Cam => cam != null ? cam : Camera.main;

        private Vector3 CamRight => Flatten(Cam ? Cam.transform.right : Vector3.right, Vector3.right);
        private Vector3 CamForward => Flatten(Cam ? Cam.transform.forward : Vector3.forward, Vector3.forward);

        private static Vector3 Flatten(Vector3 v, Vector3 fallback)
        {
            v.y = 0.0f;
            return v.sqrMagnitude < 0.0001f ? fallback : v.normalized;
        }

        // dirección de movimiento en mundo a partir del input
        private Vector3 MoveWorldDir
        {
            get
            {
                Vector3 dir = CamRight * inputMove.x + CamForward * inputMove.y;
                return Vector3.ClampMagnitude(dir, 1.0f);
            }
        }

        // proyecta un punto del mundo al plano del sprite (camRight/up) usando su dirección en pantalla
        private Vector3 ToSpritePlane(Vector3 worldTarget, Vector3 pivot)
        {
            Camera c = Cam;
            Vector3 dir;
            if (c == null) dir = worldTarget - pivot;
            else
            {
                Vector3 s = c.WorldToScreenPoint(worldTarget) - c.WorldToScreenPoint(pivot);
                dir = CamRight * s.x + Vector3.up * s.y;
            }
            if (dir.sqrMagnitude < 0.0001f) dir = CamRight * facingDir;
            return pivot + dir.normalized * 10.0f;
        }

        #endregion

        #region - RUNTIME PROPERTIES -

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsIdle => !isMoving && !IsInAir;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsInAir => !isGrounded;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public float AirTimer => airTimer;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsGrounded
        {
            get { return isGrounded; }
            private set
            {
                if (isGrounded == value) return;
                isGrounded = value;
                if (isGrounded && verticalVel < -LAND_EVENT_THRESHOLD) onLand.Invoke();
            }
        }
        private bool isGrounded;

        #endregion

        #region - ATTACK -

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsDrawingBow
        {
            get { return isDrawingBow; }
            set
            {
                if (isDrawingBow == value) return;

                isDrawingBow = value;
                if (attackCooldownTimer > 0.00f) isDrawingBow = false;

                if (isDrawingBow == false)
                {
                    if (isArrowReady && projectile)
                    {
                        if (isCrawling || isDead)
                        {
                            Destroy(projectile.gameObject);
                        }
                        else
                        {
                            if (attackCooldown > 0.01f) attackCooldownTimer = attackCooldown;

                            projectile.transform.SetParent(null, true);
                            projectile.transform.localScale = Vector3.one;
                            projectile.transform.rotation = FlatRotation(PointAtTargetDirection);
                            projectile.IsLaunched = true;
                            projectile.Velocity = projectileSpeed * projectile.transform.right;

                            onBowShoot.Invoke();
                        }
                    }

                    isArrowReady = false;
                    IsStringPulled = false;
                }

                animator.SetBool("IsDrawingBow", isDrawingBow);
            }
        }
        private bool isDrawingBow;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsStringPulled
        {
            get { return isStringPulled; }
            set
            {
                if (isStringPulled == value) return;
                isStringPulled = value;
                if (isDrawingBow == false) isStringPulled = false;
                if (weaponBow) weaponBow.IsStringPulled = isStringPulled;
            }
        }
        private bool isStringPulled;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsArrowDrawn
        {
            get { return isArrowDrawn; }
            set
            {
                if (isArrowDrawn == value) return;
                isArrowDrawn = value;
                animator.SetBool("IsArrowDrawn", isArrowDrawn);
                animator.SetFloat("AttackSpeedMul", attackSpeedMul);
            }
        }
        private bool isArrowDrawn;

        private Projectile projectile;
        private WeaponBow weaponBow;
        private bool isArrowReady;
        private bool isAttacking;
        private int attackActionIndex;
        private float attackCooldownTimer = -1.0f;

        // rotación en Y que alinea transform.right con la dirección dada (plano XZ)
        private static Quaternion FlatRotation(Vector3 dir)
        {
            dir.y = 0.0f;
            return Quaternion.Euler(0.0f, Vector3.SignedAngle(Vector3.right, dir, Vector3.up), 0.0f);
        }

        public void OnArrowDraw()
        {
            if (weaponBow == null) return;
            if (projectilePrefab == null) return;
            if (isDrawingBow == false) return;

            projectile = Instantiate(projectilePrefab, character.rigHandL).GetComponent<Projectile>();
            projectile.transform.localPosition = Vector3.zero;
            projectile.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);

            IsArrowDrawn = true;
        }

        public void OnArrowNock()
        {
            IsStringPulled = true;
            onBowPull.Invoke();
        }

        public void OnArrowReady()
        {
            if (isDrawingBow) isArrowReady = true;
        }

        public void OnArrowPutBack()
        {
            IsArrowDrawn = false;
            if (projectile)
            {
                Destroy(projectile.gameObject);
                projectile = null;
            }
        }

        public void OnThrow()
        {
            if (character.Weapon == null) return;

            var weaponGO = character.DetachWeapon();
            if (weaponGO == null) return;

            if (weaponGO.TryGetComponent<Rigidbody>(out var weaponRb))
            {
                weaponRb.linearVelocity = cc.velocity;
                weaponRb.angularVelocity = CamForward * (-facingDir * throwAngularSpeed * Mathf.Deg2Rad);
                weaponRb.AddForce(PointAtTargetDirection * throwForce);
            }

            onThrow.Invoke();
        }

        public void OnAttackCast()
        {
            onAttackCast.Invoke();

            if (character.Weapon == null) return;
            if (projectilePrefab == null) return;

            projectile = Instantiate(projectilePrefab).GetComponent<Projectile>();
            projectile.transform.position = character.Weapon.TipPosition;

            Vector3 dir = IsPointingAtTarget ? PointAtTargetDirection : CamRight * facingDir;
            projectile.transform.rotation = FlatRotation(dir);
            projectile.Velocity = projectileSpeed * projectile.transform.right;
            projectile.IsLaunched = true;
        }

        public void OnAttackStart() { onAttackStart.Invoke(); }
        public void OnAttackHit() { onAttackHit.Invoke(); }

        public void OnAttackEnd()
        {
            onAttackEnd.Invoke();
            if (attackCooldown > 0.01f) attackCooldownTimer = attackCooldown;
        }

        public void ArcheryUpdate()
        {
            if (weaponBow && IsStringPulled) weaponBow.StringPullPos = character.rigHandL.position;
        }

        private void AttackUpdate()
        {
            attackActionIndex = inputMelee ? (int)attackActionMelee : (int)attackAction;

            if (attackAction == AttackActionType.Archery)
            {
                if (isCrawling == false && character.Weapon && character.Weapon.TryGetComponent<WeaponBow>(out weaponBow))
                {
                    IsDrawingBow = inputAttack;
                }
                else if (isCrawling)
                {
                    attackActionIndex = (int)attackActionMelee;
                }
            }
            else if (attackAction == AttackActionType.PointAtTarget || attackAction == AttackActionType.Summon || attackAction == AttackActionType.Throw)
            {
                if (isCrawling) attackActionIndex = (int)attackActionMelee;
            }

            isAttacking = inputAttack || inputMelee;

            if (attackCooldownTimer > 0.0f)
            {
                attackCooldownTimer -= Time.deltaTime;
                isAttacking = false;
            }

            animator.SetInteger("AttackAction", attackActionIndex);
            animator.SetBool("IsAttacking", isAttacking);
            animator.SetFloat("AttackSpeedMul", attackSpeedMul);
        }

        #endregion

        #region - SPACE CHECKS -

        // ¿cabe una cápsula de esta altura en la posición actual?
        private bool HasSpace(float height)
        {
            float r = cc.radius * 0.95f;
            Vector3 basePos = transform.position + Vector3.up * (cc.center.y - cc.height * 0.5f);
            Vector3 bottom = basePos + Vector3.up * (r + 0.1f);
            Vector3 top = basePos + Vector3.up * Mathf.Max(height - r, r + 0.1f);

            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlapBuffer, groundCheckLayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!selfColliders.Contains(overlapBuffer[i])) return false;
            }
            return true;
        }

        #endregion

        #region - CROUCH -

        public bool CanExitCrouching => !isCrouching || HasSpace(standHeight);

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsCrouching => isCrouching;
        private bool isCrouching;

        private void Crouch()
        {
            bool shouldCrouch = inputCrounch;
            if (airTimer > 1.0f) shouldCrouch = false;
            if (isCrouching) shouldCrouch = shouldCrouch || !CanExitCrouching;
            isCrouching = shouldCrouch;
        }

        #endregion

        #region - CRAWL -

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsCrawling
        {
            get { return isCrawling; }
            set
            {
                isCrawling = value;
                if (isCrawling == false)
                {
                    isCrawlEntering = false;
                    isCrawlExiting = false;
                }
            }
        }
        private bool isCrawling;
        private bool isCrawlEntering;
        private bool isCrawlExiting;

        // hay sitio a los lados (en el plano del sprite) para tumbarse
        public bool CanEnterCrawling
        {
            get
            {
                if (isCrawling) return true;

                Vector3 origin = transform.position + Vector3.up * (16 * PIXEL_SIZE);
                float dis = 0.0f;
                dis += Physics.Raycast(origin, -CamRight, out RaycastHit hL, 1.0f, groundCheckLayerMask, QueryTriggerInteraction.Ignore) ? hL.distance : 1.0f;
                dis += Physics.Raycast(origin, CamRight, out RaycastHit hR, 1.0f, groundCheckLayerMask, QueryTriggerInteraction.Ignore) ? hR.distance : 1.0f;
                return dis > 0.9f;
            }
        }

        public bool CanExitCrawling => HasSpace(inputCrounch ? crouchHeight : standHeight);

        private void CrawlUpdate()
        {
            bool shouldCrawl = inputCrawl;
            if (isCrawling && !CanExitCrawling) shouldCrawl = true;
            if (!isCrawling && shouldCrawl && !CanEnterCrawling) shouldCrawl = false;
            if (airTimer > 1.0f) shouldCrawl = false;

            if (IsCrawling != shouldCrawl)
            {
                IsCrawling = shouldCrawl;
                if (IsCrawling) IsDrawingBow = false;
            }
        }

        public void OnCrawlEnter() { isCrawlEntering = true; }
        public void OnCrawlEntered() { isCrawlEntering = false; }
        public void OnCrawlExit() { isCrawlExiting = true; }
        public void OnCrawlExited() { isCrawlExiting = false; }

        // ajusta la altura del CharacterController manteniendo la base pegada al suelo
        private void UpdateColliderHeight()
        {
            float h = standHeight;
            if (isCrouching) h = crouchHeight;
            if (isCrawling || isCrawlEntering || isCrawlExiting) h = crawlHeight;

            h = Mathf.Max(h, cc.radius * 2.0f);
            cc.height = h;
            cc.center = new Vector3(0.0f, h * 0.5f, 0.0f);
        }

        #endregion

        #region - LOOK AT TARGET -

        private static readonly Vector3 HEAD_ROT_RANGE = new(-15.0f, 20.0f);
        private static readonly Vector3 NECK_ROT_RANGE = new(-15.0f, 15.0f);
        private static readonly Vector3 SPINE2_ROT_RANGE = new(-15.0f, 15.0f);
        private static readonly Vector3 SPINE1_ROT_RANGE = new(-15.0f, 15.0f);

        private const float NECK_ROT_PERCENT = 0.5f;
        private const float SPINE2_ROT_PERCENT = 0.3f;
        private const float SPINE1_ROT_PERCENT = 0.3f;
        private const float LOOK_AT_TARGET_LERP_SPEED = 7.5f;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsLookingAtTarget
        {
            get { return isLookingAtTarget; }
            set
            {
                if (isLookingAtTarget == value) return;
                isLookingAtTarget = value;
            }
        }
        private bool isLookingAtTarget;

        // izquierda/derecha según la posición del objetivo en pantalla
        public int LookAtTargetFacing
        {
            get
            {
                Camera c = Cam;
                float dx = c ? (c.WorldToScreenPoint(inputTarget) - c.WorldToScreenPoint(transform.position)).x
                             : Vector3.Dot(inputTarget - transform.position, CamRight);
                int f = Mathf.RoundToInt(Mathf.Sign(dx));
                if (Mathf.Abs(dx) < 0.01f) f = facingDir;
                return f;
            }
        }

        private bool EnableSpineRot => !animator.GetBool("IsCrawling");

        private float lookAtTargetPercent;
        private float spine1Rot;
        private float spine2Rot;

        private void LookAtTarget()
        {
            bool shouldLookAtTarget = false;
            if (IsDrawingBow) shouldLookAtTarget = true;
            if (inputLook) shouldLookAtTarget = true;
            if (inputAttack && attackAction == AttackActionType.PointAtTarget) shouldLookAtTarget = true;
            if (inputAttack && attackAction == AttackActionType.Throw) shouldLookAtTarget = true;
            if (inputAttack && attackAction == AttackActionType.Cast) shouldLookAtTarget = true;
            if (isDead) shouldLookAtTarget = false;

            IsLookingAtTarget = shouldLookAtTarget;
            lookAtTargetPercent = Mathf.Lerp(lookAtTargetPercent, isLookingAtTarget ? 1.0f : 0.0f, LOOK_AT_TARGET_LERP_SPEED * Time.deltaTime);

            // el objetivo del suelo pasado al plano del sprite
            Vector3 target = ToSpritePlane(inputTarget, character.rigNeck.position);
            Vector3 dir, rot;
            float targetRot, pow;

            if (EnableSpineRot)
            {
                dir = character.rigNeck.InverseTransformPoint(target) - character.rigNeck.localPosition;
                dir.z = 0.0f;
                targetRot = Vector3.Angle(Vector3.right, dir) - 90.0f;
                pow = Mathf.Abs(targetRot / 45.0f);
                targetRot = Mathf.Clamp(targetRot, SPINE1_ROT_RANGE.x, SPINE1_ROT_RANGE.y);

                rot = character.rigSpine1.localRotation.eulerAngles;
                spine1Rot = Mathf.LerpAngle(rot.z, targetRot, pow * lookAtTargetPercent * SPINE1_ROT_PERCENT);
                rot.z = spine1Rot;
                character.rigSpine1.localRotation = Quaternion.Euler(rot);

                dir = character.rigNeck.InverseTransformPoint(target) - character.rigNeck.localPosition;
                dir.z = 0.0f;
                targetRot = Vector3.Angle(Vector3.right, dir) - 90.0f;
                pow = Mathf.Abs(targetRot / 45.0f);
                targetRot = Mathf.Clamp(targetRot, SPINE2_ROT_RANGE.x, SPINE2_ROT_RANGE.y);

                rot = character.rigSpine2.localRotation.eulerAngles;
                spine2Rot = Mathf.LerpAngle(rot.z, targetRot, pow * lookAtTargetPercent * SPINE2_ROT_PERCENT);
                rot.z = spine2Rot;
                character.rigSpine2.localRotation = Quaternion.Euler(rot);
            }

            dir = character.rigNeck.InverseTransformPoint(target) - character.rigNeck.localPosition;
            dir.z = 0.0f;
            targetRot = Mathf.Clamp(Vector3.Angle(Vector3.right, dir) - 90.0f, NECK_ROT_RANGE.x, NECK_ROT_RANGE.y);
            rot = character.rigNeck.localRotation.eulerAngles;
            rot.z = Mathf.LerpAngle(rot.z, targetRot, lookAtTargetPercent * NECK_ROT_PERCENT);
            character.rigNeck.localRotation = Quaternion.Euler(rot);

            dir = character.rigHead.InverseTransformPoint(target) - character.rigHead.localPosition;
            dir.z = 0.0f;
            targetRot = Mathf.Clamp(Vector3.Angle(Vector3.right, dir) - 90.0f, HEAD_ROT_RANGE.x, HEAD_ROT_RANGE.y);
            rot = character.rigHead.localRotation.eulerAngles;
            rot.z = Mathf.LerpAngle(rot.z, targetRot, lookAtTargetPercent);
            character.rigHead.localRotation = Quaternion.Euler(rot);
        }

        #endregion

        #region - POINT AT TARGET -

        private const float POINT_AT_TARGET_LERP_SPEED = 7.5f;
        private static readonly Vector3 ARM_ROT_RANGE = new(-80.0f, 70.0f);

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsPointingAtTarget
        {
            get { return isPointingAtTarget; }
            set
            {
                if (isPointingAtTarget == value) return;
                isPointingAtTarget = value;
            }
        }
        private bool isPointingAtTarget;

        // dirección de disparo en el mundo (plano XZ)
        public Vector3 PointAtTargetDirection
        {
            get
            {
                Vector3 d = inputTarget - character.rigUpperArmR.position;
                return Flatten(d, CamRight * facingDir);
            }
        }

        // dirección del brazo en el plano del sprite
        private Vector3 SpriteAimDirection
        {
            get
            {
                Vector3 pivot = character.rigUpperArmR.position;
                return (ToSpritePlane(inputTarget, pivot) - pivot).normalized;
            }
        }

        private float pointAtTargetPercent;

        private void PointAtTarget()
        {
            bool shouldPointAtTarget = false;
            if (IsDrawingBow) shouldPointAtTarget = true;
            if (inputAttack && attackAction == AttackActionType.PointAtTarget) shouldPointAtTarget = true;
            if (inputAttack && attackAction == AttackActionType.Throw) shouldPointAtTarget = true;
            if (inputAttack && attackAction == AttackActionType.Cast) shouldPointAtTarget = true;
            if (isCrawling) shouldPointAtTarget = false;
            if (isDead) shouldPointAtTarget = false;

            IsPointingAtTarget = shouldPointAtTarget;
            pointAtTargetPercent = Mathf.Lerp(pointAtTargetPercent, isPointingAtTarget ? 1.0f : 0.0f, POINT_AT_TARGET_LERP_SPEED * Time.deltaTime * attackSpeedMul);

            float targetArmRot = Vector3.Angle(Vector3.up, SpriteAimDirection) - 90.0f;
            targetArmRot = Mathf.Clamp(targetArmRot, ARM_ROT_RANGE.x, ARM_ROT_RANGE.y);
            targetArmRot += spine1Rot + spine2Rot + character.rigPelvis.localRotation.eulerAngles.z;

            float angle = Mathf.LerpAngle(0.0f, targetArmRot, pointAtTargetPercent);

            // rotación sobre el eje Z local del hueso (equivale al rot.z += del original en 2D)
            character.rigUpperArmR.rotation *= Quaternion.Euler(0.0f, 0.0f, angle);
            if (IsDrawingBow) character.rigUpperArmL.rotation *= Quaternion.Euler(0.0f, 0.0f, angle);
        }

        #endregion

        #region - MOVE -

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsMoving => isMoving;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly]
        public bool IsRunning => isMovingFast;

        private bool isMoving;
        private bool isMovingFast;
        private int moveDir = 1;                // 1: hacia donde mira, -1: de espaldas
        private float moveBlendX;

        private void MoveCheck()
        {
            isMovingFast = inputMoveFast;

            Vector2 m = new(inputMove.x, inputMove.y);
            isMoving = m.magnitude > MOVE_THRESHOLD;

            moveDir = 1;
            if (isMoving == false) return;

            bool hasX = Mathf.Abs(inputMove.x) > MOVE_THRESHOLD;
            if (hasX && Mathf.Sign(inputMove.x) != facingDir)
            {
                moveDir = -1;
                if (IsLookingAtTarget) isMovingFast = false;    // no correr de espaldas
            }
        }

        // calcula la velocidad y mueve el CharacterController una vez por frame
        private void ApplyMotion()
        {
            float acc, max, drag;

            if (isGrounded)
            {
                acc = isMovingFast ? runAcc : walkAcc;
                max = isMovingFast ? runSpeedMax : walkSpeedMax;
                drag = groundDrag;

                if (isCrouching) { acc = crouchAcc; max = crouchSpeedMax; }
                if (isCrawling || isCrawlEntering || isCrawlExiting) { acc = crawlAcc; max = crawlSpeedMax; }
            }
            else
            {
                acc = airAcc;
                max = airSpeedMax;
                drag = airDrag;
            }

            Vector3 wish = (isDead || isReviving) ? Vector3.zero : MoveWorldDir;
            bool hasInput = wish.magnitude > MOVE_THRESHOLD;

            Vector3 target = hasInput ? wish * max : Vector3.zero;
            float rate = (!hasInput || horizontalVel.magnitude > max) ? drag : acc;
            horizontalVel = Vector3.MoveTowards(horizontalVel, target, rate * Time.deltaTime);

            // gravedad: pequeño empuje hacia abajo al estar en suelo para que isGrounded se mantenga estable
            if (isGrounded && verticalVel < 0.0f) verticalVel = -2.0f;
            else verticalVel += gravity * Time.deltaTime;

            Vector3 motion = horizontalVel + Vector3.up * verticalVel;
            cc.Move(motion * Time.deltaTime);

            // el CharacterController resuelve suelo/pendientes; leemos su resultado
            IsGrounded = cc.isGrounded;
            if (isGrounded && verticalVel < 0.0f) verticalVel = -2.0f;
        }

        private void UpdateMoveBlend()
        {
            float target = isMoving ? (isMovingFast ? 3.0f : 1.0f) : 0.0f;
            moveBlendX = Mathf.Lerp(moveBlendX, target, 7.0f * Time.deltaTime);
        }

        #endregion

        #region - FACING & BILLBOARD -

        private int facingDir = 1;             // -1: izquierda  1: derecha (en pantalla)

        private void UpdateFacing()
        {
            if (isDead || isReviving) return;

            if (IsLookingAtTarget)
            {
                facingDir = LookAtTargetFacing;
                return;
            }

            if (Mathf.Abs(inputMove.x) > MOVE_THRESHOLD)
            {
                facingDir = Mathf.RoundToInt(Mathf.Sign(inputMove.x));
            }
        }

        private void UpdateBillboard()
        {
            Camera c = Cam;
            if (c == null) return;

            Transform t = billboardTarget ? billboardTarget : transform;
            t.rotation = Quaternion.Euler(0.0f, c.transform.eulerAngles.y + billboardYawOffset, 0.0f);
        }

        #endregion

        #region - DIE & REVIVE -

        [FoldoutGroup("Runtime"), ShowInInspector, DisableInEditMode]
        public bool IsDead
        {
            get { return isDead; }
            set
            {
                if (isDead == value) return;
                isDead = value;

                IsDrawingBow = false;
                IsArrowDrawn = false;
                IsStringPulled = false;

                character.IsDead = value;
            }
        }
        private bool isDead;

        [FoldoutGroup("Runtime"), ShowInInspector, DisableInEditMode]
        public bool IsReviving
        {
            get { return isReviving; }
            set { isReviving = value; }
        }
        private bool isReviving;

        public void OnReviveStart() { IsReviving = true; }
        public void OnReviveEnd() { IsReviving = false; }

        #endregion

        #region - ANIMATOR -

        private void UpdateAnimator()
        {
            if (isDead) return;

            Vector3 v = cc.velocity;

            animator.SetBool("IsMoving", isMoving);
            animator.SetFloat("MoveBlendX", moveBlendX);
            animator.SetFloat("MoveBlendY", 0.0f);
            animator.SetBool("IsRunning", isMovingFast);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsCrouching", isCrouching);
            animator.SetBool("IsCrawling", isCrawling);
            animator.SetFloat("VelocityX", new Vector2(v.x, v.z).magnitude);
            animator.SetFloat("VelocityY", v.y);
            animator.SetFloat("MoveSpeedMul", moveDir);
            animator.SetFloat("CrawlSpeedMul", moveDir * Mathf.Clamp01(new Vector2(inputMove.x, inputMove.y).magnitude));

            character.Facing = (PixelCharacter.FacingType)facingDir;
        }

        #endregion

        #region - UNITY CALLBACKS -

        private void Awake()
        {
            character = GetComponent<PixelCharacter>();
            animator = GetComponentInChildren<Animator>();
            cc = GetComponent<CharacterController>();

            cc.slopeLimit = maxSlopeAngle;

            GetComponentsInChildren(true, selfColliders);
            UpdateColliderHeight();
        }

        private void Update()
        {
            if (IsInAir) airTimer += Time.deltaTime;
            else airTimer = 0.0f;

            if (IsIdle) idleTimer += Time.deltaTime;
            else idleTimer = 0.0f;

            MoveCheck();
            Crouch();
            CrawlUpdate();
            UpdateColliderHeight();
            ApplyMotion();
            UpdateMoveBlend();
            AttackUpdate();
            UpdateFacing();
            ArcheryUpdate();
            UpdateAnimator();
        }

        private void LateUpdate()
        {
            UpdateBillboard();
            LookAtTarget();
            PointAtTarget();
        }

        #endregion

        #region - FOOTSTEP -

        public void OnFootstep(AnimationEvent evt)
        {
            if (isDead || isReviving) return;
            if (airTimer > 0.1f) return;
            if (evt.animatorClipInfo.weight < 0.49f) return;

            if (IsCrawling)
            {
                if (evt.stringParameter == "Crawl") onFootstep.Invoke();
                return;
            }

            if (IsCrouching)
            {
                if (evt.stringParameter == "Crouch") onFootstep.Invoke();
                return;
            }

            if (evt.stringParameter == "Walk" && moveBlendX > 0.1f && moveBlendX < 1.1f) { onFootstep.Invoke(); return; }
            if (evt.stringParameter == "Run" && moveBlendX > 1.1f) { onFootstep.Invoke(); return; }
        }

        #endregion

        #region - GIZMOS -
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (debug_drawVelocity && cc)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + cc.velocity);
            }

            if (debug_drawAim && character)
            {
                Gizmos.color = Color.cyan;
                Vector3 p = character.rigUpperArmR.position;
                Gizmos.DrawLine(p, p + PointAtTargetDirection * 2.0f);
                Gizmos.DrawWireSphere(inputTarget, 0.15f);
            }
        }
#endif
        #endregion

        #region - LEGACY COMPAT -
        // miembros de sistemas eliminados (salto, dash, esquiva, escaleras, agua, bordes).
        // existen solo para que compilen otros scripts de Cainos; siempre devuelven "inactivo".
        [HideInInspector] public bool inputJump;
        [HideInInspector] public bool inputDash;
        [HideInInspector] public bool inputDodge;

        public bool IsClimbingLadder => false;
        public bool IsEnteringLadder => false;
        public bool IsExitingLadder => false;
        public bool IsClimbingLedge => false;
        public bool IsStandingOnPlatform => false;
        public bool IsDashing => false;
        public bool IsDodging => false;
        public bool IsInWater => false;
        public bool IsHeadInWater => false;
        public bool IsNearWaterSurface => false;
        public float InWaterPercent => 0.0f;
        public float InWaterDepth => -1.0f;

        // eventos de animación de clips eliminados; no hacen nada
        public void OnDodgeStart() { }
        public void OnDodgeEnd() { }
        public void OnSwimHand(AnimationEvent evt) { }
        public void OnSwimFoot(AnimationEvent evt) { }
        public void OnLadderEntered() { }
        public void OnLadderExited() { }
        public void OnLedgeClimbLocked() { }
        public void OnLedgeClimbFinised() { }
        #endregion

        #region - ENUMS -

        public enum AttackActionType
        {
            None = 0,
            Swipe = 1,
            Stab = 2,
            PointAtTarget = 11,
            Summon = 12,
            Throw = 13,
            Cast = 14,
            Archery = 21
        }

        public enum AttackActionMeleeType
        {
            None = 0,
            Swipe = 1,
            Stab = 2
        }

        #endregion
    }
}