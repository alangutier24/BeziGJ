using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{

    public static DialogueManager Instance {get; private set;}

    public TMP_Text nameText;
    public TMP_Text dialogueText;

    public Animator animator;

    public GameObject panel;

    [SerializeField] SoundDefinition dialogueSfx;  
    [SerializeField] SoundDefinition endDialogueSfx;  
    [SerializeField] SoundDefinition startDialogueSfx;  

    [SerializeField] Player player;

    private Queue<string> sentences;

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Awake()
    {
        if(Instance != null && Instance != this) 
        { Destroy(gameObject); return;}
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        panel = null; animator = null; nameText = null; dialogueText = null; player = null;

        panel = GameObject.Find("Dialogue");
        if (!panel) return;

        animator     = panel.GetComponent<Animator>();
        nameText     = FindText(panel.transform, "Name");
        dialogueText = FindText(panel.transform, "Sentence");
        player           = FindAnyObjectByType<Player>();

        if (animator) animator.enabled = false;
    }

    static TMP_Text FindText(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            if (t.name == name) return t;
        return null;
    }

    void Start()
    {
        sentences = new Queue<string>();

        animator.enabled = false;

        panel.SetActive(true);

    }

    public void StartDialogue(Dialogue dialogue)
    {
        Debug.Log($"cm:{player} audio:{AudioManager.Instance} anim:{animator} panel:{panel} " +
          $"name:{nameText} text:{dialogueText} sentences:{sentences}");
        player.dialogueBlock = true;

        AudioManager.Instance.PlaySFX(startDialogueSfx);
        animator.enabled = true;
        
        panel.SetActive(true);

        sentences.Clear();

        animator.SetBool("IsHiden", false);
        animator.SetBool("IsOpen", true);

        nameText.text = dialogue.name;

        foreach(string sentence in dialogue.sentences)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public bool DisplayNextSentence()
    {
        if(sentences.Count == 0)
        {
            AudioManager.Instance.PlaySFX(endDialogueSfx);
            EndDialogue();
            return true;
        }
        else
        {
            AudioManager.Instance.PlaySFX(dialogueSfx);
        }
        string sentence = sentences.Dequeue();

        dialogueText.text = sentence;

        return false;
    }

    public void EndDialogue()
    {
        player.dialogueBlock = false;
        animator.SetBool("IsOpen", false);
    }


}
