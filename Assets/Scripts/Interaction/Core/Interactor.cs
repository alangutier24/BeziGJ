using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{

    [SerializeField] LayerMask interactMask;   
    
    [SerializeField]
    private Vector3 _rayCastOffset = new Vector3(0, 0f, 0);


    private void Update()
    {
        if(Keyboard.current.eKey.wasPressedThisFrame)
        {
            if(DoInteractionTest(out IInteractable interactable))
            {
                if(interactable.CanInteract())
                {
                    interactable.Interact(this);
                }
            }
        }
    }
    
    private bool DoInteractionTest(out IInteractable interactable)
    {
        interactable = null;

        Vector3 center = transform.position + _rayCastOffset;
        Collider[] hits = Physics.OverlapSphere(center, 1.25f, interactMask);

        foreach (Collider hit in hits)
        {
            Debug.Log($"Hit: {hit.name} | Interactable: {hit.GetComponentInParent<IInteractable>()}");
            interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
                return true;
        }

        return false;
    }

}
