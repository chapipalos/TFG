using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueFramework
{
    /// <summary>
    /// Controlador FPS con New Input System usando RIGIDBODY (sin CharacterController).
    /// Requiere: Rigidbody + Collider (cápsula) + PlayerInput.
    ///
    /// Config del Rigidbody en el Inspector:
    ///   - Freeze Rotation: X, Y, Z (todas marcadas) → el script controla la rotación.
    ///   - Interpolate: Interpolate (movimiento suave).
    ///   - Collision Detection: Continuous (evita atravesar suelo a alta velocidad).
    ///
    /// Acciones del Input Action Asset:
    ///   Move (Vector2), Look (Vector2), Jump (Button), Sprint (Button), ToggleCursor (Button).
    ///   Behavior del PlayerInput: "Send Messages".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class FPSPlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float jumpForce = 5f;

        [Header("Detección de suelo")]
        [Tooltip("Objeto vacío en los pies del jugador")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.3f;
        public LayerMask groundLayer = ~0; // por defecto: todas las capas

        [Header("Cámara")]
        public Transform cameraTransform;
        public Vector2 mouseSensitivity = new Vector2(0.15f, 0.15f);
        public float verticalLookClamp = 85f;

        // ── Estado de inputs ────────────────────────────────────────────────────
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpQueued;
        private bool _isSprinting;

        // ── Internos ──────────────────────────────────────────────────────────
        private Rigidbody _rb;
        private float _verticalRotation;
        private bool _isGrounded;

        // ══════════════════════════════════════════════════════════════════════
        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true; // por si acaso no se marcó en el Inspector

            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
                else Debug.LogError("[FPSPlayerController] Asigna 'cameraTransform' en el Inspector.");
            }

            if (groundCheck == null)
                Debug.LogWarning("[FPSPlayerController] No hay 'groundCheck' asignado. El salto puede no funcionar bien.");

            LockCursor(true);
        }

        void Update()
        {
            // La rotación de cámara va en Update (suave, ligada a los fps de render)
            HandleLook();
        }

        void FixedUpdate()
        {
            // La física (mover el rigidbody) va en FixedUpdate
            CheckGround();
            HandleMovement();
            HandleJump();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CALLBACKS NEW INPUT SYSTEM
        // ══════════════════════════════════════════════════════════════════════
        void OnMove(InputValue v) => _moveInput = v.Get<Vector2>();
        void OnLook(InputValue v) => _lookInput = v.Get<Vector2>();
        void OnSprint(InputValue v) => _isSprinting = v.isPressed;

        void OnJump(InputValue v)
        {
            if (v.isPressed) _jumpQueued = true;
        }

        void OnToggleCursor(InputValue v)
        {
            if (v.isPressed) LockCursor(Cursor.lockState != CursorLockMode.Locked);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  LÓGICA
        // ══════════════════════════════════════════════════════════════════════

        void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float lookX = _lookInput.x * mouseSensitivity.x;
            float lookY = _lookInput.y * mouseSensitivity.y;

            // Rotación horizontal del cuerpo
            transform.Rotate(Vector3.up * lookX);

            // Rotación vertical de la cámara
            _verticalRotation -= lookY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalLookClamp, verticalLookClamp);
            cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
        }

        void CheckGround()
        {
            if (groundCheck != null)
                _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        }

        void HandleMovement()
        {
            float speed = _isSprinting ? runSpeed : walkSpeed;

            Vector3 moveDir = transform.right * _moveInput.x
                            + transform.forward * _moveInput.y;
            moveDir.Normalize();

            // Conservamos la velocidad vertical (gravedad/salto), solo cambiamos la horizontal
            Vector3 targetVelocity = moveDir * speed;
            Vector3 velocity = _rb.linearVelocity; // Unity 6+. En versiones antiguas: _rb.velocity
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
            _rb.linearVelocity = velocity;
        }

        void HandleJump()
        {
            if (_jumpQueued && _isGrounded)
            {
                // Resetea la velocidad vertical antes de impulsar (salto consistente)
                Vector3 v = _rb.linearVelocity;
                v.y = 0f;
                _rb.linearVelocity = v;

                _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
            _jumpQueued = false;
        }

        void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // Visualiza el radio de detección de suelo en el editor
        void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}