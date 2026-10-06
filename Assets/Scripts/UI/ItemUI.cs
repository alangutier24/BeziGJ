using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ItemUI : MonoBehaviour
{
    public GameObject canvas;

    public TMP_Text itemName;

    public Image item;

    void Start()
    {
        canvas.SetActive(false);
    }

    public void ItemPicked(Sprite sprite, string name)
    {
         canvas.SetActive(true);
         itemName.text = name;
         item.sprite = sprite;
    }

    public void HideUI()
    {
        canvas.SetActive(false);
    }

}
