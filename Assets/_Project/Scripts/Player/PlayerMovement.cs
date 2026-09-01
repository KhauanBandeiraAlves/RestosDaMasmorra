using UnityEngine;
using UnityEngine.InputSystem;

namespace RestosDaMasmorra.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStamina))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 3.5f;
        [SerializeField] float runSpeed = 6.5f;
        [SerializeField] float acceleration = 18f;
        [SerializeField] float deceleration = 22f;
        [SerializeField] float gravity = -20f;
        [SerializeField] Transform cameraReference;

        CharacterController controller;
        PlayerStamina stamina;

        Vector3 currentPlanarVelocity;
        float verticalVelocity;

        public bool IsRunning { get; private set; }
        public Vector2 LastMoveInput { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            stamina = GetComponent<PlayerStamina>();
            if (cameraReference == null && Camera.main != null) cameraReference = Camera.main.transform;
        }

        void Update()
        {
            Tick(ReadMoveInput(), ReadRunHeld(), Time.deltaTime);
        }

        // Testable core of the movement logic, decoupled from reading hardware input and
        // from the engine's real frame time so it can be driven deterministically in tests.
        public void Tick(Vector2 input, bool runHeld, float deltaTime)
        {
            LastMoveInput = input;

            Vector3 desiredDirection = InputToWorldDirection(input);
            bool wantsToRun = runHeld && desiredDirection.sqrMagnitude > 0.0001f;

            stamina.Tick(deltaTime, wantsToRun);
            IsRunning = stamina.IsCurrentlyRunning(wantsToRun);

            float targetSpeed = IsRunning ? runSpeed : walkSpeed;
            Vector3 targetVelocity = desiredDirection * targetSpeed;

            float rate = targetVelocity.sqrMagnitude > currentPlanarVelocity.sqrMagnitude ? acceleration : deceleration;
            currentPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, targetVelocity, rate * deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * deltaTime;

            Vector3 motion = currentPlanarVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * deltaTime);

            if (currentPlanarVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentPlanarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 12f * deltaTime);
            }
        }

        Vector2 ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            Vector2 raw = new Vector2(x, y);
            return raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }

        bool ReadRunHeld()
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.leftShiftKey.isPressed;
        }

        Vector3 InputToWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraReference != null)
            {
                forward = cameraReference.forward;
                forward.y = 0f;
                forward.Normalize();

                right = cameraReference.right;
                right.y = 0f;
                right.Normalize();
            }

            return (forward * input.y + right * input.x).normalized * input.magnitude;
        }
    }
}
