using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;
using Symvolution.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseManager: MonoBehaviour
{
    [SerializeField] private GameObject SignUpPanel;
    [SerializeField] private GameObject SignInPanel;
    
    [SerializeField] private Button SignUpButton;
    [SerializeField] private Button SignInButton;
    
    [SerializeField] private InputField privKeyInputField;
    [SerializeField] private InputField addressInputField;
    [SerializeField] private InputField passwordInputField;
    [SerializeField] private InputField nameInputField;
    [SerializeField] private Text signUpWarning;
    [SerializeField] private Button signeUp;
    
    [SerializeField] private InputField signInAddressInputField;
    [SerializeField] private InputField signInPasswordInputField;
    [SerializeField] private Text signInWarning;
    [SerializeField] private Button signeIn;
    
    [SerializeField] private GameObject loadingPanel;
    
    [Header("Localize")]
    [SerializeField] private List<Font> fonts;
    [SerializeField] private Button JpButton;
    [SerializeField] private Button EnButton;
    [SerializeField] private Text localize1;
    [SerializeField] private Text localize2;

    private readonly CancellationTokenSource cts = new();
    private FirebaseAuth.Account acc;
    
    
    private async void Start()
    {
        loadingPanel.SetActive(true);
        signeUp.onClick.AddListener(SignUpAsync);
        signeIn.onClick.AddListener(SignInAsync);
        SignInButton.gameObject.GetComponent<Text>().color = Color.grey;
        SignUpButton.onClick.AddListener(() =>
        {
            SignUpPanel.SetActive(true);
            SignInPanel.SetActive(false);
            SignUpButton.gameObject.GetComponent<Text>().color = Color.white;
            SignInButton.gameObject.GetComponent<Text>().color = Color.grey;
        });
        SignInButton.onClick.AddListener(() =>
        {
            SignUpPanel.SetActive(false);
            SignInPanel.SetActive(true);
            SignUpButton.gameObject.GetComponent<Text>().color = Color.grey;
            SignInButton.gameObject.GetComponent<Text>().color = Color.white;
        });
        
        Localize.Init(fonts, Localize.JP);
        JpButton.onClick.AddListener(()=>
        {
            SetLocalize(Localize.JP);
            SaveLocalize(Localize.JP);
        });
        EnButton.onClick.AddListener(()=>
        {
            SetLocalize(Localize.EN);
            SaveLocalize(Localize.EN);
        });
        
        var accpath = Application.persistentDataPath + "/user/account.json";
        if (File.Exists(accpath))
        {
            var account = await File.ReadAllTextAsync(accpath);
            acc = JsonUtility.FromJson<FirebaseAuth.Account>(account);
            SignInButton.onClick.Invoke();
            signInAddressInputField.text = acc.Address; 
            SetLocalize(acc.language);
        }
        else
        {
            SetLocalize(Localize.JP);
        }
        await Metal.Init(cts.Token);
        loadingPanel.SetActive(false);
    }

    private void SaveLocalize(string langage)
    {
        var accpath = Application.persistentDataPath + "/user/account.json";
        if (!File.Exists(accpath)) return;
        var account = File.ReadAllText(accpath);
        acc = JsonUtility.FromJson<FirebaseAuth.Account>(account);
        acc.language = langage;
        File.WriteAllText(accpath, JsonUtility.ToJson(acc));
    }

    private void SetLocalize(string lungage)
    {
        Localize.SetLocalizeKey(lungage);

        localize1.font = Localize.GetLocalizeFont();
        localize2.font = Localize.GetLocalizeFont();
        localize1.text = Localize.Get("AUTH1");
        localize2.text = Localize.Get("AUTH2");
    }

    private async void SignUpAsync()
    {
        if (!VerifySigneUp()) return;
        try
        {
            loadingPanel.SetActive(true);
            var filePath = Application.persistentDataPath + "/user/";
            Debug.Log(filePath);
            var privKey = new PrivateKey(privKeyInputField.text);
            var keypair = new KeyPair(privKey);
            acc = new FirebaseAuth.Account
            {
                Address = addressInputField.text,
                Name = nameInputField.text,
                PublicKey = Converter.BytesToHex(keypair.PublicKey.bytes),
                Encrypted = Crypto.EncryptString(privKeyInputField.text, passwordInputField.text, addressInputField.text),
                language = Localize.JP
            };
            var json = JsonUtility.ToJson(acc);
            if (Directory.Exists(filePath))
                Directory.Delete(filePath, true);
            Directory.CreateDirectory(filePath);
            await File.WriteAllTextAsync(filePath + "account.json", json);
            FirebaseAuth.token = await (await FirebaseAuth.SignUpAsync(addressInputField.text + "@mail.com", passwordInputField.text)).User.GetIdTokenAsync();
            Debug.Log(FirebaseAuth.token);
            await VoxelFireStore.SaveUser(acc.PublicKey, acc.Address, acc.Name, FirebaseAuth.token);
            SaveAccountCache(acc.Address, acc.PublicKey, acc.Name);
            SceneManager.LoadScene("Editor");
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
            signUpWarning.text = e.Message;
        }
    }

    private void SaveAccountCache(string address, string publicKey, string userName)
    {
        SymbolManager.address = address;
        SymbolManager.publicKey = publicKey;
        SymbolManager.userName = userName;
    }

    private async void SignInAsync()
    {
        if (!VerifySigneIn()) return;
        try
        {
            loadingPanel.SetActive(true);
            FirebaseAuth.token = await (await FirebaseAuth.SignInAsync(signInAddressInputField.text + "@mail.com", signInPasswordInputField.text)).User.GetIdTokenAsync();
            Debug.Log("Sign In!!");
            SaveAccountCache(acc.Address, acc.PublicKey,acc.Name);
            SceneManager.LoadScene("Editor");   
        }
        catch(Exception e)
        {
            signInWarning.text = e.Message;
        }
    }
    
    private bool VerifySigneIn()
    {
        signInWarning.text = "";
        if (signInAddressInputField.text.Length != 39) signInWarning.text += "アドレスが正しい長さではありません\n";

        const string pattern = @"^[a-zA-Z0-9]+$";
        var isMatched = Regex.IsMatch(signInPasswordInputField.text, pattern);
        if(!isMatched) signInWarning.text += "パスワードは半角英数のみです\n";
        if(signInPasswordInputField.text.Length < 8) signInWarning.text += "パスワードは8文字以上です\n";
        
        return signInWarning.text == "";
    }

    private bool VerifySigneUp()
    {
        signUpWarning.text = "";
        if (privKeyInputField.text.Length != 64)
        {
            signUpWarning.text += "秘密鍵が正しい形式ではありません\n";
        }
        else
        {
            var privKey = new PrivateKey(privKeyInputField.text);
            var keypair = new KeyPair(privKey);
            var address = Converter.AddressToString(SymbolConst.Facade.Network.PublicKeyToAddress(keypair.PublicKey.bytes).bytes);
            Debug.Log(address);
            if (addressInputField.text != address) signUpWarning.text += "秘密鍵から算出されたアドレスと一致しません\n";
        }

        const string pattern = @"^[a-zA-Z0-9]+$";
        var isMatched = Regex.IsMatch(passwordInputField.text, pattern);
        if(!isMatched) signUpWarning.text += "パスワードは半角英数のみです\n";
        if(passwordInputField.text.Length < 8) signUpWarning.text += "パスワードは8文字以上です\n";
        if(nameInputField.text.Length < 4) signUpWarning.text += "名前は4文字以上です\n";
        var isMatchedName = Regex.IsMatch(nameInputField.text, pattern);
        if(!isMatchedName) signUpWarning.text += "名前は半角英数のみです\n";
        return signUpWarning.text == "";
    }
}