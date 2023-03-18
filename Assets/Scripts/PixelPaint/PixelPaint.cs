using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatSdk.Utils;
using HSVPicker;
using Symvolution.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using Random = UnityEngine.Random;

public enum PaintMode
{
    draw,erace,fill,spoite
}
public class PixelPaint : MonoBehaviour
{
    public ColorPicker picker;
    public RectTransform boardRect;
    private float imageSize;

    public const float offset = 540f;

    private GameInputs gameInputs;
    private Color brushColor;
    
    public byte BoardSize = 16;
    private string filePath;
    [SerializeField] private Button drawMode;
    [SerializeField] private Button eraceMode;
    [SerializeField] private Button fillMode;
    [SerializeField] private Button spoiteMode;
    [SerializeField] private Button allClear;
    [SerializeField] private Button save;
    [SerializeField] private Button load;
    [SerializeField] private Button cloudSave;
    [SerializeField] private Button estimate;
    [SerializeField] private Button symbolSave;
    [SerializeField] private Button close;
    [SerializeField] private Button toShop;
    private PaintMode paintMode;

    [SerializeField] private Button newButton;
    [SerializeField] private GameObject newObj;
    [SerializeField] private InputField newVoxelNameInputField;
    [SerializeField] private Button newSubmitButton;
    [SerializeField] private Button newCloseButton;
    [SerializeField] private Toggle transparency;
    [SerializeField] private Text warning;
    [SerializeField] private List<Button> changeButton;

    [SerializeField] private Image mainImage;
    [SerializeField] private List<Image> images;

    [SerializeField] private List<Button> Mirrors;
    [SerializeField] private List<Image> MirrorsImage;
    public readonly List<byte> MirrorsState = new() {0,0,0,0,0,0};
    public readonly List<Texture2D> textureList = new();
    private readonly List<Sprite> spriteList = new();
    private byte textureNumber;
    
    [SerializeField] private GameObject previewCamera;
    [SerializeField] private TMP_InputField voxelName;

    public Texture2D handCursor;
    public bool isLoaded;

    [SerializeField] private List<Image> presetsImages;

    [SerializeField] private Button Undo;
    [SerializeField] private Button Redo;
    private DoRecord<Color[]> record;

    public Caputure caputure;

    private List<Metal.EstimateData> edatas;
    private Vdata vdatas;
    private bool _transparency;
    private bool isLock;
    
    [SerializeField] private GameObject EstimateConfirmObject;
    [SerializeField] private InputField privPasswordInputField;
    //[SerializeField] private InputField targetPrivKey;
    [SerializeField] private InputField price;
    [SerializeField] private Text voxelNameText;
    [SerializeField] private Text metalIDText;
    [SerializeField] private Text estimateFee;
    [SerializeField] private Toggle isPublich;

    [SerializeField] private GameObject loadListObj;
    [SerializeField] private GameObject listParent;
    [SerializeField] private GameObject loadListButton;
    [SerializeField] private Button closeloadList;

    [SerializeField] private GameObject loadingPanel;
    
    [Header("Localize")]
    [SerializeField] private Text localize1;
    //[SerializeField] private Text localize2;
    [SerializeField] private Text localize3;
    
    private string MainMetalID = "";
    private string Face;
    private readonly CancellationTokenSource cts = new();
    // Start is called before the first frame update
    void Start()
    {
        loadingPanel.SetActive(true);

        SetLocalize();
        Init(cts.Token);
        loadingPanel.SetActive(false);
    }
    private void SetLocalize()
    {
        localize1.font = Localize.GetLocalizeFont();
        //localize2.font = Localize.GetLocalizeFont();
        localize3.font = Localize.GetLocalizeFont();
        localize1.text = Localize.Get("EDITOR1");
        //localize2.text = Localize.Get("EDITOR2");
        localize3.text = Localize.Get("EDITOR3");
    }

