using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryManager: MonoBehaviour
{
    [SerializeField] private GameObject publichedVoxelPrefab;
    [SerializeField] private GameObject publishedVoxlesParent;

    [SerializeField] private GameObject loadingPanel;
    
    public Button downLoadButtn;
    
    public GameObject buyArea;
    public Button buyButton;
    public Button closeBuyDisplay;
    
    public GameObject inputPasswordArea;
    public InputField inputPassword;
    public Text warningPassword;
    public Button buySubmitButton;
    public Button closeInputPasswordArea;

    public GameObject copiedVoxel;
    public string nextPageToken;
    private readonly CancellationTokenSource cts = new();

    [Header("Search")] 
    public Button searchButton;
    public Button searchSubmitButton;
    public Button searchCloseButton;
    public GameObject searchPanel;
    public InputField voxelName;
    public InputField creatorName;
    public Toggle latest;
    public Toggle oldest;
    public bool isLatest = true;
    
    [Header("Scroll")]
    public ScrollRect scrollRect;
    public float threshold = 0.1f;
    private bool isNearBottom;
    private int voxelsCount;
    
    private async void Start()
    {
        loadingPanel.SetActive(true);

        voxelName.text = "";
        creatorName.text = "";
        await CreateShopData(null, cts.Token);
        
        buyButton.onClick.AddListener(ShowBuyVoxel);
        closeBuyDisplay.onClick.AddListener(CloseBuyDisplay);
        
        buySubmitButton.onClick.AddListener(BuyBoxel);
        closeInputPasswordArea.onClick.AddListener(()=>inputPasswordArea.SetActive(false));
        
        loadingPanel.SetActive(false);
        
        searchButton.onClick.AddListener(()=>searchPanel.SetActive(true));
        searchCloseButton.onClick.AddListener(()=>searchPanel.SetActive(false));
        latest.onValueChanged.AddListener((value) =>
        {
            latest.isOn = value;
            oldest.isOn = !value;
            isLatest = latest.isOn;
        });
        oldest.onValueChanged.AddListener((value) =>
        {
            latest.isOn = !value;
            oldest.isOn = value;
            isLatest = latest.isOn;
        });

        searchSubmitButton.onClick.AddListener(async () =>
        {
            loadingPanel.SetActive(true);
            ClearShopData();
            searchPanel.SetActive(false);
            await CreateShopData();
            loadingPanel.SetActive(false);
        });
    }

    private void ClearShopData()
    {
        voxelsCount = 0;
        foreach (Transform child in publishedVoxlesParent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private async Task CreateShopData(string _nextPageToken = null,  CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var (s, voxels) = await VoxelFireStore.GetVoxels(FirebaseAuth.token,24, isLatest, voxelName.text, creatorName.text, _nextPageToken);
        if (s == null && voxels == null) return;
        
        nextPageToken = s;
        foreach (var v in voxels)
        {
            var voxel = Instantiate(publichedVoxelPrefab, publishedVoxlesParent.transform, false);
            if (this == null) return;
            var objectTransform = gameObject.transform;
            var currentPosition = objectTransform.position;
            var newPosition = new Vector3(currentPosition.x, currentPosition.y, 0f);
            objectTransform.position = newPosition;
            voxel.transform.localScale = new Vector3(1,1,1);
            var publishedVoxel = voxel.GetComponent<PublishedVoxel>(); 
            publishedVoxel.CreatePublishedVoxel(v.voxelName, v.metalId, v.creatorName, v.creatorPublicKey, v.createTime[..10], v.price, this , cancellationToken);
            voxelsCount++;
            await Task.Delay(100, cancellationToken);
        }
        
        var newHeight = (Mathf.FloorToInt(voxelsCount / 6) + 1) * 230 + 100;
        var rect = publishedVoxlesParent.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, newHeight);
    }
    
    void ShowBuyVoxel()
    {
        buyArea.SetActive(false);
        inputPasswordArea.SetActive(true);
    }

    async void BuyBoxel()
    {
        try
        {
            var publishedVoxel = copiedVoxel.GetComponent<PublishedVoxel>();
            var accpath = Application.persistentDataPath + "/user/account.json";
            if (!File.Exists(accpath)) return;
            var account = await File.ReadAllTextAsync(accpath);
            var acc = JsonUtility.FromJson<FirebaseAuth.Account>(account);
            var privateKey = Crypto.DecryptString(acc.Encrypted, inputPassword.text, acc.Address);

            var receipt = new Receipt
            {
                metalId = publishedVoxel.metalId,
                creatorPublicKey = publishedVoxel.creatorPublicKey,
                buyerPublicKey = acc.PublicKey,
                voxelName = publishedVoxel.voxelName,
                creatorName = publishedVoxel.creatorName,
                price = publishedVoxel.price
            };

            await SymbolManager.TransferTransaction(privateKey, publishedVoxel.address,
                SymbolConst.CurrencyMosaicId.ToString("X16"), (ulong) publishedVoxel.price * 1000000, JsonUtility.ToJson(receipt),
                true,  () => CompletedBuyVoxel(receipt));
            inputPasswordArea.SetActive(false);
        }
        catch(Exception e)
        {
            warningPassword.text = e.Message;
        }
        
    }
    
    public void CloseBuyDisplay()
    {
        buyArea.SetActive(false);
        Destroy(copiedVoxel);
    }

    private async void CompletedBuyVoxel(Receipt receipt)
    {
        Debug.Log($"Complete Transaction");
        Debug.Log(JsonUtility.ToJson(receipt));
        await VoxelFireStore.SaveReceipt(receipt, FirebaseAuth.token);
        await WebSocketManager.websocket.Close();
        DownLoadVoxel(receipt.metalId, receipt.voxelName);
        WebSocketManager.OnConfirmedTransaction -= ()=>CompletedBuyVoxel(receipt);
    }

    public async void DownLoadVoxel(string _metalId, string voxleName)
    {
        loadingPanel.SetActive(true);
        var savePath = $"{Application.persistentDataPath}/voxels/{voxleName}_{_metalId}/";
        var originalPath = $"{Application.persistentDataPath}/voxels/{voxleName}/";
        if (Directory.Exists(savePath))
            Directory.Delete(savePath, true);
        if (Directory.Exists(originalPath))
            Directory.Delete(originalPath, true);
        Directory.CreateDirectory(savePath);
        var dataJsonBytes = await Metal.Fetch(_metalId);
        await File.WriteAllBytesAsync(savePath + "data.json", dataJsonBytes, cts.Token);
        var datastr = await File.ReadAllTextAsync(savePath + "data.json");
        var vdata = JsonUtility.FromJson<Vdata>(datastr);
        var tempList = vdata.face.Select(f => f.textureId).ToList();
        var newList = tempList.Distinct().ToList();
        foreach (var s in newList)
        {
            var texture = await VoxelFireStore.GetDataFromStorage($"voxels/{_metalId}/{s}.png");
            await File.WriteAllBytesAsync($"{savePath}{s}.png", texture, cts.Token);
        }
        var iconD = await VoxelFireStore.GetDataFromStorage($"voxels/{_metalId}/icon.png");
        await File.WriteAllBytesAsync(savePath + "icon.png", iconD, cts.Token);
        buyArea.SetActive(false);
        loadingPanel.SetActive(false);
    } 
    
    private async void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        WebSocketManager.websocket?.DispatchMessageQueue();
#endif
        if (scrollRect == null) return;

        // Scrollbarの値が一番下に近づいたら関数を呼び出す
        var scrollDifference = Mathf.Abs(scrollRect.verticalNormalizedPosition - 0);
        if (scrollDifference < threshold && !isNearBottom)
        {
            isNearBottom = true;
            await OnScrollBottom();
        }
        else if (scrollDifference >= threshold && isNearBottom)
        {
            isNearBottom = false;
        }
    }
    private async Task OnScrollBottom()
    {
        loadingPanel.SetActive(true);
        await CreateShopData(nextPageToken, cts.Token);
        loadingPanel.SetActive(false);
    }

    private async void OnDestroy()
    {
        if (publishedVoxlesParent != null)
        {
            foreach (Transform child in publishedVoxlesParent.transform)
            {
                Destroy(child.gameObject);
            }   
        }
        cts.Cancel();
        if (WebSocketManager.websocket == null) return;
        if(WebSocketManager.websocket.State != WebSocketState.Closed && WebSocketManager.websocket.State != WebSocketState.Closing) await WebSocketManager.websocket.Close();
    }

    private async void OnApplicationQuit()
    {
        if (WebSocketManager.websocket == null) return;
        if(WebSocketManager.websocket.State != WebSocketState.Closed && WebSocketManager.websocket.State != WebSocketState.Closing) await WebSocketManager.websocket.Close();
    }
}

[Serializable]
public class OrgnizedVoxelData
{
    public string metalId;
    public string voxelName;
    public string creatorName;
    public string creatorPublicKey;
    public string face;
    public bool isPublish;
    public int price;
    public string createTime;
}

[Serializable]
public class Receipt
{
    public string metalId;
    public string creatorPublicKey;
    public string buyerPublicKey;
    public string voxelName;
    public string creatorName;
    public int price;
}
