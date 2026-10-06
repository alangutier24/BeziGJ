using UnityEngine;
using UnityEngine.EventSystems;

namespace Cainos.CustomizablePixelCharacter
{
    // alimenta PixelCharacterController (versión 3D XZ) con ratón y teclado
    public class PixelCharacterInputMouseAndKeyboard : MonoBehaviour
    {
        public KeyCode upKey = KeyCode.W;           // profundidad +
        public KeyCode downKey = KeyCode.S;         // profundidad -
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        [Space]
        public KeyCode runKey = KeyCode.LeftShift;
        public KeyCode crounchKey = KeyCode.C;
        public KeyCode crawlKey = KeyCode.Z;
        [Space]
        public KeyCode attackKey = KeyCode.Mouse0;
        public KeyCode meleeKey = KeyCode.V;
        [Space]
        public KeyCode lookKey = KeyCode.Mouse1;
        [Space]
        public LayerMask aimLayerMask = ~0;         // capas contra las que se proyecta el ratón (suelo)
        public float aimRayDistance = 200.0f;

        private PixelCharacterController controller;

        private Vector2 inputMove;
        private bool inputMoveFast;
        private bool inputCrouch;
        private bool inputCrawl;
        private bool inputAttack;
        private bool inputMelee;
        private bool inputLook;
        private Vector3 inputTarget;

        private void Awake()
        {
            controller = GetComponent<PixelCharacterController>();
        }

        private void Update()
        {
            bool pointerOverUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();

            if (pointerOverUI)
            {
                inputMove = Vector2.zero;
                inputMoveFast = false;
                inputAttack = false;
                inputMelee = false;
            }
            else
            {
                inputMoveFast = Input.GetKey(runKey);
                inputAttack = Input.GetKey(attackKey);
                inputMelee = Input.GetKey(meleeKey);
                inputLook = Input.GetKey(lookKey);

                //crouch
                if (Input.GetKeyDown(crounchKey) && controller.IsInAir == false)
                {
                    if (inputCrawl && controller.CanExitCrawling)
                    {
                        inputCrawl = false;
                        inputCrouch = true;
                    }
                    else
                    {
                        inputCrouch = !inputCrouch;
                    }

                    if (controller.CanExitCrouching == false) inputCrouch = true;
                }

                //crawl
                if (Input.GetKeyDown(crawlKey) && controller.IsInAir == false)
                {
                    inputCrawl = !inputCrawl;
                    if (controller.CanEnterCrawling == false) inputCrawl = false;
                    if (controller.CanExitCrawling == false) inputCrawl = true;
                    if (inputCrawl == false) inputCrouch = false;
                }

                //run cancels crouch and crawl
                if (Input.GetKeyDown(runKey))
                {
                    inputCrouch = false;
                    inputCrawl = false;
                }

                //staying in air for more than 1s cancels crouch and crawl
                if (controller.AirTimer > 1.0f)
                {
                    inputCrouch = false;
                    inputCrawl = false;
                }

                //cancel crouch or crawl when dead
                if (controller.IsDead)
                {
                    inputCrouch = false;
                    inputCrawl = false;
                }

                //x: horizontal en pantalla, y: profundidad
                if (Input.GetKey(leftKey)) inputMove.x = -1.0f;
                else if (Input.GetKey(rightKey)) inputMove.x = 1.0f;
                else inputMove.x = 0.0f;

                if (Input.GetKey(downKey)) inputMove.y = -1.0f;
                else if (Input.GetKey(upKey)) inputMove.y = 1.0f;
                else inputMove.y = 0.0f;

                UpdateAimTarget();
            }

            //feed input to controller
            if (controller)
            {
                controller.inputMove = inputMove;
                controller.inputMoveFast = inputMoveFast;
                controller.inputCrounch = inputCrouch;
                controller.inputCrawl = inputCrawl;
                controller.inputAttack = inputAttack;
                controller.inputMelee = inputMelee;
                controller.inputLook = inputLook;
                controller.inputTarget = inputTarget;
            }
        }

        // ratón proyectado al suelo; si no toca nada, plano horizontal a la altura del personaje
        private void UpdateAimTarget()
        {
            Camera cam = controller && controller.cam ? controller.cam : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, aimRayDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
            {
                inputTarget = hit.point;
                return;
            }

            Plane ground = new(Vector3.up, transform.position);
            if (ground.Raycast(ray, out float enter)) inputTarget = ray.GetPoint(enter);
        }
    }
}