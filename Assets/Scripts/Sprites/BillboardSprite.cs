using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    Transform mainCameraTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCameraTransform = UnityEngine.Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(mainCameraTransform.rotation.eulerAngles.x, 0f, 0f);
    }
}
