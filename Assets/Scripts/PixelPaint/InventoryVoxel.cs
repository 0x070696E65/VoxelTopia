using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class InventoryVoxel: MonoBehaviour
{
    public string metalId;
    public Toggle toggle;
    public string filePath;
    
    [SerializeField] private Text voxelName;
    [SerializeField] private Text metalIdText;
    [SerializeField] private Image icon;
    
    public Image bg;
    public Sprite onSprite;
    public Sprite offSprite;

    private string address;

    private readonly CancellationTokenSource cts = new();
    private Texture2D iconTexture;
    private Sprite iconSprite;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    public void CreateInventoryVoxel(string _voxelName, string path, string _metalId)
    {
        voxelName.text = _voxelName;
        metalIdText.text = _metalId;
        metalId = _metalId;
        filePath = _metalId == "" ? $"{_voxelName}" : $"{_voxelName}_{_metalId}";
        var iconD = File.ReadAllBytes(path);
        iconTexture = new Texture2D(2, 2) {
            filterMode = FilterMode.Point
        };
        iconTexture.LoadImage(iconD);
        iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
        icon.sprite = iconSprite;
        icon.enabled = true;
    }

    void OnToggleValueChanged(bool isOn)
    {
        bg.sprite = isOn ? onSprite : offSprite;
    }

    private void OnDestroy()
    {
        cts.Cancel();
        Destroy(iconTexture);
        Destroy(iconSprite);
    }
}