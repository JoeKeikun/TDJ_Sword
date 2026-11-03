using System.IO;
using UnityEngine;

/// <summary>
/// 快速存档 / 快速读档。
/// 全部读写独立文件，完全不占用、也不读取游戏原本的 5 个存档槽和战斗存档。
/// 城镇、营地、RPG 复用游戏自身的存档字节格式；
/// RPG 场景额外记录城镇 ID 与角色坐标，读档后能回到原处。
/// 支持多个档位（SLOT_COUNT），每个档位一套独立文件。
/// </summary>
public static class GameQuickSave
{
    public const int SLOT_COUNT = 3;

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

    public static string dataPath( int slot ) { return Dir + "/QuickSave_" + slot + ".dat"; }
    public static string modePath( int slot ) { return Dir + "/QuickSave_" + slot + ".mode"; }
    public static string rpgPath( int slot ) { return Dir + "/QuickSave_" + slot + ".rpg"; }

    // 战斗档单独一份，不碰原版的 SaveBattle.dat
    public static string quickBattlePath( int slot ) { return Dir + "/QuickBattle_" + slot + ".dat"; }

    // 旧版借用的是当时最后一个槽位（5 槽时代的 Save4.dat），
    // 槽位扩充后不能再跟着 MAX_SAVE 走，固定写死
    static string LegacySlotPath { get { return Dir + "/Save4.dat"; } }
    static string LegacyBakPath { get { return LegacySlotPath + ".quickbak"; } }

    public static bool HasSave( int slot )
    {
        if ( slot < 0 || slot >= SLOT_COUNT )
        {
            return false;
        }

        string mp = modePath( slot );

        if ( !File.Exists( mp ) )
        {
            return false;
        }

        string mode = File.ReadAllText( mp ).Trim();

        if ( mode == MODE_BATTLE )
        {
            return File.Exists( quickBattlePath( slot ) );
        }

        if ( mode == MODE_RPG )
        {
            return File.Exists( dataPath( slot ) ) && File.Exists( rpgPath( slot ) );
        }

        return File.Exists( dataPath( slot ) );
    }

    public static bool save( int slot )
    {
        if ( slot < 0 || slot >= SLOT_COUNT )
        {
            return false;
        }

        GameSceneType scene = GameSceneManager.instance.SceneType;

        bool battle = scene == GameSceneType.Battle;
        bool rpg = scene == GameSceneType.Rpg;

        try
        {
            if ( battle )
            {
                // 写到独立的战斗档，原版 SaveBattle.dat 保持不动
                GameUserData.instance.BattleSavePathOverride = quickBattlePath( slot );

                GameUserData.instance.saveBattle();
            }
            else
            {
                if ( !GameUserData.instance.saveToFile( dataPath( slot ) ) )
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
            writeRpg( slot );
        }
        else if ( File.Exists( rpgPath( slot ) ) )
        {
            File.Delete( rpgPath( slot ) );
        }

        File.WriteAllText( modePath( slot ) , battle ? MODE_BATTLE : ( rpg ? MODE_RPG : MODE_CAMP ) );

        return true;
    }

    public static bool load( int slot )
    {
        if ( slot < 0 || slot >= SLOT_COUNT )
        {
            return false;
        }

        if ( GameSceneManager.instance.IsLoading || !HasSave( slot ) )
        {
            return false;
        }

        string mode = File.ReadAllText( modePath( slot ) ).Trim();

        if ( mode == MODE_BATTLE )
        {
            // loadBattle() 读完后会立即清空该路径
            GameUserData.instance.BattleSavePathOverride = quickBattlePath( slot );

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

        if ( mode == MODE_RPG && readRpg( slot ) )
        {
            // 城镇 ID 不在原生存档里，单独补回来
            GameUserData.instance.setTown( pendingTown );

            GameUserData.instance.loadFromFile( dataPath( slot ) , GameSceneType.Rpg );

            return true;
        }

        GameUserData.instance.loadFromFile( dataPath( slot ) , GameSceneType.Camp );

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

    static void writeRpg( int slot )
    {
        GameRPGManager manager = GameRPGManager.instance;

        if ( manager == null || manager.ActiveID == GameDefine.INVALID_ID )
        {
            return;
        }

        File.WriteAllText( rpgPath( slot ) , manager.ActiveID + "|" + manager.PosX + "|" + manager.PosY + "|" + manager.Direction );
    }

    static bool readRpg( int slot )
    {
        string rp = rpgPath( slot );

        if ( !File.Exists( rp ) )
        {
            return false;
        }

        string[] parts = File.ReadAllText( rp ).Split( '|' );

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
