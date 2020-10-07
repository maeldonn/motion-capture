using System.IO;
using UnityEngine;

public class ListCreator : MonoBehaviour
{

    [SerializeField]
    private Transform SpawnPoint = null;

    [SerializeField]
    private GameObject item = null;

    [SerializeField]
    private RectTransform content = null;

    private ItemDetails[] items = null;

    public void SetItems()
    {
        string[] pathArray = Directory.GetFiles(Application.dataPath + "/BVH/Arm/", "*.bvh");
        items = new ItemDetails[pathArray.Length];
        foreach (string path in pathArray)
        {
            //create a new item details object
            // Set his values with an external function
            // Add to the array
            items.Add("aaaa");
        }
    }

    // Use this for initialization
    void Start()
    {
        // Initialize itemNames
        SetItems();

        //setContent Holder Height;
        content.sizeDelta = new Vector2(0, items.Length * 60);

        for (int i = 0; i < items.Length; i++)
        {
            // 60 width of item
            float spawnY = i * 60;
            //newSpawn Position
            Vector3 pos = new Vector3(SpawnPoint.localPosition.x, -spawnY, SpawnPoint.localPosition.z);
            //instantiate item
            GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.localRotation);
            //setParent
            SpawnedItem.transform.SetParent(SpawnPoint, false);
            //get ItemDetails Component
            ItemDetails itemDetails = SpawnedItem.GetComponent<ItemDetails>();
            //set name
            itemDetails.text.text = items[i];

        }
    }
}
