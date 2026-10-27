using System.IO;
using UnityEngine;

/// <summary>
/// 快速存档 / 快速读档。
/// 全部读写独立文件，完全不占用、也不读取游戏原本的 5 个存档槽和战斗存档。
/// 城镇、营地、RPG 复用游戏自身的存档字节格式；
/// RPG 场景额外记录城镇 ID 与角色坐标，读档后能回到原处。
/// </summary>
public static class GameQuickSave
{
    public const string MODE_CAMP = "camp";
    public const string MODE_BATTLE = "battle";
    public const string MODE_RPG = "rpg";

    // 读档后待落回的 RPG 位置
    static bool pendingPos = false;
    static int pendingTown = 0;
    static int pendingX = 0;
    static int pendingY = 0;
    static int pendingDir = 0;

    static string Dir { get { return GameSetting.PersistentDataPath; } }

    public static string DataPath { get { return Dir + "/QuickSave.dat"; } }
    public static string ModePath { get { return Dir + "/QuickSave.mode"; } }
    public static string RpgPath { get { return Dir + "/QuickSave.rpg"; } }

    // 战斗档单独一份，不碰原版的 SaveBattle.dat
    public static string QuickBattlePath { get { return Dir + "/QuickBattle.dat"; } }

    static string LegacySlotPath { get { return Dir + "/Save" + ( GameDefine.MAX_SAVE - 1 ) + ".dat"; } }
    static string LegacyBakPath { get { return LegacySlotPath + ".quickbak"; } }

    public static bool HasSave
    {
        get
        {
            if ( !File.Exists( ModePath ) )
            {
                return false;
            }

            string mode = File.ReadAllText( ModePath ).Trim();

            if ( mode == MODE_BATTLE )
            {
                return File.Exists( QuickBattlePath );
            }

            if ( mode == MODE_RPG )
            {
                return File.Exists( DataPath ) && File.Exists( RpgPath );
            }

            return File.Exists( DataPath );
        }
    }

    public static bool save()
    {
        GameSceneType scene = GameSceneManager.instance.SceneType;

        bool battle = scene == GameSceneType.Battle;
        bool rpg = scene == GameSceneType.Rpg;

        try
        {
            if ( battle )
            {
                // 写到独立的战斗档，原版 SaveBattle.dat 保持不动
                GameUserData.instance.BattleSavePathOverride = QuickBattlePath;

                GameUserData.instance.saveBattle();
            }
            else
            {
                if ( !GameUserData.instance.saveToFile( DataPath ) )
                {
                    return false;
                }
            }
        }
        catch ( System.Exception e )
        {
            GameUserData.instance.BattleSavePathOverride = null;

            Debug.LogError( "[QuickSave] 存档失败：" + e );

            return false;
        }

        if ( rpg )
        {
            writeRpg();
        }
        else if ( File.Exists( RpgPath ) )
        {
            File.Delete( RpgPath );
        }

        File.WriteAllText( ModePath , battle ? MODE_BATTLE : ( rpg ? MODE_RPG : MODE_CAMP ) );

        return true;
    }

    public static bool load()
    {
        if ( GameSceneManager.instance.IsLoading || !HasSave )
        {
            return false;
        }

        string mode = File.ReadAllText( ModePath ).Trim();

        if ( mode == MODE_BATTLE )
        {
            // loadBattle() 读完后会立即清空该路径
            GameUserData.instance.BattleSavePathOverride = QuickBattlePath;

            if ( GameSceneManager.instance.SceneType == GameSceneType.Battle )
            {
                GameUserData.instance.loadBattle();
            }
            else
            {
                // 战斗场景加载完成后由 sceneLoaded 调 loadBattle()，那时 override 仍有效
                GameSceneManager.instance.loadScene( GameSceneType.Battle , GameSceneLoadMode.LoadBattle );
            }

            return true;
        }

        if ( mode == MODE_RPG && readRpg() )
        {
            // 城镇 ID 不在原生存档里，单独补回来
            GameUserData.instance.setTown( pendingTown );

            GameUserData.instance.loadFromFile( DataPath , GameSceneType.Rpg );

            return true;
        }

        GameUserData.instance.loadFromFile( DataPath , GameSceneType.Camp );

        return true;
    }

    /// <summary>RPG 场景初始化完成后调用，把角色放回存档时的格子</summary>
    public static void applyPendingPos()
    {
        if ( !pendingPos )
        {
            return;
        }

        pendingPos = false;

        GameRPGManager.instance.restorePos( pendingX , pendingY , pendingDir );
    }

    /// <summary>
    /// 早期版本曾借用最后一个存档槽做中转，若当时被系统打断，
    /// 槽位旁会留下 .quickbak。这里把原档还回去，只跑一次。
    /// </summary>
    public static void recoverLegacySlot()
    {
        if ( !File.Exists( LegacyBakPath ) )
        {
            return;
        }

        try
        {
            File.Copy( LegacyBakPath , LegacySlotPath , true );
            File.Delete( LegacyBakPath );

            if ( GameUserData.instance != null )
            {
                GameUserData.instance.loadSaveInfo();
            }
        }
        catch ( System.Exception e )
        {
            Debug.LogError( "[QuickSave] 还原旧版中转槽位失败：" + e );
        }
    }

    static void writeRpg()
    {
        GameRPGManager manager = GameRPGManager.instance;

        if ( manager == null || manager.ActiveID == GameDefine.INVALID_ID )
        {
            return;
        }

        File.WriteAllText( RpgPath , manager.ActiveID + "|" + manager.PosX + "|" + manager.PosY + "|" + manager.Direction );
    }

    static bool readRpg()
    {
        if ( !File.Exists( RpgPath ) )
        {
            return false;
        }

        string[] parts = File.ReadAllText( RpgPath ).Split( '|' );

        if ( parts.Length < 4 )
        {
            return false;
        }

        if ( !int.TryParse( parts[ 0 ] , out pendingTown ) ||
            !int.TryParse( parts[ 1 ] , out pendingX ) ||
            !int.TryParse( parts[ 2 ] , out pendingY ) ||
            !int.TryParse( parts[ 3 ] , out pendingDir ) )
        {
            return false;
        }

        pendingPos = true;

        return true;
    }
}
