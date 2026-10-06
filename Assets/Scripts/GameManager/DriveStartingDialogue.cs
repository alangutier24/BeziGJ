using UnityEngine;

public class DriveStartingDialogue : MonoBehaviour, IInteractable
{

    private bool hasStarted = false;
    private bool used = false;
    public Animator eanim;
    public CharacterController player;

    public bool CanInteract() => !used;

    void Start()
    {
        eanim.SetBool("Display", true);
        player.enabled = false;
    }

    public bool Interact(Interactor interactor)
    {
        if (!hasStarted)   { Open();  return true; }

        if (DialogueManager.Instance.DisplayNextSentence())
        {
            player.enabled = true;
            used = true;
        }

        return true;
    }

    void Open()
    {
        hasStarted = true;
        eanim.SetBool("Display", false);
        StoryManager.Instance.PlayNextStory(0);
    }

}
