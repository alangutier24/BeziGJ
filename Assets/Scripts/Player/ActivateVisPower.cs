using UnityEngine;

public class ActivateVisPower : MonoBehaviour
{
    
   void OnTriggerEnter(Collider other)
   {
    Debug.Log("entered vis activate");
    if(other.CompareTag("Player"))
    {
        GameManager.Instance.UnlockEliasPower();
        Destroy(this);
    }
   }
}
