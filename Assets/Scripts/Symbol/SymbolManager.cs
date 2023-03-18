using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CatSdk.CryptoTypes;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Symbol.Factory;
using CatSdk.Utils;
using UnityEngine.Events;
using Network = CatSdk.Symbol.Network;
using NetworkTimestamp = CatSdk.Symbol.NetworkTimestamp;

public class SymbolManager
{
    // 以下のデータは他のシーンなどでも流用できる
    public static string address { get; set; }
    public static string publicKey { get; set; }
    public static string userName { get; set; }
    public static float amount { get; set; }
    
    public static readonly string Node = SymbolConst.NODE;
    public static readonly string WsNode = Node.Replace("https", "wss") + "/ws";
    public static readonly Network network = SymbolConst.Network;
    public static readonly SymbolFacade facade = new (network);
    
    // この関数をボタンにインスペクターから設定する
    public static async UniTask GetAmount(string mosaicId)
    {
        Debug.Log($"{Node}/accounts/{address}");
        var symbolAccountData = await GetData($"{Node}/accounts/{address}");
        var symbolAccountDataJson = JsonUtility.FromJson<Root>(symbolAccountData);
        var xym = symbolAccountDataJson?.account.mosaics.FirstOrDefault(mosaic => mosaic.id == mosaicId);
        if (xym != null) amount = (float) xym.amount / 1000000;
    }
    
    // Apiからデータ取得するための汎用的な関数
    static async UniTask<string> GetData(string url)
    {
        using var webRequest = UnityWebRequest.Get(url);
        await webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ConnectionError) throw new Exception(webRequest.error);
        return webRequest.downloadHandler.text;
    }

    // 転送トランザクション送信 Editor用
    public static async UniTask TransferTransaction(string _priavteKey, string _address, string _mosaicId, ulong _amount, string _message, bool unconfirmed = false, UnityAction function = null)
    {
        var privateKey = new PrivateKey(_priavteKey);
        var keyPair = new KeyPair(privateKey);
        var tx = BuildTransferTransaction(_address, Converter.BytesToHex(keyPair.PublicKey.bytes), _mosaicId, _amount, _message);
        var signature = facade.SignTransaction(keyPair, tx);
        var payload = TransactionsFactory.AttachSignature(tx, signature);
        Debug.Log(payload);
        var hash = facade.HashTransaction(tx);
        WebSocketManager.ConnectWebSocket(WsNode, _address, Converter.BytesToHex(hash.bytes), unconfirmed);
        WebSocketManager.OnConfirmedTransaction += function;
        
        var endpoint = Node + "/transactions";
        var result = await Announce(endpoint, payload);
        Debug.Log(result);
    }
    
    // 転送トランザクション送信 SSS用
    /*public static async UniTask TransferTransaction(string _address, string _mosaicId, ulong _amount, string _message, bool unconfirmed = false, UnityAction function = null)
    {
        var pubKey = SSS.getActivePublicKey();
        var tx = BuildTransferTransaction(_address, pubKey, _mosaicId, _amount, _message);
        var signedPayload = await SSS.SignTransactionByPayloadAsync(Converter.BytesToHex(tx.Serialize()));
        var arr = signedPayload.Split(",");
        var payload = "{ \"payload\" : \"" + arr[0] + "\"}";
        var endpoint = Node + "/transactions";
        var result = await Announce(endpoint, payload);
        Debug.Log(result);
        WebSocketManager.ConnectWebSocket(WsNode, _address, arr[1], unconfirmed);
        WebSocketManager.OnConfirmedTransaction += function;
    }*/
    
    private static TransferTransactionV1 BuildTransferTransaction(string _address, string _pubKey, string _mosaicId, ulong _amount, string _message, ulong _feeMultiplier = 100)
    {
        var _publicKey = new PublicKey(Converter.HexToBytes(_pubKey));
        
        var tx = new TransferTransactionV1
        {
            Network = SymbolConst.NetworkType,
            RecipientAddress = new UnresolvedAddress(Converter.StringToAddress(_address)),
            Mosaics = new UnresolvedMosaic[]
            {
                new()
                {
                    MosaicId = new UnresolvedMosaicId(ulong.Parse(_mosaicId, NumberStyles.HexNumber)),
                    Amount = new Amount(_amount)
                },
            },
            SignerPublicKey = _publicKey,
            Message = Converter.Utf8ToPlainMessage(_message),
            Deadline = new Timestamp(facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp)
        };
        tx.Fee = new Amount(tx.Size * _feeMultiplier);
        return tx;
    }

    private static async UniTask<string> Announce(string endpoint, string payload)
    {
        using var webRequest = UnityWebRequest.Put(endpoint, Encoding.UTF8.GetBytes(payload));
        webRequest.SetRequestHeader("Content-Type", "application/json");
        await webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(webRequest.error);
        }
        return webRequest.downloadHandler.text;
    }
    
    [System.Serializable]
    public class SupplementalPublicKeys
    {
    }
 
    [System.Serializable]
    public class ActivityBucket
    {
        public string startHeight;
        public string totalFeesPaid;
        public int beneficiaryCount;
        public string rawScore;
    }
 
    [System.Serializable]
    public class Mosaic
    {
        public string id;
        public ulong amount;
    }
    [System.Serializable]
    public class Account
    {
        public int version;
        public string address;
        public string addressHeight;
        public string publicKey;
        public string publicKeyHeight;
        public int accountType;
        public SupplementalPublicKeys supplementalPublicKeys;
        public List<ActivityBucket> activityBuckets;
        public List<Mosaic> mosaics;
        public string importance;
        public string importanceHeight;
    }
 
    [System.Serializable]
    public class Root
    {
        public Account account;
        public string id;
    }
}

/*
public static class SSS
{
    [DllImport("__Internal")]
    public static extern string getActivePublicKey();
    
    [DllImport("__Internal")]
    private static extern string signTransactionByPayload(Action<string> cb, string pyaload);
    
    private static UniTaskCompletionSource<string> utcs;
    
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void funcCB(string payload)
    {
        utcs.TrySetResult(payload);  
    }
    
    public static UniTask<string> SignTransactionByPayloadAsync(string payload)
    {
        utcs = new UniTaskCompletionSource<string>();
        signTransactionByPayload(funcCB, payload);
        return utcs.Task;
    }
}*/