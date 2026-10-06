using UnityEngine;
using System.Collections;

public class PickUpItemInteraction : MonoBehaviour, IInteractable
{
    [SerializeField]
    private Sprite sprite;

    public ItemUI itemUI;

    public string itemName;

    public bool startNextDialogue = false;

    public int storyIndex;

    [SerializeField] SoundDefinition alertSfx; 

    [SerializeField] public SpriteRenderer sr; 
    [SerializeField] public SpriteRenderer sr2; 

    private bool isOpen = false;
    private bool used = false;
    private bool isClosed = false;

    public CharacterController player;

    public bool CanInteract() => !used;

    public bool Interact(Interactor interactor)
    {
        if (!isOpen)   { Open();  return true; }
        if (!isClosed) { Close(); return true; }   // starts dialogue and shows sentence 1

        // Following presses: advance the dialogue
        if (DialogueManager.Instance.DisplayNextSentence())
        {
            player.enabled = true;
            used = true;   // dialogue finished
            Destroy(gameObject);
        }
        return true;
    }

    void Close()
    {
        isClosed = true;
        if(itemUI) itemUI.HideUI();
        if (startNextDialogue)
            StoryManager.Instance.PlayNextStory(storyIndex);
        else
            used = true;   // no dialogue, nothing left to do
    }

    void Open()
    {
        isOpen = true;
        if(itemUI) itemUI.ItemPicked(sprite, itemName);
        if(alertSfx) AudioManager.Instance.PlaySFX(alertSfx);
        if(sr) sr.enabled = false;
        if(sr2) sr2.enabled = false;
        player.enabled = false;
    }

}
