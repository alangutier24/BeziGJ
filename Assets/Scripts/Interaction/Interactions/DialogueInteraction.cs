using System.Collections.Generic;
using UnityEngine;

public class DialogueInteraction : MonoBehaviour, IInteractable
{
    public Dialogue dialogue;
  
    private bool hasStarted = false;

    [SerializeField] SoundDefinition firstalertSfx; 

    [SerializeField] SoundDefinition alertSfx;

    public bool playSfx = false;

    public bool hasInstruction = false;

    private bool usedPower = false;

    public TutVisPower tutPower;

    public bool CanInteract()
    {
        return true;
    }

    public bool Interact(Interactor interactor)
    {
        if(!hasStarted)
        {
            hasStarted = true;
            DialogueManager.Instance.StartDialogue(dialogue);
            if(playSfx)
                AudioManager.Instance.PlaySFX(firstalertSfx);

            tutPower.SetGUIText();
        }
        else        
        {
            if(DialogueManager.Instance.DisplayNextSentence() && hasInstruction)
            {
                usedPower = true;
                tutPower.OpenInstruction();
            }

            if(playSfx)
                AudioManager.Instance.PlaySFX(alertSfx);
        }

       return true;
    }
}
