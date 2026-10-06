using UnityEngine;
using UnityEngine.AI;

public class SpriteFacing : MonoBehaviour
{
    [SerializeField] Transform cam;

    void Reset()  // auto-fill when added in the editor
    {
        if (Camera.main) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.rotation = cam.rotation; 
    }
}