using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RestosDaMasmorra.Core;

namespace RestosDaMasmorra.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] float interactionRadius = 1.5f;
        [SerializeField] LayerMask interactableMask = ~0;

        IInteractable current;

        public event Action<string> InteractableChanged;

        void Update()
        {
            IInteractable found = FindClosestInteractable();
            if (found != current)
            {
                current = found;
                InteractableChanged?.Invoke(current?.InteractionPrompt);
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && current != null && current.CanInteract)
            {
                current.Interact(gameObject);
            }
        }

        IInteractable FindClosestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, interactableMask, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDistSqr = float.MaxValue;

            foreach (Collider hit in hits)
            {
                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract) continue;

                float distSqr = (hit.transform.position - transform.position).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = interactable;
                }
            }

            return best;
        }
    }
}
