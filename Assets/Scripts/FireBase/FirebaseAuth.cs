using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Google.Cloud.Firestore.V1;
using UnityEngine;

public class FirebaseAuth
{
    public static string token { get; set; }

    public static readonly FirebaseAuthConfig config = new()
    {
        ApiKey = "AIzaSyDebOmkb5q2uJ5eXU5GWUKKMKT577SkI_U",
        AuthDomain = "voxel-13243.firebaseapp.com",
        Providers = new FirebaseAuthProvider[] {
            new EmailProvider()
        }
    };
    
    public static async Task<UserCredential> SignInAsync(string email, string password)
    {
        UserCredential cre;
        try
        {
            var client = new FirebaseAuthClient(config);
            cre = await client.SignInWithEmailAndPasswordAsync(email, password);
        }
        catch (FirebaseAuthException ex)
        {
            Debug.LogError(ex.Message);
            throw new Exception("エラー発生しました！エラーコード：" + ex.Reason);
        }
        return cre;
    }

    /// <summary>
    /// ユーザ作成を行う
    /// </summary>
    public static async Task<UserCredential> SignUpAsync(string email, string password)
    {
        UserCredential cre;
        try
        {
            // 認証するためのオブジェクトを作成
            var client = new FirebaseAuthClient(config);
            // サインアップを行い、リンクを取得する
            cre = await client.CreateUserWithEmailAndPasswordAsync(email, password);
        }
        catch (FirebaseAuthException ex)
        {
            Debug.LogError(ex.Message);
            throw new Exception("エラー発生しました！エラーコード：" + ex.Reason);
        }
        return cre;
    }

    public class Account
    {
        public string Encrypted;
        public string PublicKey;
        public string Address;
        public string Name;
        public string language;
    }
}