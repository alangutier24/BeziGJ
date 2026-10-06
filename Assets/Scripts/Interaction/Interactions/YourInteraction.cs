using UnityEngine;
using DG.Tweening;

public class YourInteraction : MonoBehaviour, IInteractable
{

    [SerializeField]
    private Vector3 _targetRotation = new Vector3(0f, -100f, 0f);

    [SerializeField]
    private float _rotaionSpeed = 3f;

    private bool _isOpen = false;



    public bool CanInteract()
    {
        return true;
    }

    public bool Interact(Interactor interactor)
    {
       if(_isOpen)
        {
            transform.DORotate(-_targetRotation, _rotaionSpeed, RotateMode.WorldAxisAdd);
        }
        else
        {
            transform.DORotate(_targetRotation, _rotaionSpeed, RotateMode.WorldAxisAdd);
        }

        _isOpen = !_isOpen;
        return true;

    }

}