    void Init(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Cursor.SetCursor(handCursor, Vector2.zero, CursorMode.Auto);
        picker.CurrentColor = presetsImages[0].color;
        brushColor = picker.CurrentColor;
        picker.onValueChanged.AddListener(color => brushColor = color );
        gameInputs = new GameInputs();
        gameInputs.Enable();
        
        drawMode.onClick.AddListener(() => {
            paintMode = PaintMode.draw;
            drawMode.interactable = false;
            eraceMode.interactable = true;
            fillMode.interactable = true;
            spoiteMode.interactable = true;
        });
        eraceMode.onClick.AddListener(() => {
            paintMode = PaintMode.erace;
            drawMode.interactable = true;
            eraceMode.interactable = false;
            fillMode.interactable = true;
            spoiteMode.interactable = true;
        });
        fillMode.onClick.AddListener(() => {
            paintMode = PaintMode.fill;
            drawMode.interactable = true;
            eraceMode.interactable = true;
            fillMode.interactable = false;
            spoiteMode.interactable = true;
        });
        spoiteMode.onClick.AddListener(() => {
            paintMode = PaintMode.spoite;
            drawMode.interactable = true;
            eraceMode.interactable = true;
            fillMode.interactable = true;
            spoiteMode.interactable = false;
        });
        toShop.onClick.AddListener(() => SceneManager.LoadScene("Shop"));
        paintMode = PaintMode.draw;
        
        allClear.onClick.AddListener(()=> {
            CommitColors();
            AllErace(textureList[textureNumber]);
        });
        save.onClick.AddListener(SaveTexture);
        load.onClick.AddListener(OpenLoad);
        cloudSave.onClick.AddListener(ShowEstimate);
        estimate.onClick.AddListener(Estimate);
        symbolSave.onClick.AddListener(SaveSymbol);
        close.onClick.AddListener(CloseEstimate);
        
        closeloadList.onClick.AddListener(()=>loadListObj.SetActive(false));
        
        newButton.onClick.AddListener(()=>
        {
            isLock = true;
            newObj.SetActive(true);
        });
        newSubmitButton.onClick.AddListener(() =>
        {
            InitCanvas();
            voxelName.text = newVoxelNameInputField.text;
            _transparency = transparency.isOn;
            newObj.SetActive(false);
            isLock = false;
            OnButtons();
            isLoaded = true;
        });
        newCloseButton.onClick.AddListener(()=>
        {
            newObj.SetActive(false);
            isLock = false;
        });
        newVoxelNameInputField.onValueChanged.AddListener((value) => { newSubmitButton.interactable = value != ""; }); 

        isPublich.onValueChanged.AddListener((b)=> price.interactable = b);
        
        for (byte i = 0; i < changeButton.Count; i++) {
            var i1 = i;
            changeButton[i].onClick.AddListener(()=>SwitchTexture(i1));
        }
        for (byte i = 0; i < Mirrors.Count; i++) {
            var i1 = i;
            Mirrors[i].onClick.AddListener(()=>Mirror(i1));
        }
        previewCamera.SetActive(true);
        filePath = Application.persistentDataPath + "/voxels/";

        InitCanvas();
    }

    void OnButtons()
    {
        foreach (var mirror in Mirrors)
        {
            mirror.interactable = true;
        }
        foreach (var change in changeButton)
        {
            change.interactable = true;
        }
        eraceMode.interactable = true;
        fillMode.interactable = true;
        spoiteMode.interactable = true;
        
        allClear.interactable = true;
        save.interactable = true;
        load.interactable = true;
        cloudSave.interactable = true;
        Undo.interactable = true;
        Redo.interactable = true;
    }
    
    void InitCanvas()
    {
        Clear();
        imageSize = offset / BoardSize;
        initAllImages(BoardSize);
        mainImage.sprite = spriteList[0];
        for (var i = 0; i < 6; i++)
            images[i].sprite = spriteList[0];
        textureNumber = 0;
        record = new DoRecord<Color[]>(GetColors(mainImage.sprite.texture), (colors) => {
            var pixelData = mainImage.sprite.texture.GetPixelData<Color32>( 0 );
            for (var i = 0; i < pixelData.Length; i++) 
                pixelData[i] = colors[i];
            mainImage.sprite.texture.Apply();
        });
        record.Reset();
        Redo.onClick.AddListener(() => record.Redo());
        Undo.onClick.AddListener(() => record.Undo());
        _transparency = false;
    }

    private void Update()
    {
        if (!isLoaded) return;
        var mousePos = Mouse.current.position.ReadValue();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, mousePos, Camera.main, out mousePos);
        Redo.interactable = record.enableRedo;
        Undo.interactable = record.enableUndo;
        if (!InBoard(mousePos)) return;
        if (gameInputs.Paint.GetMousePos.IsPressed()) {
            Draw(mousePos);
        }

