using UnityEngine;

public class TriggerChageState : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if(player != null )
            player.stateMachine.ChangeState(player.attackState);
    }
}
