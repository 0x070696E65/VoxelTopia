using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase.Storage;
using UnityEngine;
using UnityEngine.Networking;

public class VoxelFireStore
{
    private static readonly string projectId = "voxel-13243";
    
    public static async Task<(string nextPageToken, List<OrgnizedVoxelData> voxels)> GetVoxels(string token, int limit, bool isLatest, string voxelName, string creatorName, string nextPageToken = null)
    {
        var baseUrl = $"https://firestore.googleapis.com/v1beta1/projects/{projectId}/databases/(default)/documents:runQuery";
        var queryJson = CulcQuery(limit, isLatest, voxelName, creatorName, nextPageToken);
        var postData = Encoding.UTF8.GetBytes( queryJson );
        using var request = new UnityWebRequest( baseUrl, UnityWebRequest.kHttpVerbPOST )
        {
            uploadHandler   = new UploadHandlerRaw( postData ),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader( "Content-Type", "application/json" );
        request.SetRequestHeader( "Authorization", $"Bearer {token}" );
        
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            var se = JsonDeserializer.FromJsonArray<VoxelsRoot>(request.downloadHandler.text);
            try
            {
                var voxels = se.Select(v => new OrgnizedVoxelData
                {
                    voxelName = v.document.fields.voxelName.stringValue,
                    metalId = v.document.name.Replace("projects/voxel-13243/databases/(default)/documents/voxels/", ""),
                    creatorName = v.document.fields.creatorName.stringValue,
                    creatorPublicKey = v.document.fields.creatorPublicKey.stringValue,
                    face = v.document.fields.face.stringValue, createTime = v.document.createTime,
                    isPublish = v.document.fields.isPublish.booleanValue,
                    price = int.Parse(v.document.fields.price.integerValue)
                }).ToList();
                var last = se[^1].document.fields.createTime.timestampValue;
                return (last, voxels);
            }
            catch
            {
                return (null, null);
            }
        }
    }
    public static async Task SaveUser(string id, string address, string name, string token)
    {
        var url = $"https://firestore.googleapis.com/v1beta1/projects/{projectId}/databases/(default)/documents/users?documentId={id}";
        var json = $"{{\"fields\": {{\"address\": {{\"stringValue\": \"{address}\"}},\"name\": {{\"stringValue\": \"{name}\"}}}}}}";
        var postData = Encoding.UTF8.GetBytes( json );
        using var request = new UnityWebRequest( url, UnityWebRequest.kHttpVerbPOST )
        {
            uploadHandler   = new UploadHandlerRaw( postData ),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader( "Content-Type", "application/json" );
        request.SetRequestHeader( "Authorization", $"Bearer {token}" );

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            Debug.Log(request.downloadHandler.text);
        }
    }

    public static async Task SaveReceipt(Receipt receipt, string token)
    {
        var url = $"https://firestore.googleapis.com/v1beta1/projects/{projectId}/databases/(default)/documents/receipts";
        var json = $"{{\"fields\": {{" +
                   $"\"metalId\": {{\"stringValue\": \"{receipt.metalId}\"}}," +
                   $"\"creatorPublicKey\": {{\"stringValue\": \"{receipt.creatorPublicKey}\"}}," +
                   $"\"buyerPublicKey\": {{\"stringValue\": \"{receipt.buyerPublicKey}\"}}," +
                   $"\"voxelName\": {{\"stringValue\": \"{receipt.voxelName}\"}}," +
                   $"\"creatorName\": {{\"stringValue\": \"{receipt.creatorName}\"}}," +
                   $"\"price\": {{\"integerValue\": \"{receipt.price}\"}}" +
                   $"}}}}";
        var postData = Encoding.UTF8.GetBytes( json );
        using var request = new UnityWebRequest( url, UnityWebRequest.kHttpVerbPOST )
        {
            uploadHandler   = new UploadHandlerRaw( postData ),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader( "Content-Type", "application/json" );
        request.SetRequestHeader( "Authorization", $"Bearer {token}" );

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            Debug.Log(request.downloadHandler.text);
        }
    }
    
