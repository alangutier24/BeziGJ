using UnityEngine;

public class PlayerKeys : MonoBehaviour
{

    public enum Keys{
        SecretRoom,
        FrontDoor
    }

    private bool SecretRoomKey = false;

    private bool FrontDoorKey = false;


    public bool hasSecretRoomKey()
    {
        return SecretRoomKey;
    }

    public bool hasFrontDoorKey()
    {
        return FrontDoorKey;
    }

    public void FoundSecretRoomKey()
    {
        Debug.Log("Set secret Key");
        SecretRoomKey = true;
    }

    public void FoundFrontDoorKey()
    {
        FrontDoorKey = true;
    }

}