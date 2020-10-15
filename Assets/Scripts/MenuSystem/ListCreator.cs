using System.IO;
using CERV.MouvementRecognition.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CERV.MouvementRecognition.Menus
{
    public class ListCreator : MonoBehaviour
    {
        [SerializeField] private Transform SpawnPoint = null;

        [SerializeField] private GameObject item = null;

        [SerializeField] private RectTransform content = null;

        [SerializeField] private GameObject menu = null;

        [SerializeField] private Panel panel = null;

        [SerializeField] private Store store = null;

        private string[] itemNames = null;
        private string[] itemPaths = null;

        private bool lastArm;

        public string GetNameFromPath(string path)
        {
            string[] splittedPath = path.Split('/');
            return splittedPath[splittedPath.Length - 1].Split('.')[0];
        }

        public void SetItems()
        {
            string directoryPath = store.UsingArm ? "/BVH/Arm/" : "/BVH/Body/";
            itemPaths = Directory.GetFiles(Application.dataPath + directoryPath, "*.bvh");
            itemNames = new string[itemPaths.Length];
            for (int i = 0; i < itemNames.Length; i++)
            {
                itemNames[i] = GetNameFromPath(itemPaths[i]);
            }
        }

        public void ShowItems()
        {
            for (int i = 0; i < itemNames.Length; i++)
            {
                // 60 width of item
                float spawnY = i * 60;
                //newSpawn Position
                Vector3 pos = new Vector3(0, -spawnY, 0);
                //instantiate item
                GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.localRotation);
                //setParent
                SpawnedItem.transform.SetParent(SpawnPoint, false);
                //get ItemDetails Component
                ItemDetails itemDetails = SpawnedItem.GetComponent<ItemDetails>();
                //set name and path
                itemDetails.text.text = itemNames[i];
                itemDetails.path = itemPaths[i];
                // Set on click in script
                SpawnedItem.GetComponentInChildren<Button>().onClick
                    .AddListener(delegate { onClick(itemDetails.path); });
                //setContent Holder Height;
                content.sizeDelta = new Vector2(0, itemNames.Length * 60);
            }
        }

        private void onClick(string path)
        {
            menu.GetComponent<MenuManager>().SetCurrentWithHistory(panel);
            store.Path = path;
        }

        public void removeItems()
        {
            foreach (Transform child in SpawnPoint)
            {
                Destroy(child.gameObject);
            }
        }

        private void Start()
        {
            lastArm = store.UsingArm;

            // Initialize itemNames
            SetItems();

            // Display items
            ShowItems();
        }

        private void Update()
        {
            if (store.UsingArm != lastArm)
            {
                removeItems();
                SetItems();
                ShowItems();
                lastArm = store.UsingArm;
            }
        }
    }
}