    public static async Task<OrgnizedVoxelData> GetVoxel(string metalId, string token)
    {
        var url = $"https://firestore.googleapis.com/v1beta1/projects/{projectId}/databases/(default)/documents/voxels/{metalId}";
        using var request = UnityWebRequest.Get(url);

        request.SetRequestHeader( "Content-Type", "application/json" );
        request.SetRequestHeader( "Authorization", $"Bearer {token}" );

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            var docment = JsonUtility.FromJson<Document>(request.downloadHandler.text);
            var orgnizedVoxelData = new OrgnizedVoxelData
            {
                metalId = metalId,
                voxelName = docment.fields.voxelName.stringValue,
                creatorPublicKey = docment.fields.creatorPublicKey.stringValue,
                creatorName = docment.fields.creatorName.stringValue,
                isPublish = docment.fields.isPublish.booleanValue,
                face = docment.fields.face.stringValue,
                price = int.Parse(docment.fields.price.integerValue),
                createTime = docment.createTime
            };
            return orgnizedVoxelData;
        }
    }
    
    public static async Task SaveVoxel(string voxelName, string creatorName, string creatorPublicKey, string metalId, string face, string token, bool isPublish, string price)
    {
        var url = $"https://firestore.googleapis.com/v1beta1/projects/{projectId}/databases/(default)/documents/voxels?documentId={metalId}";
        var now = DateTime.Now;
        var createTime = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var json = $"{{\"fields\": {{\"creatorName\": {{\"stringValue\": \"{creatorName}\"}},\"voxelName\": {{\"stringValue\": \"{voxelName}\"}},\"creatorPublicKey\": {{\"stringValue\": \"{creatorPublicKey}\"}},\"face\": {{\"stringValue\": \"{face}\"}},\"isPublish\": {{\"booleanValue\": \"{isPublish}\"}},\"price\": {{\"integerValue\": \"{price}\"}},\"createTime\": {{\"timestampValue\": \"{createTime}\"}}}}}}";
        var postData = Encoding.UTF8.GetBytes( json );
        using var request = new UnityWebRequest( url, UnityWebRequest.kHttpVerbPOST )
        {
            uploadHandler   = new UploadHandlerRaw( postData ),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader( "Content-Type", "application/json" );
        request.SetRequestHeader( "Authorization", $"Bearer {token}" );

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            Debug.Log(request.downloadHandler.text);
        }
    }
    public static async UniTask<string> SaveStorage(byte[] data, string path)
    {
        try
        {
            var token = FirebaseAuth.token;
            var task = new FirebaseStorage(
                    "voxel-13243.appspot.com",
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(token),
                        ThrowOnCancel = true,
                    })
                .Child(path)
                .PutAsync(new MemoryStream(data));
            return await task;
        }
        catch
        {
            throw new Exception("token is invalid");
        }
    }

    public static async UniTask<byte[]> GetDataFromStorage(string path)
    {
        try
        {
            var token = FirebaseAuth.token;
            var task = new FirebaseStorage(
                    "voxel-13243.appspot.com",
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(token),
                        ThrowOnCancel = true,
                    })
                .Child(path)
                .GetDownloadUrlAsync();
            var url = await task;
            var www = UnityWebRequest.Get(url);
            await www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
                throw new Exception(www.error);
            
            return www.downloadHandler.data;
        }
        catch
        {
            throw new Exception("token is invalid");
        }
    }
    
    public static async Task<bool> HasVoxel(string buyerPublicKey, string metalId, string token)
    {
        var baseUrl = $"https://firestore.googleapis.com/v1beta1/projects/{projectId}/databases/(default)/documents:runQuery";
        var queryJson = $@"{{
    ""structuredQuery"": {{
        ""where"": {{
            ""compositeFilter"": {{
                ""filters"": [
                    {{
                        ""fieldFilter"": {{
                            ""field"": {{
                                ""fieldPath"": ""metalId""
                            }},
                            ""op"": ""EQUAL"",
                            ""value"": {{
                                ""stringValue"": ""{metalId}""
                            }}
                        }}
                    }},
                    {{
                        ""fieldFilter"": {{
                            ""field"": {{
                                ""fieldPath"": ""buyerPublicKey""
                            }},
                            ""op"": ""EQUAL"",
                            ""value"": {{
                                ""stringValue"": ""{buyerPublicKey}""
                            }}
                        }}
                    }}
                ],
                ""op"": ""AND""
            }}
        }},
        ""from"": [
            {{
                ""collectionId"": ""receipts""
            }}
        ],
        ""limit"": 1
    }}
}}";

        var postData = Encoding.UTF8.GetBytes( queryJson );
        using var request = new UnityWebRequest( baseUrl, UnityWebRequest.kHttpVerbPOST )
        {
            uploadHandler   = new UploadHandlerRaw( postData ),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader( "Content-Type", "application/json" );
        request.SetRequestHeader( "Authorization", $"Bearer {token}" );

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(request.error);
        }
        {
            var j = JsonDeserializer.FromJsonArray<VoxelsRoot>(request.downloadHandler.text);
            return j[0].document.name != null;
        }
    }
    
    private static string CulcQuery(int limit, bool isLatest, string voxelName, string creatorName, string nextPageToken = null)
    {
        var filter = "";
        filter += voxelName == ""
            ? ""
            : $@"{{
                ""fieldFilter"": {{
                    ""field"": {{
                        ""fieldPath"": ""voxelName""
                    }},
                    ""op"": ""EQUAL"",
                    ""value"": {{
                        ""stringValue"": ""{voxelName}""
                    }}
                }}
            }},";
                    
        filter += creatorName == ""
            ? ""
            : $@"{{
                ""fieldFilter"": {{
                    ""field"": {{
                        ""fieldPath"": ""creatorName""
                    }},
                    ""op"": ""EQUAL"",
                    ""value"": {{
                        ""stringValue"": ""{creatorName}""
                    }}
                }}
            }},";
        filter = filter != "" ? filter.Remove(filter.Length - 1) : "";
        //var op = voxelName == "" || creatorName == "" ? "" : $@"""op"": ""AND""";
        var op = $@"""op"": ""AND""";
        var where = voxelName == "" && creatorName == "" ? "" : $@"
            ""where"": {{
                ""compositeFilter"": {{
                    ""filters"": [
                        {filter}
                    ]{(op == "" ? "" : ",")}
                    {op}
                }}
            }},";
        var direction = isLatest ? "DESCENDING" : "ASCENDING";
        var startAt = nextPageToken == null ? "" : 
            $@"""startAt"": {{
                  ""values"": [
                  {{
                       ""timestampValue"": ""{nextPageToken}""
                  }}
                  ],
                  ""before"": false
           }},";
        return 
            $@"{{
               ""structuredQuery"": {{
                   ""from"": [
                       {{
                           ""collectionId"": ""voxels""
                       }}
                   ],
                   ""orderBy"": [
                       {{
                           ""field"": {{
                               ""fieldPath"": ""createTime""
                           }},
                           ""direction"": ""{direction}""
                       }}
                   ],
                   {where}
                   {startAt}
                   ""limit"": {limit}
               }}
           }}";
    }
}

[Serializable]
public class CreatorName
{
    public string stringValue;
}

[Serializable]
public class CreatorPublicKey
{
    public string stringValue;
}

[Serializable]
public class VoxelName
{
    public string stringValue;
}

[Serializable]
public class IsPublish
{
    public bool booleanValue;
}

[Serializable]
public class Price
{
    public string integerValue;
}

[Serializable]
public class Document
{
    public string name;
    public Fields fields;
    public string createTime;
    public string updateTime;
}

[Serializable]
public class Face
{
    public string stringValue;
}

[Serializable]
public class CreateTime
{
    public string timestampValue;
}

[Serializable]
public class Fields
{
    public CreatorPublicKey creatorPublicKey;
    public VoxelName voxelName;
    public CreateTime createTime;
    public Face face;
    public CreatorName creatorName;
    public IsPublish isPublish;
    public Price price;
}

[Serializable]
public class VoxelsRoot
{
    public Document document;
    public DateTime readTime;
}

[Serializable]
public class CompositeFilter
{
    public List<Filter> filters;
    public string op;
}

[Serializable]
public class Field
{
    public string fieldPath;
}

[Serializable]
public class FieldFilter
{
    public Field field;
    public string op;
    public Value value;
}

[Serializable]
public class Filter
{
    public FieldFilter fieldFilter;
}

[Serializable]
public class From
{
    public string collectionId;
}

[Serializable]
public class QueryRoot
{
    public StructuredQuery structuredQuery;
}

[Serializable]
public class StructuredQuery
{
    public Where where;
    public List<From> from;
    public int limit;
    public StartAt startAt;
}
[Serializable]
public class StartAt
{
    public List<Values> values;
}

[Serializable]
public class Values
{
    public string referenceValue;
}

[Serializable]
public class Value
{
    public string stringValue;
}

[Serializable]
public class Where
{
    public CompositeFilter compositeFilter;
}