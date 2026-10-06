using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PushRigidbodies : MonoBehaviour
{
    [SerializeField] float pushForce = 20f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return; // ignora lo que pisas

        Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        rb.AddForceAtPosition(dir * pushForce * Time.deltaTime, hit.point, ForceMode.Impulse);
    }
}