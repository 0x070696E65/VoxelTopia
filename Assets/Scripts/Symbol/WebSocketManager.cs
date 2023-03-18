using System;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;

public class WebSocketManager
{
    public static UnityAction OnConfirmedTransaction { get; set; }
    public static WebSocket websocket { get; set; }
    
    public static async void ConnectWebSocket(string wsNode, string recipientAddress, string hash, bool unconfirmed = false)
    {
        websocket = new WebSocket(wsNode);
        websocket.OnOpen += () => { Debug.Log("WebSocket opened. " + wsNode); };
        websocket.OnError += errMsg => Debug.Log($"WebSocket Error Message: {errMsg}");
        websocket.OnClose += code => Debug.Log("WS closed with code: " + code);

        websocket.OnMessage += async (msg) =>
        {
            var data = Encoding.UTF8.GetString(msg);
            Debug.Log(data);
            var rootData = JsonUtility.FromJson<RootData>(data);
            if (rootData.uid != null)
            {
                var body = $"{{\"uid\":\"{rootData.uid}\", \"subscribe\":\"block\"}}";
                await websocket.SendText(body);
                var c = unconfirmed ? "unconfirmedAdded" : "confirmedAdded";
                var confirmed = $"{{\"uid\":\"{rootData.uid}\", \"subscribe\":\"{c}/{recipientAddress}\"}}";
                await websocket.SendText(confirmed);
            }
            else
            {
                var root = JsonUtility.FromJson<Root>(data);
                if (root.topic == "block")
                {
                    Debug.Log("new block:");
                }
                else if (root.topic.Contains("confirmed"))
                {
                    if (root.data.meta.hash != hash) return;
                    await websocket.Close();
                    OnConfirmedTransaction?.Invoke();
                }
                else
                {
                    Debug.Log("else e.Data :" + data);
                }
            }
        };
        await websocket.Connect();
    }
    
    [Serializable]
    public class Mosaic
    {
        public string id;
        public string amount;
    }

    [Serializable]
    public class WsTransaction
    {
        public string signature;
        public string signerPublicKey;
        public int version;
        public int network;
        public int type;
        public string maxFee;
        public string deadline;
        public string recipientAddress;
        public string secret;
        public string proof;
        public List<Mosaic> mosaics;
    }

    [Serializable]
    public class Meta
    {
        public string hash;
        public string merkleComponentHash;
        public string height;
    }

    [Serializable]
    public class WaTransactionData
    {
        public WsTransaction transaction;
        public Meta meta;
    }

    [Serializable]
    public class Root
    {
        public string topic;
        public WaTransactionData data;
    }

    [Serializable]
    public class RootData
    {
        public string uid;
    }
}