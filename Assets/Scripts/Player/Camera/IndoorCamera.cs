using UnityEngine;

public class IndoorCamera : MonoBehaviour
{
    public float speed;
    public float cameraDistance;

    public Transform cameraTransform;

    public Transform targetPosition;

    public Quaternion cameraAngle;

    public Vector3 offset;

    public LayerMask collisionMask;  

    public void Start()
    {
        offset = targetPosition.localPosition;  
    }
    public void CameraUpdate()
    {
        Ray ray = new Ray(transform.position, -this.transform.forward);
        RaycastHit hit;
        float dist = Physics.SphereCast(ray, 0.25f, out hit, cameraDistance, collisionMask) ? hit.distance : cameraDistance;        
        cameraTransform.localPosition = new Vector3(offset.x, offset.y, -dist);

        cameraTransform.LookAt(transform);

    }
}
