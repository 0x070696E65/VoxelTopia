using System;
using System.Threading;
using System.Threading.Tasks;
using CatSdk.Utils;
using UnityEngine;
using UnityEngine.UI;

public class PublishedVoxel: MonoBehaviour
{
    [SerializeField] private Text metalIdText;
    [SerializeField] private Text creatorNameText;
    [SerializeField] private Text voxelNameText;
    [SerializeField] private Text createTime;
    [SerializeField] private Text priceText;
    [SerializeField] private Image icon;

    public string metalId;
    public string voxelName;
    public string creatorPublicKey;
    public string creatorName;
    public int price;
    public string address;

    private readonly CancellationTokenSource cts = new();
    private Texture2D iconTexture;
    private Sprite iconSprite;

    private InventoryManager inventoryManager;
    private GameObject copy;

    public async void CreatePublishedVoxel(string _voxelName, string _metalId, string _creatorName, string _creatorPublicKey, string _createTime, int _price, InventoryManager _inventoryManager, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        creatorPublicKey = _creatorPublicKey;
        metalIdText.text = _metalId;
        metalId = _metalId;
        voxelNameText.text = _voxelName;
        voxelName = _voxelName;
        creatorNameText.text = _creatorName;
        creatorName = _creatorName;
        if (Metal.symbolService.Network != null)
            address = Converter.AddressToString(Metal.symbolService.Network.Facade.Network.PublicKeyToAddress(_creatorPublicKey).bytes);
        createTime.text = _createTime;
        priceText.text = _price.ToString();
        price = int.Parse(priceText.text);
        var iconD = await VoxelFireStore.GetDataFromStorage($"voxels/{_metalId}/icon.png");
        iconTexture = new Texture2D(2, 2) {
            filterMode = FilterMode.Point
        };
        iconTexture.LoadImage(iconD);
        iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
        if (icon == null) return;
        icon.sprite = iconSprite;
        icon.enabled = true;
        inventoryManager = _inventoryManager;

        var buyButton = GetComponent<Button>();
        buyButton.onClick.AddListener(ShowBuyVoxelDisplay);
    }
    
    private async void ShowBuyVoxelDisplay()
    {
        inventoryManager.CloseBuyDisplay();
        inventoryManager.buyArea.SetActive(true);
        copy = Instantiate(gameObject, inventoryManager.buyArea.transform);
        copy.transform.localScale = new Vector3(2, 2, 1);
        var rectTransform = copy.GetComponent<RectTransform>();
        var newSize = new Vector2(300.0f, 230.0f);
        rectTransform.sizeDelta = newSize;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0, 36.0f);
        Destroy(copy.GetComponent<Button>());
        inventoryManager.copiedVoxel = copy;
        inventoryManager.buyButton.gameObject.SetActive(true);
        inventoryManager.downLoadButtn.gameObject.SetActive(false);
        await CheckHasVoxel(creatorPublicKey, metalId, inventoryManager);
    }
    
    private async Task CheckHasVoxel(string _creatorPublicKey, string _metalId, InventoryManager _inventoryManager)
    {
        var hasVoxel = false;
        if (_creatorPublicKey == SymbolManager.publicKey)
            hasVoxel = true;
        else if(await VoxelFireStore.HasVoxel(_creatorPublicKey, _metalId, FirebaseAuth.token))
            hasVoxel = true;

        if (!hasVoxel) return;
        _inventoryManager.buyButton.gameObject.SetActive(false);
        _inventoryManager.downLoadButtn.gameObject.SetActive(true);
        _inventoryManager.downLoadButtn.onClick.AddListener(()=> _inventoryManager.DownLoadVoxel(_metalId, voxelName));
    }
    
    private void OnDestroy()
    {
        cts.Cancel();
        Destroy(iconTexture);
        Destroy(iconSprite);
        Destroy(gameObject);
    }
}