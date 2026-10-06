using UnityEngine;

public class ItemLookAtCamera : MonoBehaviour
{
    void Update()
    {
        transform.LookAt(Camera.main.transform); 
    }
}
