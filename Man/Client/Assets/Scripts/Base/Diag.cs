using System.IO;
using UnityEngine;

/// <summary>
/// 轻量诊断：把关键信息追加写到 persistentDataPath/tdj_diag.txt，
/// 便于在真机上复现后从 app 容器里取回（不依赖 console 连接）。
/// 仅用于排障，问题定位后应移除调用。
/// </summary>
public static class Diag
{
    private static int count;
    private const int LIMIT = 300;

    public static void write( string s )
    {
        if ( count >= LIMIT )
            return;

        count++;

        try
        {
            File.AppendAllText( Path.Combine( Application.persistentDataPath , "tdj_diag.txt" ) , s + "\n" );
        }
        catch ( System.Exception )
        {
        }
    }
}
