#if UNITY_EDITOR
using UnityEditor;
using System.Linq;

/// <summary>
/// Firebase가 있으면 FIREBASE_AVAILABLE 심볼을 자동으로 정의
/// </summary>
[InitializeOnLoad]
public class DefineFirebaseSymbol
{
    const string SYMBOL = "FIREBASE_AVAILABLE";

    static DefineFirebaseSymbol()
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';').ToList();

        // Firebase.Firestore 타입이 있는지 확인
        bool hasFirebase = System.Type.GetType("Firebase.Firestore.FirestoreDb, Firebase.Firestore") != null;

        if (hasFirebase)
        {
            // Firebase 있으면 심볼 추가
            if (!defines.Contains(SYMBOL))
            {
                defines.Add(SYMBOL);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
                UnityEngine.Debug.Log($"[DefineFirebaseSymbol] {SYMBOL} 심볼 추가됨");
            }
        }
        else
        {
            // Firebase 없으면 심볼 제거
            if (defines.Contains(SYMBOL))
            {
                defines.Remove(SYMBOL);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
                UnityEngine.Debug.Log($"[DefineFirebaseSymbol] {SYMBOL} 심볼 제거됨");
            }
        }
    }
}
#endif
