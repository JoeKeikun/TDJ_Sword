﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public class GameRPGManager : Singleton<GameRPGManager>
{
    [SerializeField]
    int activeID = GameDefine.INVALID_ID;

    [SerializeField]
    GameRPGDTL activeDTL = null;
    [SerializeField]
    GameRPGStage activeRPGStage = null;

    [SerializeField]
    GameRPGInfo activeInfo = null;

    [SerializeField]
    int layerHeight = 0;

    Transform transEvent;
    Transform transBackground;
    Transform transUnit;
    Transform transCell;

    GameObject[] objsLayer1 = null;
    GameObject[] objsLayer0 = null;

    bool isVisibleLayer = true;

    GameRPGMovement movement = null;
    GameAnimation gameAnimation = null;

    public int LayerHeight { get { return layerHeight; } }

    public int ActiveID { get { return activeID; } }

    public GameRPGStage ActiveRPGStage { get { return activeRPGStage; } }

    public int Width { get{ return activeDTL.Width; } }
    public int Height { get { return activeDTL.Height; } }
    public bool IsVisibleLayer { get { return isVisibleLayer; } }

    public int PosX { get { return movement.PosX; } }
    public int PosY { get { return movement.PosY; } }
    public int Direction { get { return gameAnimation == null ? 0 : (int)gameAnimation.animationDirection; } }

    Dictionary<string , GameAnimation> animations = new Dictionary<string , GameAnimation>();


    public override void initSingleton()
    {
        transBackground = GameObject.Find( "RPGScene/Background" ).transform;
        transUnit = GameObject.Find( "RPGScene/Unit" ).transform;
        transCell = GameObject.Find( "RPGScene/Cell" ).transform;
        transEvent = GameObject.Find( "RPGScene/Event" ).transform;
    }

    public void clear()
    {
        visibleLayer( true );

        clearStage();

        activeID = GameDefine.INVALID_ID;
        activeDTL = null;
        activeRPGStage = null;
        activeInfo = null;

        objsLayer1 = null;
        objsLayer0 = null;

        movement = null;
        gameAnimation = null;

        animations.Clear();
    }


    public void visibleLayer( bool v )
    {
        isVisibleLayer = v;

        transBackground.gameObject.SetActive( v );
        transUnit.gameObject.SetActive( v );
        transCell.gameObject.SetActive( v );
    }


    void clearStage()
    {
        GameDefine.DestroyAll( transBackground );
        GameDefine.DestroyAll( transUnit );
        GameDefine.DestroyAll( transCell );
        GameDefine.DestroyAll( transEvent );
    }



    public void active( int id )
    {
        activeID = id;
        activeRPGStage = GameRPGData.instance.getData( activeID );
        activeDTL = activeRPGStage.DTL;
        activeInfo = activeRPGStage.Info;

        byte[] block = new byte[ activeDTL.Width * activeDTL.Height ];
        for ( int i = 0 ; i < activeDTL.Width * activeDTL.Height ; i++ )
        {
            block[ i ] = ( activeDTL.Points[ i ].Move == 0 ? (byte)0 : GameRPGPathFinder.BLOCK );
        }

        GameRPGPathFinder.instance.initMap( activeDTL.Width ,
            activeDTL.Height , block );

        isVisibleLayer = true;
    }

    public void initPos( int i )
    {
        GameRPGPosInfo info = activeInfo.Pos[ i ];

//        GameRPGSceneMovement.instance.moveTo( info.MapPosX , info.MapPosY );

        GameMusicManager.instance.playMusic( 0 , "Music/Music_" + GameDefine.getString2( info.Music ) );

        string path = "Prefab/RPG/Rpgman";

        GameObject obj = Instantiate<GameObject>( Resources.Load<GameObject>( path ) );
        gameAnimation = obj.GetComponent<GameAnimation>();
        obj.name = "Rpgman";

        gameAnimation.playAnimationRPG( GameAnimationType.Stand , GameAnimationDirection.South , null );

        Transform trans = obj.transform;
        trans.SetParent( transUnit );
        trans.localScale = new Vector3( 1.0f , 1.0f , 1.0f );

        movement = obj.AddComponent<GameRPGMovement>();
        movement.setPos( info.PosX , info.PosY );
        movement.setDirection( (GameAnimationDirection)info.Direction );

        GameRPGSceneMovement.instance.followPos( movement.PosBattleX , movement.PosBattleY );
    }

    // 快速读档：直接落回存档时的格子与朝向
    public void restorePos( int x , int y , int direction )
    {
        if ( movement == null )
        {
            return;
        }

        movement.stopMove();
        movement.setPos( x , y );
        movement.setDirection( (GameAnimationDirection)direction );

        GameRPGSceneMovement.instance.followPos( movement.PosBattleX , movement.PosBattleY );
    }

    public void moveUpdate()
    {
        movement.moveUpdate();
    }

    public void moveTo( bool b )
    {
        movement.nextMove = b;
    }

    public void showEvent()
    {
        int posX = movement.PosX;
        int posY = movement.PosY;

        switch ( gameAnimation.animationDirection )
        {
            case GameAnimationDirection.South:
                {
                    if ( posY + 1 == activeDTL.Height )
                    {
                        return;
                    }

                    posY++;
                }
                break;
            case GameAnimationDirection.West:
                {
                    if ( posX == 0 )
                    {
                        return;
                    }

                    posX--;
                }
                break;
            case GameAnimationDirection.North:
                {
                    if ( posY == 0 )
                    {
                        return;
                    }

                    posY--;
                }
                break;
            case GameAnimationDirection.East:
                {
                    if ( posX + 1 == activeDTL.Width )
                    {
                        return;
                    }

                    posX++;
                }
                break;
        }


        GameRPGDTL.Point point = activeDTL.Points[ posY * Width + posX ];

        if ( point.MapEvent <= 0 )
        {
            return;
        }

        GameRPGEventManager.instance.showEvent( point.MapEvent , 0 , null );
    }

    public void moveTo( GameAnimationDirection dir )
    {
        if ( movement.isMoving )
        {
            return;
        }

        int posX = movement.PosX;
        int posY = movement.PosY;

        bool exit = false;

        switch ( dir )
        {
            case GameAnimationDirection.South:
                {
                    if ( posY + 1 == activeDTL.Height )
                    {
                        exit = true;
                        break;
                    }

                    posY++;
                }
                break;
            case GameAnimationDirection.West:
                {
                    if ( posX == 0 )
                    {
                        exit = true;
                        break;
                    }

                    posX--;
                }
                break;
            case GameAnimationDirection.North:
                {
                    if ( posY == 0 )
                    {
                        exit = true;
                        break;
                    }

                    posY--;
                }
                break;
            case GameAnimationDirection.East:
                {
                    if ( posX + 1 == activeDTL.Width )
                    {
                        exit = true;
                        break;
                    }

                    posX++;
                }
                break;
        }

        if ( exit || GameRPGPathFinder.instance.isBlock( posX , posY ) )
        {
            gameAnimation.playAnimationRPG( GameAnimationType.Stand ,
                            dir , null );

            return;
        }

        movement.moveTo( posX , posY , false , true , false , 2 , onMoveOver );
    }

    public void moveTo( int x , int y , OnEventOver over )
    {
        if ( movement.isMoving )
        {
            return;
        }
        
        movement.moveTo( x , y , false , true , false , 2 , over );
    }

    public void stopMove()
    {
        movement.stopMove();
    }

    void onMoveOver()
    {
        GameRPGDTL.Point point = activeDTL.Points[ movement.PosY * Width + movement.PosX ];

        if ( point.RPGEvent <= 0 )
        {
            return;
        }

        moveTo( false );

        GameRPGEventManager.instance.showEvent( point.RPGEvent , 0 , null );
    }

    public void unShowLayer( int id )
    {
        for ( int j = 0 ; j < activeRPGStage.Layer.L0.Length ; j++ )
        {
            if ( activeRPGStage.Layer.L0[ j ].ID == id )
            {
            }
        }
    }

    public void showUnits()
    {

    }

    /// <summary>
    /// 修正个别关卡数据里的底图名。
    /// 第四幕酒店大厅的数据写的是 map04（悬崖外景），实际应该是客栈内景 map01。
    /// 城镇类关卡（5、7、9 幕）的数据都写 map05，但 APK 里同名精灵有两张
    /// （古墓 2048×2048 / 远景城镇 4096×1024），导出时 map05 保留了古墓图，
    /// 正确应为远景城镇，单独存为 map05_town，因此这里按名字统一替换。
    /// 第 26 幕同样是同名冲突（战斗用 map26 4096×2048 / 剧情城镇 4096×1024），
    /// 剧情城镇单独存为 map26_town。
    /// </summary>
    static string getBackgroundName( int stage , string name )
    {
        if ( stage == 4 && name.ToLower().Contains( "map04" ) )
        {
            return "map01";
        }

        if ( name.ToLower().Contains( "map05" ) )
        {
            return "map05_town";
        }

        if ( name.ToLower().Contains( "map26" ) )
        {
            return "map26_town";
        }

        return name;
    }

    /// <summary>
    /// 客栈类底图（2048×2048，内容只占上半部，其下为纯黑留白）的内容底部像素行。
    /// 返回 0 表示该底图不需要特殊对齐。
    /// </summary>
    static float getContentBottom( string name )
    {
        string n = name.ToLower();

        // 第一章客栈
        if ( n.Contains( "map01" ) )
        {
            return 1394.0f;
        }

        // 第四章客栈
        if ( n.Contains( "map04" ) )
        {
            return 1346.0f;
        }

        return 0.0f;
    }

    public void showLayer( int id )
    {
        string path = GameDefine.getString2( activeID );

        if ( objsLayer0 == null ||
            objsLayer0.Length != activeRPGStage.Layer.L0.Length )
            objsLayer0 = new GameObject[ activeRPGStage.Layer.L0.Length ];

        for ( int j = 0 ; j < activeRPGStage.Layer.L0.Length ; j++ )
        {
            if ( activeRPGStage.Layer.L0[ j ].ID == id )
            {
                if ( objsLayer0[ j ] != null )
                {
                    continue;
                }

                if ( activeRPGStage.Layer.L0[ j ].Unknow4 == 10 &&
                        activeRPGStage.Layer.L0[ j ].Unknow5 == 8 )
                {
                    layerHeight = id * 5000;
                }

                if ( activeRPGStage.Layer.L0[ j ].Name.Contains( ".saf" ) )
                {
                    string pathDir = "Prefab/RPG/RPG" + path + "/";
                    string pathTexture = pathDir + activeRPGStage.Layer.L0[ j ].Name.Replace( ".saf" , "" );

                    GameObject imgObject = Resources.Load<GameObject>( pathTexture );

                    if ( imgObject == null )
                    {
                        Debug.LogError( "[RPGBG] prefab missing " + pathTexture );
                    }

                    GameObject obj = Instantiate( imgObject , transBackground );
                    obj.name = activeRPGStage.Layer.L0[ j ].Name;

                    float z = GameDefine.getZ( activeRPGStage.Layer.L0[ j ].Unknow0 );

                    Transform trans = obj.transform;
                    trans.localPosition = new Vector3( 0.0f , layerHeight , z );
                    trans.localScale = new Vector3( 5.0f , 5.0f , 5.0f );

                    GameAnimation animation = obj.GetComponent<GameAnimation>();
                    animation.lightBehindCharacters = true;

                    animation.playAnimation();

                    objsLayer0[ j ] = obj;
                }
                else
                {
                    string layerName = activeRPGStage.Layer.L0[ j ].Name;

                    // 第四幕酒店大厅：数据里写的是 map04（悬崖外景），实际应为客栈内景 map01
                    string bgName = getBackgroundName( activeID , layerName );

                    string pathDir = "Texture/Rpg/";
                    string pathTexture = pathDir + bgName;

                    GameObject imgObject = Resources.Load<GameObject>( "Prefab/Sprite" );
                    GameObject obj = Instantiate( imgObject , transBackground );
                    obj.name = layerName;

                    float z = GameDefine.getZ( activeRPGStage.Layer.L0[ j ].Unknow0 );

                    Transform trans = obj.transform;
                    trans.localPosition = new Vector3( 0.0f , layerHeight , z );
                    trans.localScale = new Vector3( 1.0f , 1.0f , 1.0f );

                    if ( activeRPGStage.Layer.L0[ j ].Unknow4 != 10 &&
                        activeRPGStage.Layer.L0[ j ].Unknow5 != 8 )
                    {
                        trans.localScale = new Vector3( 2.0f , 2.0f , 1.0f );
                    }

                    transUnit.localPosition = new Vector3( 0.0f , 0.0f , -1100 );

                    SpriteRenderer sprite = obj.GetComponent<SpriteRenderer>();
                    sprite.sprite = Resources.Load<Sprite>( pathTexture );

                    if ( sprite.sprite == null )
                    {
                        // 优先使用 APK 未裁切原图：Texture/MapRaw/<Name>
                        // 裁切版存在同名对象互相覆盖的问题，尺寸/内容与原版不一致
                        sprite.sprite = Resources.Load<Sprite>( "Texture/MapRaw/" + bgName );
                    }

                    if ( sprite.sprite == null )
                    {
                        // 数据里部分章节的地图名有误（第二章写成 map00），不能只靠 Name。
                        // 城镇地图编号与当前关卡一致：activeID -> Texture/Map/StageNN/mapNN
                        string town = activeID.ToString( "D2" );
                        sprite.sprite = Resources.Load<Sprite>( "Texture/Map/Stage" + town + "/map" + town );
                    }

                    if ( sprite.sprite == null )
                    {
                        // 再按名字里的编号回退：Map00 / map00-1 -> Texture/Map/Stage00/map00
                        // 取名字里第一段数字的前两位，避免 map00-1 被拼成 Stage001
                        string digits = "";

                        for ( int k = 0 ; k < activeRPGStage.Layer.L0[ j ].Name.Length ; k++ )
                        {
                            char c = activeRPGStage.Layer.L0[ j ].Name[ k ];

                            if ( c >= '0' && c <= '9' )
                            {
                                digits += c;

                                if ( digits.Length == 2 )
                                {
                                    break;
                                }
                            }
                            else if ( digits.Length > 0 )
                            {
                                break;
                            }
                        }

                        if ( digits.Length > 0 )
                        {
                            sprite.sprite = Resources.Load<Sprite>( "Texture/Map/Stage" + digits + "/map" + digits );
                        }
                    }

                    if ( sprite.sprite == null )
                    {
                        string msg = "[RPGBG] sprite missing stage=" + activeID
                            + " layer=" + layerName + " bg=" + bgName
                            + " path=" + pathTexture;

                        Debug.LogError( msg );
                    }
                    else
                    {
                        // 记录实际命中的底图，便于排查数据里的名字错误
                        string msg = "[RPGBG] stage=" + activeID
                            + " layer=" + layerName
                            + " bg=" + bgName
                            + " sprite=" + sprite.sprite.name
                            + " tex=" + sprite.sprite.texture.width + "x" + sprite.sprite.texture.height
                            + " rect=" + sprite.sprite.rect.width + "x" + sprite.sprite.rect.height;

                        Debug.Log( msg );

                        if ( getContentBottom( bgName ) > 0.0f &&
                            activeDTL != null )
                        {
                            // 客栈类底图和本关格子数据不是配套的：
                            // 图片 2048×2048，内容只占上部，下部是纯黑留白，
                            // 按原逻辑贴格子左上角会让最上面几行落在墙面上。
                            // 保持原图 1:1 尺寸不缩放，只把内容底部对齐到格子区域底部。
                            float contentH = getContentBottom( bgName ) /
                                sprite.sprite.pixelsPerUnit;
                            float gridH = activeDTL.Height * GameDefine.TEXTURE_HEIGHT;

                            trans.localScale = Vector3.one;

                            trans.localPosition = new Vector3( 0.0f , layerHeight + contentH - gridH , z );
                        }
                    }

                    objsLayer0[ j ] = obj;
                }

            }
        }


        if ( objsLayer1 == null ||
            objsLayer1.Length != activeRPGStage.Layer.L1.Length )
            objsLayer1 = new GameObject[ activeRPGStage.Layer.L1.Length ];

        for ( int j = 0 ; j < activeRPGStage.Layer.L1.Length ; j++ )
        {
            if ( activeRPGStage.Layer.L1[ j ].ID == id )
            {
                string pathDir = "Prefab/RPG/RPG" + path + "/";
                string pathTexture = pathDir + activeRPGStage.Layer.L1[ j ].Name;

                GameObject imgObject = Resources.Load<GameObject>( pathTexture );
                GameObject obj = Instantiate( imgObject , transBackground );
                obj.name = activeRPGStage.Layer.L1[ j ].Name;

                Transform trans = obj.transform;
                trans.localPosition = new Vector3( activeRPGStage.Layer.L1[ j ].OffsetX ,
                    -activeRPGStage.Layer.L1[ j ].OffsetY + layerHeight ,
                    -1000 - j );

                trans.localScale = new Vector3( 1.0f , 1.0f , 1.0f );

                GameAnimation animation = obj.GetComponent<GameAnimation>();
                animation.lightBehindCharacters = true;

                if ( activeRPGStage.Layer.L1[ j ].Unknow5 == 0 )
                {
                    if ( activeRPGStage.Layer.L1[ j ].Unknow3 == GameDefine.INVALID_ID )
                    {
                        animation.playAnimation();
                    }
                    else
                    {
                    }
                }
                else
                {
                }

                objsLayer1[ j ] = obj;
            }
        }



#if UNITY_EDITOR

        if ( id == 1 )
        {
            GameRPGDTL dtl = activeRPGStage.DTL;
            int ii = 0;

            Sprite spriteObject = Resources.Load<Sprite>( "White" );

            for ( int i = 0 ; i < dtl.Height ; i++ )
            {
                for ( int j = 0 ; j < dtl.Width ; j++ )
                {
                    // editor test

                    GameObject imgObject = Resources.Load<GameObject>( "Prefab/Sprite" );
                    GameObject obj = Instantiate( imgObject , transCell );
                    obj.name = i + "-" + j;

                    Transform trans = obj.transform;
                    trans.localPosition = new Vector3( j * 30 , -i * 24 + layerHeight , 0.0f );
                    //                    trans.localScale = new Vector3( 1.0f , 1.0f , 1.0f );

                    SpriteRenderer sprite = obj.GetComponent<SpriteRenderer>();
                    sprite.color = new Color( 1.0f , 1.0f , 1.0f , ( dtl.Points[ ii ].Move == 1 ? 0.2f : 0.0f ) );
                    sprite.sprite = spriteObject;

                    GamePointTset test = obj.AddComponent<GamePointTset>();
                    test.Value0 = dtl.Points[ ii ].Move;
                    test.Value1 = dtl.Points[ ii ].MapEvent;
                    test.Value2 = dtl.Points[ ii ].RPGEvent;

                    if ( dtl.Points[ ii ].MapEvent > 0 )
                    {
                        sprite.color = new Color( 0.0f , 1.0f , 0.0f , 0.25f );
                    }
                    else if ( dtl.Points[ ii ].RPGEvent > 0 )
                    {
                        sprite.color = new Color( 1.0f , 0.0f , 0.0f , 0.25f );
                    }

                    ii++;
                }
            }

        }

#endif

    }

    public bool checkPosition( float x , float y )
    {
        if ( x < 0 || y > 0 ||
            x + GameCameraManager.instance.sceneWidth > activeDTL.Width * GameDefine.TEXTURE_WIDTH ||
            y - GameCameraManager.instance.sceneHeight < -activeDTL.Height * GameDefine.TEXTURE_HEIGHT )
        {
            return false;
        }

        return true;
    }

    public int getZ( int x , int y )
    {
        return -y;
    }


    public void addAnimation( int id , int id2 )
    {
        GameAnimation ani = null;

        string str = id + " " + id2;

        if ( animations.ContainsKey( str ) )
        {
            ani = animations[ str ];
            ani.stopAnimation();
            ani.clearAnimation();
            Destroy( ani.gameObject );
            ani.transform.SetParent( null );
            animations.Remove( str );
        }

        for ( int i = 0 ; i < activeRPGStage.Layer.L1.Length ; i++ )
        {
            if ( activeRPGStage.Layer.L1[ i ].ID == id &&
                activeRPGStage.Layer.L1[ i ].Parm == id2 )
            {
                string path = "Prefab/RPG/RPG" + GameDefine.getString2( activeID ) + "/";
                path += activeRPGStage.Layer.L1[ i ].Name;

                GameObject obj = Instantiate<GameObject>( Resources.Load<GameObject>( path ) );
                ani = obj.GetComponent<GameAnimation>();
                ani.lightBehindCharacters = true;

                Transform trans = obj.transform;
                trans.SetParent( transEvent );
                // 事件动画也要用 L1 里的偏移（与 showLayer 的摆放规则一致），
                // 否则客栈房间等剧情叠层会被顶到屏幕上方，左上角只露出人物/火光的一截
                trans.localPosition = new Vector3( activeRPGStage.Layer.L1[ i ].OffsetX ,
                    -activeRPGStage.Layer.L1[ i ].OffsetY + layerHeight , 0.0f );
                trans.localScale = new Vector3( 1.0f , 1.0f , 1.0f );

                animations.Add( str , ani );

                return;
            }
           
        }

    }

    public void playAnimation( int id , int id2 )
    {
        string str = id + " " + id2;

        if ( !animations.ContainsKey( str ) )
        {
            return;
        }

#if UNITY_EDITOR
        Debug.Log( "playAnimation " + animations[ str ].gameObject.name );
#endif

        GameAnimation animation = animations[ str ];
        animation.playAnimation( 0 , GameDefine.INVALID_ID , false );
    }

    public void clearGameAnimation()
    {
        gameAnimation.stopAnimation();
        gameAnimation.clearAnimation();
    }

    public void playAnimation( int id , int id2 , int frame , int endFrame , OnEventOver over )
    {
        string str = id + " " + id2;

        if ( !animations.ContainsKey( str ) )
        {
            return;
        }

#if UNITY_EDITOR
        Debug.Log( "playAnimation " + animations[ str ].gameObject.name );
#endif

        GameAnimation animation = animations[ str ];

        Transform trans = animation.transform;
        trans.localPosition = new Vector3( GameRPGSceneMovement.instance.PosXReal ,
            GameRPGSceneMovement.instance.PosYReal + layerHeight ,
            -id2 * 20 );

        if ( frame == GameDefine.INVALID_ID )
        {
            animation.playAnimation( 0 , GameDefine.INVALID_ID , false , over );
        }
        else
        {
            if ( frame == endFrame )
            {
                animation.stopAnimation();
                animation.showFrame( endFrame );

                if ( over != null )
                {
                    over();
                }
            }
            else
            {
                animation.playAnimation( frame , endFrame + 1 , false , over );
            }

            //                animation.stopAnimation();
            //               animation.showFrame( frame );             
        }
    }

    public void clearAnimation( int id , int id2 )
    {
        string str = id + " " + id2;

        if ( !animations.ContainsKey( str ) )
        {
            return;
        }

        GameAnimation animation = animations[ str ];
        animation.stopAnimation();
        animation.clearAnimation();
    }

}


