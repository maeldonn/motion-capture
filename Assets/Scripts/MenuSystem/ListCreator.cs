using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListCreator : MonoBehaviour
{

    private Transform SpawnPoint = null;

    private GameObject item = null;

    private RectTransform content = null;

    private int numberOfItems = 0;

    public string[] itemNames = null;
    public Sprite[] itemImages = null;

    // Start is called before the first frame update
    void Start()
    {
        content.sizeDelta = new Vector2(0, numberOfItems * 60);

        for (int i = 0; i < numberOfItems; i++)
        {
            float spawnY = i * 60;
            Vector3 pos = new Vector3(SpawnPoint.position.x, -spawnY, SpawnPoint.position.z);
            GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.rotation);
            SpawnedItem.transform.SetParent(SpawnPoint, false);
            ArmItemDetails armItemDetails = SpawnedItem.GetComponent<ArmItemDetails>();
            armItemDetails.text.text = itemNames[i];
        }
    }
}
