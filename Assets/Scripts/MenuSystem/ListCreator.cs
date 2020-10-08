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

    [SerializeField]
    private bool usingArm = false;

    private string[] itemNames = null;
    private string[] itemPaths = null;

    public string getNameFromPath(string path)
    {
        string[] splittedPath = path.Split('/');
        return splittedPath[splittedPath.Length - 1].Split('.')[0];
    }

    public void SetItems()
    {
        string directoryPath = usingArm ? "/BVH/Arm/" : "/BVH/Body/";
        itemPaths = Directory.GetFiles(Application.dataPath + directoryPath, "*.bvh");
        itemNames = new string[itemPaths.Length];
        for (int i = 0; i < itemNames.Length; i++)
        {
            itemNames[i] = getNameFromPath(itemPaths[i]);
        }
    }

    // Use this for initialization
    void Start()
    {
        // Initialize itemNames
        SetItems();

        //setContent Holder Height;
        content.sizeDelta = new Vector2(0, itemNames.Length * 60);

        for (int i = 0; i < itemNames.Length; i++)
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
            //set name and path
            itemDetails.text.text = itemNames[i];
            itemDetails.path = itemPaths[i];
        }
    }
}
