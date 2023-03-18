using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryFactoryView: MonoBehaviour
{
    [SerializeField] private Button newInventoryButton;
    [SerializeField] private Button loadInventoryButton;
    [SerializeField] private Button closeNewInventoryButton;
    [SerializeField] private Button createInventoryButton;
    
    [SerializeField] private GameObject loadListObj;
    [SerializeField] private GameObject listParent;
    [SerializeField] private GameObject loadListButton;
    [SerializeField] private GameObject newInventoryPanel;
    [SerializeField] private InputField newInventoryName;
    [SerializeField] private Text warningText;
    [SerializeField] private Button closeListObj;
    
    [SerializeField] private GameObject inventoryVoxelPrefab;
    [SerializeField] private GameObject inventoryVoxlesParent;

    [SerializeField] private GameObject loadingPanel;
    
    [SerializeField] private BlockTypeCreator blockTypeCreator;
    private readonly List<GameObject> storageVoxelList = new();
    private readonly Dictionary<string, Vdata> vdatas = new();
    private readonly CancellationTokenSource cts = new();
    private void Start()
    {
        newInventoryButton.onClick.AddListener(OnClickNewInventoryButton);
        closeNewInventoryButton.onClick.AddListener(CloseNewInventory);
        createInventoryButton.onClick.AddListener(async ()=> await CreateInventory());
        loadInventoryButton.onClick.AddListener(OpenLoad);
        closeListObj.onClick.AddListener(()=>loadListObj.SetActive(false));
    }

    private async Task CreateInventory()
    {
        warningText.text = "";
        if (newInventoryName.text == "")
        {
            warningText.text = "name is empty";
            return;
        }
        await SaveInventory();
    }

    private async Task SaveInventory()
    {
        loadingPanel.SetActive(true);
        var d = new List<Vdata>();
        var blockPaths = new List<string>();
        foreach (var voxel in storageVoxelList.Select(v => v.GetComponent<InventoryVoxel>()).Where(voxel => voxel.toggle.isOn).Where(voxel => !voxel.toggle.interactable))
        {
            d.Add(vdatas[voxel.filePath]);
            blockPaths.Add(voxel.filePath);
        }
        foreach (var voxel in storageVoxelList.Select(v => v.GetComponent<InventoryVoxel>()).Where(voxel => voxel.toggle.isOn).Where(voxel => voxel.toggle.interactable))
        {
            d.Add(vdatas[voxel.filePath]);
            blockPaths.Add(voxel.filePath);
        }

        if (d.Count > 63)
        {
            warningText.text = "Maximum number of voxels per world is 64.";
            return;
        }
        var result = new InventoryData
        {
            inventoryName = newInventoryName.text,
            voxels = d
        };
        var json = JsonUtility.ToJson(result);
        
        var filePath = Application.persistentDataPath + "/worlds/";
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);
        
        var savePath = $"{filePath}/{result.inventoryName}/";
        await blockTypeCreator.CreateBlockTypes(savePath, blockPaths);

        await File.WriteAllTextAsync($"{savePath}/data.json", json);
        CloseNewInventory();
        
        await BlockTypeCreator.SetBlockTypes(result.inventoryName);
        World.worldName = result.inventoryName;
        
        SceneManager.LoadScene("WorldMain");
    }

    private void OnClickNewInventoryButton()
    {
        newInventoryPanel.SetActive(true);
        loadListObj.SetActive(false);
        InitFactory();
    }

    private void CloseNewInventory()
    {
        newInventoryName.text = "";
        warningText.text = "";
        ClearStorageVoxelList();
        newInventoryPanel.SetActive(false);
    }
    
    private void OpenLoad()
    {
        CloseNewInventory();
        loadListObj.SetActive(true);
        
        foreach (Transform child in listParent.transform) {
            Destroy(child.gameObject);
        }
        var filePath = Application.persistentDataPath + "/worlds/";
        if (!Directory.Exists(filePath)) return;
        var files = Directory.GetDirectories(filePath);
        files = files.OrderBy(s => s).ToArray();

        const float height = 40.0f;
        var setting_count = files.Length;
        var newHeight = setting_count * height;
        var rect = listParent.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, newHeight);
        
        foreach (var t in files)
        {
            var loadobj = Instantiate(loadListButton, listParent.transform, false);
            var fname = Path.GetFileName(t);
            loadobj.transform.Find("Name").GetComponent<Text>().text = fname;
            var datastr = File.ReadAllText($"{t}/data.json");
            var idata = JsonUtility.FromJson<InventoryData>(datastr);
            
            loadobj.GetComponent<Button>().onClick.AddListener(
                () =>
                {
                    InitFactory();
                    foreach (var vv 
                             in idata.voxels.SelectMany(idataVoxel => 
                                 from v in storageVoxelList 
                                 select v.GetComponent<InventoryVoxel>() into vv 
                                 let arr = vv.filePath.Split("_") 
                                 where arr[0] == idataVoxel.name where arr.Length <= 1 || idataVoxel.metalId == arr[1] 
                                 select vv))
                    {
                        vv.toggle.isOn = true;
                        vv.toggle.interactable = false;
                        vv.bg.sprite = vv.onSprite;
                    }
                    loadListObj.SetActive(false);
                    newInventoryName.text = fname;
                    newInventoryPanel.SetActive(true);
                });
        }
    }

    private void InitFactory()
    {
        var count = 0;
        var filePath = Application.persistentDataPath + "/voxels/";
        if (!Directory.Exists(filePath)) return;
        var folders = Directory.GetDirectories(filePath);
        foreach (var t in folders)
        {
            var folderName = Path.GetFileName(t);
            var arr = folderName.Split("_");
            var metalId = "";
            if (arr.Length > 1) metalId = arr[1];
            var loadPath = filePath + folderName + "/";
            if (!Directory.Exists(loadPath))
                throw new Exception("no directory.");
            var datastr = File.ReadAllText(loadPath + "/data.json");
            var vdata = JsonUtility.FromJson<Vdata>(datastr);
            vdata.metalId = metalId;
            vdatas.Add(folderName, vdata);
            var voxel = Instantiate(inventoryVoxelPrefab, inventoryVoxlesParent.transform, false);
            storageVoxelList.Add(voxel);
            if (gameObject == null) return;
            var objectTransform = gameObject.transform;
            var currentPosition = objectTransform.localPosition;
            var newPosition = new Vector3(currentPosition.x, currentPosition.y, 0f);
            objectTransform.localPosition = newPosition;
            voxel.transform.localScale = new Vector3(1,1,1);
            voxel.GetComponent<InventoryVoxel>().CreateInventoryVoxel(vdata.name, loadPath + "icon.png", metalId);

            count++;
        }
        
        var newHeight = (Mathf.FloorToInt(count / 6) + 1) * 185;
        var rect = inventoryVoxlesParent.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, newHeight);
    }

    private void ClearStorageVoxelList()
    {
        if (storageVoxelList.Count <= 0) return;
        foreach (var v in storageVoxelList)
        {
            Destroy(v);
        }
        storageVoxelList.Clear();
        vdatas.Clear();
    }

    private void OnDestroy()
    {
        if (storageVoxelList.Count > 0)
        {
            foreach (var v in storageVoxelList)
            {
                Destroy(v);
            }
            storageVoxelList.Clear();
        }
        cts.Cancel();
    }
}

[Serializable]
public class InventoryData
{
    public string inventoryName;
    public List<Vdata> voxels;
}