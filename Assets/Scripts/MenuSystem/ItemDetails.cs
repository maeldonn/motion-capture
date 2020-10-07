using TMPro;
using UnityEngine;

public class ItemDetails : MonoBehaviour
{
    public TextMeshProUGUI text = null;
    public string path = null;

    public ItemDetails(string text, string path)
    {
        text.text = text;
        this.path = path;
    }
}
