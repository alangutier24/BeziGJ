using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    
    bool hasWeapon = false;

    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;
        hasWeapon = true;


    }   

    void Start()
    {
        
    }
   
    void Update()
    {
        if(!hasWeapon) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Debug.Log(hit.collider.name + " at " + hit.point);
            }
        }

    }
}
