using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Symbol.Factory;
using CatSdk.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

[Serializable]
public class Metal
{
    public static SymbolService symbolService;
    public static MetalService metalService;
    
    public static async Task Init(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = new SymbolServiceConfig(SymbolConst.NODE);
        symbolService = new SymbolService(config, GetJsonAsync);
        await symbolService.Init();
        metalService = new MetalService(symbolService);
    }
    public static async Task<EstimateData> Estimate(byte[] fileData, string sourcePrivKey, string targetPrivKey = null)
    {
        EstimateData estimateData = null;
        try
        {
            var sourcePrivateKey = new PrivateKey(sourcePrivKey);
            var sourceKeyPair = new KeyPair(sourcePrivateKey);
            var random = CatSdk.CryptoTypes.PrivateKey.Random();
            var targetPrivateKey = new PrivateKey(random.bytes);
            var targetKeyPair = new KeyPair(targetPrivateKey);
            
            // トランザクション構築
            var (key, txs, _) =
                await metalService.CreateForgeTxs(sourceKeyPair.PublicKey, targetKeyPair.PublicKey, fileData);

            // MetalIDの確認
            var metalId = metalService.CalculateMetalId(
                MetadataType.Account,
                sourceKeyPair.PublicKey,
                targetKeyPair.PublicKey,
                key
            );

            // 署名

            var batches = symbolService.BuildSignedAggregateCompleteTxBatches(
                txs,
                sourceKeyPair,
                new List<KeyPair>() {targetKeyPair}
            );

            // 手数料確認
            var totalFee = (ulong)batches.Select(batch => (long) batch.Fee.Value).Sum();

            estimateData = new EstimateData(metalId, batches, totalFee);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        return estimateData;
    }

    public static EstimateData CreatePublishFeeTransaction(string sourcePrivKeyStr)
    {
        if (symbolService.Network == null) throw new NullReferenceException("network is null");
        var facade = symbolService.Network.Facade;
        
        var sourcePrivKey = new PrivateKey(Converter.HexToBytes(sourcePrivKeyStr));
        var sourcePair = new KeyPair(sourcePrivKey);
        
        var publicKey = new PublicKey(Converter.HexToBytes(SymbolConst.PUBLISH_PUB_KEY));
        var targetAddress = new UnresolvedAddress(facade.Network.PublicKeyToAddress(publicKey.bytes).bytes);
        
        var transferTransaction = new EmbeddedTransferTransactionV1()
        {
            Network = SymbolConst.NetworkType,
            SignerPublicKey = sourcePair.PublicKey,
            RecipientAddress = targetAddress,
            Mosaics = new UnresolvedMosaic[]
            {
                new()
                {
                    MosaicId = new UnresolvedMosaicId(SymbolConst.CurrencyMosaicId),
                    Amount = new Amount(10000000)
                }
            },
            Message = Converter.Utf8ToPlainMessage("Publish Fee")
        };
        var innerTransactions = new IBaseTransaction[]
        {
            transferTransaction
        };
        var merkleHash = SymbolFacade.HashEmbeddedTransactions(innerTransactions);

        var aggTx = new AggregateCompleteTransactionV2 {
            Network = SymbolConst.NetworkType,
            Transactions = 	innerTransactions,
            SignerPublicKey = sourcePair.PublicKey,
            TransactionsHash = merkleHash,
            Deadline = new Timestamp(facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp),
        };
        aggTx.Fee = new Amount(aggTx.Size * symbolService.Config.FeeRatio);
        
        var aliceSignature = facade.SignTransaction(sourcePair, aggTx);
        TransactionsFactory.AttachSignature(aggTx, aliceSignature);

        return new EstimateData("", new List<AggregateCompleteTransactionV2>() {aggTx}, aggTx.Fee.Value);
    }

    public static async Task<string> Execute(List<AggregateCompleteTransactionV2> _batches)
    {
        return await symbolService.ExecuteBatches(_batches);
    }

    public static async Task<byte[]> Fetch(string metalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await metalService.FetchByMetalId(metalId);
        return result.Payload;
    }

    public static async Task<string> GetJsonAsync(string url)
    {
        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            return request.downloadHandler.text;
        }
    } 

    public class EstimateData
    {
        public string MetalId;
        public List<AggregateCompleteTransactionV2> Batches;
        public ulong TotalFee;
        
        public EstimateData(string _metalId, List<AggregateCompleteTransactionV2> _batches, ulong _totalFee)
        {
            MetalId = _metalId;
            Batches = _batches;
            TotalFee = _totalFee;
        }
    }
    
    public class VoxelMetadata
    {
        public string metalId;
        public bool isPublich;
        public string creatorPubKey;
        public int price;
    }
}