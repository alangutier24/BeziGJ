using UnityEngine;

public class StoryManager : MonoBehaviour
{

    public static StoryManager Instance {get; private set;}

    [SerializeField]
    private Dialogue[] story;

    private enum StoryState
    {
        Beginning,
        MeetsElias, 
        FindsNecklace, 
        CopsSearchingStreet, 
        FindsGun,
        BuildingRoad,
        ArrivesToPark,
        FindsWife,
        End
    }

    private StoryState storyState;

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        storyState = StoryState.Beginning;
    }

    void Update()
    {
        
    }

    public void StartStory(int index)
    {
        DialogueManager.Instance.StartDialogue(story[0]);
    }

    public bool PlayNextStory(int index)
    {
        DialogueManager.Instance.StartDialogue(story[index]);
        return true;
    }

}
