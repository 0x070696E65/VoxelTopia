using CatSdk.Facade;
using CatSdk.Symbol;

public class SymbolConst
{
    public static NetworkType NetworkType = NetworkType.TESTNET;
    public static Network Network = Network.TestNet;
    public static SymbolFacade Facade = new SymbolFacade(Network);
    public static ulong CurrencyMosaicId = 0x72C0212E67A08BCE;
    public static string NODE = "https://mikun-testnet.tk:3001";
    public static string PUBLISH_PUB_KEY = "360FD4320AA0D3F2B9264F9D5840DF4BCBBBB16F37F07F3E38947823E031E7C2";
}