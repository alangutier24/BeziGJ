using UnityEngine;
using System.Collections;

public class Key : MonoBehaviour, IInteractable
{

    public PlayerKeys.Keys keyType;
    public PlayerKeys pk;
    [SerializeField] SoundDefinition alertSfx;  
    [SerializeField] bool requiresVision = false;
    private bool used = false;

    public bool CanInteract() => !used;

    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted with key");
        if(!CanPickUp()) return false;
        Debug.Log("Can pick up key");

        PlayerKeys keys = interactor.GetComponentInParent<PlayerKeys>();
          
        AudioManager.Instance.PlaySFX(alertSfx);
        
        switch(keyType)
        {
            case PlayerKeys.Keys.SecretRoom:
                pk.FoundSecretRoomKey();
                Debug.Log("Found secret room key8");
            break;
            case PlayerKeys.Keys.FrontDoor:
                pk.FoundFrontDoorKey();
            break;

        }
        StartCoroutine(DestroyKey(3f));
        gameObject.SetActive(false);

        return true;
    }

    bool CanPickUp()
    {
        if (!requiresVision) return true;
        return DeityVision.Instance && DeityVision.Instance.IsRevealed(transform.position);
    }

    IEnumerator DestroyKey(float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
