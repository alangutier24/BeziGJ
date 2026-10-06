using UnityEngine;

public class PickUpProximity : MonoBehaviour
{
    public SpriteRenderer E;
    [SerializeField] SoundDefinition alertSfx;  

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) 
        {
            ItemLookAtCamera itemCam = GetComponent<ItemLookAtCamera>();
            if(itemCam)
                itemCam.enabled = true;
            E.enabled = true;

            AudioManager.Instance.PlaySFX3D(alertSfx, transform.position);
        }
    }
    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) 
        {
            ItemLookAtCamera itemCam = GetComponent<ItemLookAtCamera>();
            if(itemCam)
                itemCam.enabled = false;
            E.enabled = false;
        }
    }
}