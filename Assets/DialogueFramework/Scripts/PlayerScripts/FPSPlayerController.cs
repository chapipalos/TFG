using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueFramework
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class FPSPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float m_WalkSpeed = 5f;

        [Header("Camera")]
        public Transform m_CameraTransform;
        public Vector2 m_MouseSensitivity = new Vector2(0.15f, 0.15f);
        public float m_VerticalLookClamp = 85f;

        private Vector2 m_MoveInput;
        private Vector2 m_LookInput;
        private bool m_Quest;
        private bool m_Interact;
        private bool m_Test1;
        private bool m_Test2;

        private Rigidbody m_Rigidbody;
        private float m_VerticalRotation;

        [Header("Interaction")]
        public float m_InteractRange = 3f;
        private QuestController m_QuestController;

        private bool m_QuestPanelOpen;

        // ══════════════════════════════════════════════════════════════════════
        void Awake()
        {
            m_QuestController = FindFirstObjectByType<QuestController>(FindObjectsInactive.Include);
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.freezeRotation = true;

            if (m_CameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) m_CameraTransform = cam.transform;
                else Debug.LogError("[FPSPlayerController] Asign 'cameraTransform' at Inspector.");
            }
        }

        void Update()
        {
            HandleLook();
            HandleQuest();
            HandleInteract();
            HandleTest1();
            HandleTest2();
        }

        void FixedUpdate()
        {
            HandleMovement();
        }

        void OnMove(InputValue v)
        {
            m_MoveInput = v.Get<Vector2>();
        }

        void OnLook(InputValue v) => m_LookInput = v.Get<Vector2>();

        void OnInteract(InputValue v)
        {
            if (v.isPressed) m_Interact = true;
        }

        void OnQuest(InputValue v)
        {
            if (v.isPressed) m_Quest = true;
        }

        void OnTest1(InputValue v)
        {
            if (v.isPressed) m_Test1 = true;
        }

        void OnTest2(InputValue v)
        {
            if (v.isPressed) m_Test2 = true;
        }

        void HandleLook()
        {
            if (m_QuestPanelOpen || (DialogueManager.Active != null && DialogueManager.Active.m_DialogueRunning)) return;
            float lookX = m_LookInput.x * m_MouseSensitivity.x;
            float lookY = m_LookInput.y * m_MouseSensitivity.y;

            transform.Rotate(Vector3.up * lookX);

            m_VerticalRotation -= lookY;
            m_VerticalRotation = Mathf.Clamp(m_VerticalRotation, -m_VerticalLookClamp, m_VerticalLookClamp);
            m_CameraTransform.localRotation = Quaternion.Euler(m_VerticalRotation, 0f, 0f);
        }

        void HandleMovement()
        {
            if (m_QuestPanelOpen || (DialogueManager.Active != null && DialogueManager.Active.m_DialogueRunning)) return;
            Vector3 moveDir = transform.right * m_MoveInput.x
                            + transform.forward * m_MoveInput.y;
            moveDir.Normalize();

            Vector3 targetVelocity = moveDir * m_WalkSpeed;
            Vector3 velocity = m_Rigidbody.linearVelocity;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
            m_Rigidbody.linearVelocity = velocity;
        }

        void HandleQuest()
        {
            if (m_Quest)
            {
                if (m_QuestController.gameObject.activeSelf)
                {
                    m_QuestController.CloseQuestPanel();
                    m_QuestPanelOpen = false;
                }
                else
                {
                    m_QuestController.OpenQuestPanel();
                    m_QuestPanelOpen = true;
                }
                m_Quest = false;
            }
        }

        void HandleInteract()
        {
            if (m_Interact)
            {
                if (Physics.Raycast(m_CameraTransform.position, m_CameraTransform.forward, out RaycastHit hit, m_InteractRange))
                {
                    DialogueManager npcDialogue = hit.collider.GetComponent<DialogueManager>();

                    if (npcDialogue != null && !npcDialogue.m_DialogueRunning)
                    {
                        npcDialogue.StartDialogue();
                    }
                }

                m_Interact = false;
            }
        }

        void HandleTest1()
        {
            if (m_Test1)
            {
                GameEventBus.Raise("OnIronPickedUp");
                m_Test1 = false;
            }
        }

        void HandleTest2()
        {
            if (m_Test2)
            {
                GameEventBus.Raise("OnREADMERead");
                m_Test2 = false;
            }
        }

        void OnDrawGizmos()
        {
            if (m_CameraTransform == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(m_CameraTransform.position, m_CameraTransform.forward * m_InteractRange);
        }
    }
}