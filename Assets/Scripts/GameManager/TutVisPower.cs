using UnityEngine;
using TMPro;
using System.Collections;

public class TutVisPower : MonoBehaviour
{

    public TMP_Text dialogueText;
    public string instruction;
    public Animator anim;

    public void SetGUIText()
    {
        dialogueText.text = instruction;
    }

    public void OpenInstruction()
    {
        anim.SetBool("Display", true);
        StartCoroutine(DoAfterDelay(4f));
    }
        
    IEnumerator DoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetBool("Display", false);
    }
    
}
