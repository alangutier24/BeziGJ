using UnityEngine;

public class TriggerStreetCamera : MonoBehaviour
{

    [SerializeField] public SoundDefinition streetMusic;  

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        Player player = other.GetComponent<Player>();
        if(player != null )
        {
            Vector3 pos = player.cam.transform.position;
            Vector3 offset = Vector3.up * 3f;
            Vector3 dist = Vector3.forward * -3f;

            player.GetComponent<IndoorCamera>().offset += offset + dist;

            EnemyManager.Instance.EnableCops();
            AudioManager.Instance.PlayMusic(streetMusic);
        }

        Destroy(gameObject);
    }
}
