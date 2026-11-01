﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[System.Serializable]
public class GameString
{
    [SerializeField]
    string stringT;
    [SerializeField]
    string stringS;

    public void init( string str )
    {
        stringS = str;
        stringT = ChineseStringUtility.ToTraditional( str );
    }

    public string String
    {
        get
        {
            switch ( GameSetting.instance.location )
            {
                case GameSetting.GameLocation.SimplifiedChinese:
                    return stringS;
                case GameSetting.GameLocation.TraditionalChinese:
                    return stringT;
            }
            return "";
        }
    }

    public string Simplified { get { return stringS; } }
    public string Traditional { get { return stringT; } set { stringT = value; } }
}

public enum GameStringType
{
    Title0 = 0,
    Title1,
    Title2,
    Title3,

    Shop0,
    Shop1,
    Shop2,
    
    Bag0,

    Camp0,
    Camp1,
    Camp2,
    Camp3,
    Camp4,
    Camp5,
    Camp6,

    CampExit0,
    CampExit1,

    ItemUI0,
    ItemUI1,
    ItemUI2,
    ItemUI3,

    AlchemyUI0,
    AlchemyUI1,

    PowerUI0,

    BattleInfoUI0,
    BattleInfoUI1,

    GameOver0,
    GameOver1,
    GameOver2,
    GameOver3,


    SL0,
    SL1,
    SL2,
    SL3,
    SL4,

    Time0,

    Agreement0,

    Help0,
    Help1,
    Help2,
    Help3,
    Help4,
    Help5,
    Help6,
    Help7,

    Help00,
    Help10,
    Help20,
    Help30,
    Help40,

    Setting0,
    Setting1,
    Setting2,
    Setting3,
    Setting4,
    Setting5,

    Setting01,
    Setting02,

    Setting11,
    Setting12,

    Setting21,
    Setting22,


    ActiveGame0,

    BattleInfoUI2,

    SL5,
    SL6,

    ItemFullToBag,
    BagFull,

}

public class GameStringData : Singleton<GameStringData>
{
    [SerializeField]
    List<GameString> data;

    public void load()
    {
        // 场景里序列化的字符串表是早期版本导出的，可能和枚举顺序对不上
        // （例如已删除的云存档条目会让其后所有索引整体偏移）。
        // 这里以代码里的枚举顺序为准重建，并从旧表里复用同文的繁体译文，避免繁体模式下变回简体。
        List<GameString> old = data;

        data = new List<GameString>();

        addString( "新章初始" );
        addString( "前历再续" );
        addString( "战阵重现" );
//        addString( "回返太虚" );
        addString( "游戏设定" );

        addString( "武器店" );
        addString( "饰品店" );
        addString( "杂货铺" );

        addString( "行囊" );

        addString( "囊内乾坤" );
        addString( "诸物炼化" );
        addString( "体态观览" );
        addString( "五魂化蕴" );
        addString( "今况记入" );
        addString( "前历再续" );
        addString( "空明流转" );

        addString( "确定要出发吗？" );
        addString( "确定要返回标题界面吗？" );

        addString( "给与" );
        addString( "装备" );
        addString( "卸下" );
        addString( "丢弃" );

        addString( "选择物品" );
        addString( "结果预测" );

        addString( "确定要保存调整？" );

        addString( "胜利条件" );
        addString( "失败条件" );

        addString( "是否重新开始本关卡游戏？" );
        addString( "是，继承等级金钱，但无法获得当前关卡熟练度。" );
        addString( "否，返回标题界面。" );
        addString( "感谢您购买本游戏。图片等素材归属原汉堂国际，代码版权归属狐兔网络，其他游戏正在重置中敬请期待。前几名玩家截图以下随机码发送至游戏群管理员可兑换奖励！" );

        addString( "熟练度：" );
        addString( "简单" );
        addString( "普通" );
        addString( "困难" );
        addString( "回合数：" );

        addString( "游戏时间：" );

        addString( "用户协议\n用户使用本游戏软件默认同意以下协议：\n\n1.用户不得以任何形式将本游戏软件公开下载。\n2.游戏软件测试期间使用云存档系统无本地数据，游戏软件开发商将妥善保存用户任何数据信息并可以任意使用用户云存档等数据。请等待存档上传完毕，以免数据丢失，测试结束后存档将被清空，本游戏软件将无法继续使用。\n3.本游戏软件将默认上传游戏内错误信息，如有必要请将底部本机ID截图发送游戏测试群。\n4.测试期间可能更新比较频繁，目前需要整体全部下载，暂无差量下载更新。感谢您的理解与支持！\n5.测试结束后我们将给予测试优异者丰厚奖励。\n" );


        addString( "基础说明" );
        addString( "道具一览" );
        addString( "技能一览" );
        addString( "攻略指引" );
        addString( "五魂化蕴" );
        addString( "QQ讨论群" );
        addString( "帮助" );
        addString( "关闭" );

        addString( "5星好评，获得额外金钱并进入GoodEnding熟练度减少2点！" );
        addString( "已评价" );
        addString( "" );
        addString( "" );
        addString( "" );

        addString( "难度设定" );
        addString( "语言设定" );
        addString( "修改器" );
        addString( "控制器大小" );
        addString( "" );
        addString( "" );

        addString( "新版正常模式" );
        addString( "原版加强模式" );

        addString( "简体" );
        addString( "繁體" );

        addString( "缩小" );
        addString( "放大" );

        addString( "点击解锁游戏！\n（未解锁无法存取进度）" );

        addString( "熟练度" );

        addString( "今况记入" );
        addString( "前历再续" );

        addString( "0物品已满，物品已放入行囊" );
        addString( "行囊已满，无法拾取物品" );


        //         addString( File.ReadAllText( Application.dataPath + "/Objects/Help/Help00.txt" , Encoding.UTF8 ) );
        //         addString( File.ReadAllText( Application.dataPath + "/Objects/Help/Help10.txt" , Encoding.UTF8 ) );
        //         addString( File.ReadAllText( Application.dataPath + "/Objects/Help/Help20.txt" , Encoding.UTF8 ) );
        //         addString( File.ReadAllText( Application.dataPath + "/Objects/Help/Help30.txt" , Encoding.UTF8 ) );
        //         addString( File.ReadAllText( Application.dataPath + "/Objects/Help/Help40.txt" , Encoding.UTF8 ) );

        reuseTraditional( old );
    }

    void addString( string str )
    {
        GameString gs = new GameString();
        gs.init( str );

        data.Add( gs );
    }

    /// <summary>把旧表里同一条文案的繁体译文搬到新表</summary>
    void reuseTraditional( List<GameString> old )
    {
        if ( old == null || old.Count == 0 )
        {
            return;
        }

        for ( int i = 0 ; i < data.Count ; i++ )
        {
            for ( int j = 0 ; j < old.Count ; j++ )
            {
                if ( old[ j ].Simplified == data[ i ].Simplified )
                {
                    string t = old[ j ].Traditional;

                    if ( !string.IsNullOrEmpty( t ) && t != data[ i ].Simplified )
                    {
                        data[ i ].Traditional = t;
                    }

                    break;
                }
            }
        }
    }

    bool loaded = false;

    public string getString( GameStringType t )
    {
        // 第一次取值时按枚举顺序重建字符串表，保证下标和枚举严格对应
        // （场景里的序列化数据可能滞后，会导致存/读档标题等文案整体错位）
        if ( !loaded )
        {
            loaded = true;

            load();
        }

        string str = data[ (int)t ].String;

        return str;
    }
}
