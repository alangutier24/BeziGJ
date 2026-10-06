using UnityEngine;

public class ChestInteraction : MonoBehaviour, IInteractable
{
    [SerializeField]
    private SpriteRenderer chestSpriteRenderer;

    [SerializeField]
    private SpriteRenderer weaponSpriteRenderer;

    [SerializeField]
    private Sprite[] sprites;

    private bool open = false;

    public bool CanInteract()
    {
        return true;
    }

    public bool Interact(Interactor interactor)
    {
        if(chestSpriteRenderer && weaponSpriteRenderer)
        {
            open = !open;
            if(open)
            {
                chestSpriteRenderer.sprite = sprites[1];
                weaponSpriteRenderer.enabled = true;
                
            }
            else
            {
                chestSpriteRenderer.sprite = sprites[0];
                weaponSpriteRenderer.enabled = false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}
