using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] private BoxCollider trigger;
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] public PlayerKeys.Keys keyType;
 
    [Header("Opcional: se desactivan al abrir")]
    [SerializeField] private Collider solidCollider;
    [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;
    [SerializeField] SoundDefinition alertSfx;  

    [SerializeField] Light light;

 
    private bool opened;
 
    void Reset()
    {
        trigger = GetComponent<BoxCollider>();
        animator = GetComponentInChildren<Animator>();
    }
 
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger");
        if (opened || !other.CompareTag("Player")) 
        {
            Debug.Log("Not Player!");
            return;
        }
        PlayerKeys keys = other.GetComponentInParent<PlayerKeys>();
        if (keys == null) 
        {
            Debug.Log("Keys are null");
            return;
        }
 
        switch(keyType)
        {
            case PlayerKeys.Keys.SecretRoom:
                if(keys.hasSecretRoomKey())
                {
                 Debug.Log("Openned secret Door");
                    Open();
                }

            break;
            case PlayerKeys.Keys.FrontDoor:
                if(keys.hasFrontDoorKey())
                    Open();
            break;
        }
    }
 
    void Open()
    {
        AudioManager.Instance.PlaySFX(alertSfx);
        opened = true;
        animator.SetTrigger(openTrigger);
 
        if (solidCollider) solidCollider.enabled = false;
        if (navObstacle) navObstacle.enabled = false;
 
        light.color = Color.green;

        Destroy(trigger);
    }
}