        if (gameInputs.Paint.GetMousePos.WasReleasedThisFrame()) {
            CommitColors();
        }
    }
    
    private void Draw(Vector2 mousePos)
    {
        if (isLock) return;
        var v2i = new Vector2Int((int)(mousePos.x / imageSize), (int)(mousePos.y / imageSize));
        if (v2i.x + v2i.y * BoardSize > BoardSize * BoardSize) return;

        var pixelData = textureList[textureNumber].GetPixelData<Color32>( 0 );
        switch (paintMode)
        {
            case PaintMode.draw:
                pixelData[v2i.x + v2i.y * BoardSize] = brushColor;
                textureList[textureNumber].Apply();
                break;
            case PaintMode.erace:
                pixelData[v2i.x + v2i.y * BoardSize] = new Color(0, 0, 0, 0);
                textureList[textureNumber].Apply();
                break;
            case PaintMode.fill:
                Fill(textureList[textureNumber], v2i, brushColor);
                break;
            case PaintMode.spoite:
                picker.CurrentColor = pixelData[v2i.x + v2i.y * BoardSize];
                drawMode.onClick.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void Fill(Texture2D texture, Vector2Int position, Color color) {
        var targetColor = texture.GetPixel(position.x, position.y);
        FloodFill(position, targetColor, color, texture);
        texture.Apply();
    }

    void FloodFill(Vector2Int position, Color targetColor, Color replacementColor, Texture2D texture) {
        if (targetColor == replacementColor) {
            return;
        }
        var pixels = new Stack<Vector2Int>();
        pixels.Push(position);
        var count = 0;
        while (pixels.Count > 0 && count < BoardSize * BoardSize)
        {
            var current = pixels.Pop();
            if (texture.GetPixel(current.x, current.y) != targetColor) continue;
            texture.SetPixel(current.x, current.y, replacementColor);
            if (current.x > 0) {
                pixels.Push(new Vector2Int(current.x - 1, current.y));
            }
            if (current.x < texture.width - 1) {
                pixels.Push(new Vector2Int(current.x + 1, current.y));
            }
            if (current.y > 0) {
                pixels.Push(new Vector2Int(current.x, current.y - 1));
            }
            if (current.y < texture.height - 1) {
                pixels.Push(new Vector2Int(current.x, current.y + 1));
            }
            count++;
        }
    }

    private static void AllErace(Texture2D _texture)
    {
        var pixelData = _texture.GetPixelData<Color32>( 0 );
        for ( var i = 0; i < pixelData.Length; i++ ) {
            pixelData[ i ] = new Color32( 0, 0, 0, 0 );
        }
        _texture.Apply();
    }

    private void CommitColors()
    {
        if(mainImage.sprite != null) record.Commit(GetColors(mainImage.sprite.texture));
    }

    private Color[] GetColors(Texture2D _texture)
    {
        var result = new Color[BoardSize * BoardSize];
        var pixelData = _texture.GetPixelData<Color32>( 0 );
        for ( var i = 0; i < pixelData.Length; i++ ) {
            result[ i ] = pixelData[i];
        }
        return result;
    }

    private static bool InBoard(Vector2 pos)
    {
        return pos.x is > 0 and < offset && pos.y is > 0 and < offset;
    }

    private static bool IsTransparent(Texture2D _texture)
    {
        var pixelData = _texture.GetPixelData<Color32>( 0 );
        for ( var i = 0; i < pixelData.Length; i++ ) {
            if (pixelData[i].b != 0 || pixelData[i].g != 0 || pixelData[i].r != 0 || pixelData[i].a != 0) return false;
        }
        return true;
    }

    private void OpenLoad()
    {
        isLock = true;
        loadListObj.SetActive(true);
        
        foreach (Transform child in listParent.transform) {
            Destroy(child.gameObject);
        }
        
        if (!Directory.Exists(filePath)) return;
        var folders = Directory.GetDirectories(filePath);
        folders = folders.OrderBy(s => s).ToArray();
        const float height = 40.0f;
        var setting_count = folders.Length;
        var newHeight = setting_count * height;
        var rect = listParent.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, newHeight);

        foreach (var t in folders)
        {
            var loadobj = Instantiate(loadListButton, listParent.transform, false);
            var folderName = Path.GetFileName(t);
            var arr = folderName.Split("_");
            loadobj.transform.Find("Name").GetComponent<Text>().text = arr[0];
            loadobj.GetComponent<Button>().onClick.AddListener(
                () =>
                {
                    if (!isLoaded)
                    {
                        OnButtons();
                        isLoaded = true;   
                    }
                    MainMetalID = "";
                    LoadTexture(folderName);
                    loadListObj.SetActive(false);
                    voxelName.text = arr[0];
                    if (arr.Length > 1) MainMetalID = arr[1];
                    isLock = false;
                    _transparency = false;
                });
        }
    }

    private void LoadTexture(string folderName)
    {
        if (folderName == "") throw new Exception("no texture name.");
        var loadPath = filePath + folderName + "/";
        if (!Directory.Exists(loadPath))
            throw new Exception("no directory.");
        var datastr = File.ReadAllText(loadPath + "/data.json");
        var vdata = JsonUtility.FromJson<Vdata>(datastr);
        Clear();
        for (var i = 0; i < 6; i++) {
            var _texture = LoadPNG(BoardSize, loadPath + i + ".png");
            if (_texture == null) {
                _texture = new Texture2D(BoardSize, BoardSize) {
                    filterMode = FilterMode.Point
                };
                AllErace(_texture);
            }
            textureList.Add(_texture);
            var _sprite = Sprite.Create(_texture, new Rect(0, 0, _texture.width, _texture.height), Vector2.zero);
            spriteList.Add(_sprite);
        }

        for (var i = 0; i < vdata.palettes.Count; i++) {
            var hexColor = vdata.palettes[i];
            if(!ColorUtility.TryParseHtmlString("#"+hexColor, out var color)) {
                Debug.Log("Failed to convert color");
                continue;
            }
            presetsImages[i].color = color;
            if(!presetsImages[i].gameObject.activeSelf) presetsImages[i].gameObject.SetActive(true);
        }

        for (var i = 0; i < 6; i++)
        {
            MirrorsState[i] = byte.Parse(vdata.face[i].textureId);
            MirrorsImage[i].color = MirrorsState[i] switch
            {
                0 => Color.white,
                1 => Color.blue,
                2 => Color.red,
                3 => Color.green,
                4 => Color.yellow,
                5 => Color.cyan,
                _ => MirrorsImage[i].color
            };
            images[i].sprite = spriteList[MirrorsState[i]];
        }
        mainImage.sprite = spriteList[0];
        CommitColors();
        SwitchTexture(0);
    }
    
    private async void SaveTexture()
    {
        if (voxelName.text == "") throw new Exception("no texture name.");
        
        var metalId = MainMetalID == "" ? "" : $"_{MainMetalID}";
        var savePath = $"{filePath}{voxelName.text}{metalId}/";
        Vdata d;
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            d = new Vdata
            {
                name = voxelName.text,
                type = 0,
                transparency = _transparency,
                face = new FaceData[6]
            };
            for (var i = 0; i < 6; i++)
            {
                d.face[i] = new FaceData();
            }
        }
        else
        {
            var s = await File.ReadAllTextAsync(savePath + "data.json");
            d = JsonUtility.FromJson<Vdata>(s);
            d.name = voxelName.text;
        }
        
        for (byte i = 0; i < 6; i++)
        {
            var i2 = i;
            foreach (var _texture in from f in d.face select textureList[MirrorsState[i2]] into _texture where !IsTransparent(_texture) select _texture)
            {
                await File.WriteAllBytesAsync(savePath + MirrorsState[i] + ".png", _texture.EncodeToPNG());
            }
            d.face[i].textureId = MirrorsState[i].ToString();
        }
        d.palettes.Clear();
        foreach (var hexColor in from presetsImage in presetsImages where presetsImage.gameObject.activeSelf select ColorUtility.ToHtmlStringRGBA(presetsImage.color))
        {
            d.palettes.Add(hexColor);
        }
        caputure.Shot(textureList[MirrorsState[2]], textureList[MirrorsState[0]], textureList[MirrorsState[4]], savePath);
        var json = JsonUtility.ToJson(d);
        await File.WriteAllTextAsync(savePath + "data.json", json);
    }

    public async void SaveSymbol()
    {
        loadingPanel.SetActive(true);
        var directoryPath = filePath + voxelName.text;
        var newDirectoryName = voxelName.text + "_" + MainMetalID;

        var newDirectoryPath = Path.Combine(Path.GetDirectoryName(directoryPath) ?? string.Empty, newDirectoryName);
        if(!Directory.Exists(newDirectoryPath))
            Directory.Move(directoryPath, newDirectoryPath);
        
        var files = Directory.GetFiles(newDirectoryPath, "*.png", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            Debug.Log(file);
            var data = await File.ReadAllBytesAsync(file);
            await VoxelFireStore.SaveStorage(data, $"voxels/{MainMetalID}/{Path.GetFileName(file)}");
        }
        await VoxelFireStore.SaveVoxel(voxelName.text, SymbolManager.userName, SymbolManager.publicKey, MainMetalID, Face, FirebaseAuth.token, isPublich.isOn, price.text);
        
        var result = edatas.SelectMany(e => e.Batches).ToList();
        var res = await Metal.Execute(result);
        Debug.Log(res);
        var json = JsonUtility.ToJson(vdatas);
        await File.WriteAllTextAsync(newDirectoryPath + "/data.json", json);
        CloseEstimate();
        loadingPanel.SetActive(false);
    }

    public async void Estimate()
    {
        try
        {
            warning.text = "";
            SaveTexture();
            Face = "";
            var accpath = Application.persistentDataPath + "/user/account.json";
            if (!File.Exists(accpath)) return;
            var account = await File.ReadAllTextAsync(accpath);
            var acc = JsonUtility.FromJson<FirebaseAuth.Account>(account);
            var sourcePrivateKey = Crypto.DecryptString(acc.Encrypted, privPasswordInputField.text, acc.Address);
            //var targetPrivateKey = targetPrivKey.text == "" ? null : targetPrivKey.text;
            var edata = new List<Metal.EstimateData>();
            var fileName = MainMetalID == "" ? voxelName.text : voxelName.text + "_" + MainMetalID;
            var loadPath = filePath + fileName;
            var dataPath = loadPath + "/data.json";
            if (!File.Exists(dataPath)) throw new Exception("datafile is nothing");
            var dataJson = await File.ReadAllTextAsync(loadPath + "/data.json");
            var vdata = JsonUtility.FromJson<Vdata>(dataJson);
            var finishList = new Dictionary<byte, string>();
            foreach (var face in vdata.face)
            {
                Face += face.textureId + ",";
                if (finishList.Keys.Contains(byte.Parse(face.textureId)))
                {
                    face.metalId = finishList[byte.Parse(face.textureId)];
                    continue;
                }

                var path = loadPath + "/" + face.textureId + ".png";
                if (!File.Exists(path)) throw new Exception("texture is nothing");
                var data = await File.ReadAllBytesAsync(path);
                var d = await Metal.Estimate(data, sourcePrivateKey);
                face.metalId = d.MetalId;
                edata.Add(d);
                finishList.Add(byte.Parse(face.textureId), face.metalId);
            }

            Face = Face.Remove(Face.Length - 1);
            var dj = await Metal.Estimate(Converter.Utf8ToBytes(JsonUtility.ToJson(vdata)), sourcePrivateKey);
            edata.Add(dj);

            if (isPublich.isOn)
            {
                var publichFee = Metal.CreatePublishFeeTransaction(sourcePrivateKey);
                edata.Add(publichFee);
            }

            edatas = edata;
            vdatas = vdata;
            metalIDText.text = dj.MetalId;
            MainMetalID = dj.MetalId;
            var fee = (double) edatas.Select(e => (long) e.TotalFee).Sum() / 1000000;

            fee += isPublich.isOn ? 10 : 0;
            estimateFee.text = fee.ToString(CultureInfo.InvariantCulture);
            symbolSave.interactable = true;
        }
        catch(Exception e)
        {
            warning.text = e.Message;
        }
    }

    private void ShowEstimate()
    {
        isLock = true;
        voxelNameText.text = voxelName.text;
        EstimateConfirmObject.SetActive(true);
    }

    private void CloseEstimate()
    {
        ClearEstimate();
        EstimateConfirmObject.SetActive(false);
    }

    private void ClearEstimate()
    {
        edatas = null;
        vdatas = null;
        privPasswordInputField.text = "";
        //targetPrivKey.text = "";
        voxelNameText.text = "";
        metalIDText.text = "";
        estimateFee.text = "";
        symbolSave.interactable = false;
        isLock = false;
    }
    
    private void Mirror(int index)
    {
        MirrorsState[index]++;
        if (MirrorsState[index] > 5) MirrorsState[index] = 0;
        MirrorsImage[index].color = MirrorsState[index] switch
        {
            0 => Color.white,
            1 => Color.blue,
            2 => Color.red,
            3 => Color.green,
            4 => Color.yellow,
            5 => Color.cyan,
            _ => MirrorsImage[index].color
        };
        images[index].sprite = spriteList[MirrorsState[index]];
    }

    private void SwitchTexture(byte index)
    {
        textureNumber = MirrorsState[index];
        mainImage.sprite = spriteList[MirrorsState[index]];
    }

    private void initAllImages(byte size)
    {
        foreach (var image in images)
        {
            var _texture = new Texture2D(size, size) {
                filterMode = FilterMode.Point
            };
            AllErace(_texture);
            textureList.Add(_texture);
            var _sprite = Sprite.Create(_texture, new Rect(0, 0, _texture.width, _texture.height), Vector2.zero);
            spriteList.Add(_sprite);
            image.sprite = _sprite;
        }
    }
    
    void Clear()
    {
        if (textureList.Count > 0) {
            foreach (var _texture in textureList)
                Destroy(_texture);
            textureList.Clear();
        }

        if (spriteList.Count > 0) {
            foreach (var _sprite in spriteList)
                Destroy(_sprite);
            spriteList.Clear();
        }
    }
    
    private void OnDestroy()
    {
        cts.Cancel();
        Clear();
    }
    
    private static Texture2D LoadPNG(byte size, string filePath) {
        if (!File.Exists(filePath)) return null;
        var fileData = File.ReadAllBytes(filePath);
        var texture = new Texture2D(size, size);
        texture.LoadImage(fileData);
        var change_pixels = new Color[size * size];
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                change_pixels[x + y * size] = texture.GetPixel(x, y);
            }
        }
        Destroy(texture);
        var change_texture = new Texture2D (size, size) {
            filterMode = FilterMode.Point
        };
        change_texture.SetPixels(change_pixels);
        change_texture.Apply();
        return change_texture;
    }
    
    /*private void RandomTexture(IEnumerable<string> colorList)
    {
        /*
         var colorList = new List<string>()
        {
            "#5C832F",
            "#6F9E3F",
            "#769E3F",
            "#7FA03F",
            "#A0C55C",
            "#8CBB4E",
            "#98C34D",
            "#8AB84B",
            "#7FB33F",
            "#6F9E3F",
            "#5C832F",
            "#496728",
            "#4F6E2D",
            "#4C6A29",
            "#41521B",
            "#344317",
        };
         #1#
        var pixelData = textureList[textureNumber].GetPixelData<Color32>( 0 );
        var l = colorList.Select(color =>
        {
            color = color.Replace("#", "");
            return new []
            {
                Converter.HexToBytes(color.Substring(0, 2))[0], Converter.HexToBytes(color.Substring(2, 2))[0],
                Converter.HexToBytes(color.Substring(4, 2))[0],
            };
        }).ToList();
        for (var x = 0; x < pixelData.Length; x++)
        {
            var rand = Random.Range(0, 16);
            pixelData[x] = new Color32(l[rand][0], l[rand][1], l[rand][2], 255);
        }
        textureList[textureNumber].Apply();
    }*/
}

[Serializable]
public class Vdata
{
    public string name;
    public FaceData[] face;
    public string metalId;
    public byte type;
    public List<string> palettes;
    public bool transparency;

    public Vdata()
    {
        name = "";
        face = new FaceData[6];
        transparency = false;
        palettes = new List<string>();
    }
}

[Serializable]
public class FaceData
{
    public string textureId;
    public string metalId;

    public FaceData()
    {
        textureId = "";
        metalId = "";
    }
}