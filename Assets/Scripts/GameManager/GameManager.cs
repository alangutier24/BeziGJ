using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance {get; private set;}

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    [SerializeField] public SoundDefinition HouseMusic; 
    [SerializeField] public SoundDefinition MainMenuMusic;
    [SerializeField] public SoundDefinition startingDialogueSfx;  

    private enum GameState
    {
        MainMenu,
        Starting,
        Pause, 
        Play, 
        End,
        Win, 
        Loose, 
        Street, 
        Road, 
        Park,
        Apartment,
        Story
    }

    private GameState gameState;

    [SerializeField]
    private StoryManager sm;

    private bool storyStarted = false;
    private bool mainMenuLoaded = false;
    private bool startApartmentStory = false;
    private bool unlockDeityVisi = false;
    
    private GameObject ECanvas;
    private Animator eanim;

    void Awake()
    {
        QualitySettings.vSyncCount = 1;
        if(Instance != null && Instance != this) {Destroy(gameObject); return;}
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        gameState = GameState.MainMenu;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameState = GameState.Apartment;
        ECanvas = GameObject.Find("PressECanvas");
        Debug.Log("Scene Loaded");
        if(ECanvas == null) 
        {
            Debug.Log("Canvas is null");
            return;
        }
        eanim = ECanvas.GetComponent<Animator>();
    }

    void Update()
    {
        switch(gameState)
        {
            case GameState.Play:
            {
                //Play();
            }
            break;

            case GameState.MainMenu:
            {
                if(!mainMenuLoaded)
                {
                    AudioManager.Instance.PlayMusic(MainMenuMusic, 1.5f);
                    mainMenuLoaded = true;
                }
            }
            break;

            case GameState.Story:
            {

            }
            break;

            case GameState.Pause:
            {
                //GameSettings.Pause();
            }
            break;

            case GameState.Loose:
            {
                //MainMenu();
            }
            break;

            case GameState.Street:
            {
                //MainMenu();
            }
            break;
            case GameState.Road:
            {
                //MainMenu();
            }
            break;

            case GameState.Park:
            {
                //MainMenu();
            }
            break;

            case GameState.Apartment:
            {
                if(!startApartmentStory)
                {
                    startApartmentStory = true;
                    AudioManager.Instance.PlaySFX(startingDialogueSfx);
                }

                if(unlockDeityVisi)
                {
                    DeityVision.Instance.UnlockSkill();
                    unlockDeityVisi = false;
                }

            }
            break;

            case GameState.Starting:
            {
                if(!storyStarted)
                {
                    storyStarted = true;
                    AudioManager.Instance.PlayMusic(HouseMusic, 1.5f);
                }
            }
            break;

            case GameState.Win:
            {
                //MainMenu();
            }
            break;

        }

    }

    public void StartGame()
    {
        gameState = GameState.Starting;
    }

    public void UnlockEliasPower()
    {
        unlockDeityVisi = true;
        gameState = GameState.Apartment;
    }

}
