﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum GameSLType
{
    Save = 0,
    Load,

    Count
}

public class GameSLUI : GameUI<GameSLUI>
{
    // 场景里只有 item0~item4 五个槽位控件，
    // 存档槽扩到与关卡数一致后，这五个控件当成一页窗口翻页显示
    public const int PAGE_SIZE = 5;

    GameSLUISlot[] slot = new GameSLUISlot[ PAGE_SIZE ];

    GameSLType slType;

    Text text;
    Text loading;

    string title = "";

    int selection = 0;
    int page = 0;
    GameAskUI askUI;

    public int Selection { get { return selection; } }
    public GameSLType Type { get { return slType; } }

    public int PageCount { get { return ( GameDefine.MAX_SAVE + PAGE_SIZE - 1 ) / PAGE_SIZE; } }


    public override void initSingleton()
    {
        for ( int i = 0 ; i < PAGE_SIZE ; i++ )
        {
            slot[ i ] = transform.Find( "item" + i ).GetComponent<GameSLUISlot>();
        }

        text = transform.Find( "text" ).GetComponent<Text>();
        loading = transform.Find( "loading" ).GetComponent<Text>();

        askUI = GetComponentInChildren<GameAskUI>();
    }

    void onLoadGame()
    {
        GameUserData.instance.load( selection );
    }

    public void onClick()
    {
        if ( IsShowAskUI )
        {
            if ( IsOKAskUI )
            {
                switch ( slType )
                {
                    case GameSLType.Save:
                        {
                            GameUserData.instance.save( selection );

                            updateData();
                        }
                        break;
                    case GameSLType.Load:
                        {
                            GameBlackUI.instance.showBlack( 0.5f , onLoadGame );
                        }
                        break;
                }
            }
            else
            {
            }

            unShowAskUI();
        }
        else
        {
            switch ( slType )
            {
                case GameSLType.Save:
                        showAskUI( true );
                    break;
                case GameSLType.Load:
                    {
                        if ( GameUserData.instance.SaveInfo[ selection ].Stage != 0 )
                        {
                            showAskUI( true );
                        }
                    }
                    break;
            }

        }
    }

    public void select( int n )
    {
        selection = n;

        if ( selection < 0 )
        {
            selection = 0;
        }

        if ( selection >= GameDefine.MAX_SAVE )
        {
            selection = GameDefine.MAX_SAVE - 1;
        }

        int newPage = selection / PAGE_SIZE;

        for ( int i = 0 ; i < slot.Length ; i++ )
        {
            slot[ i ].clear();
        }

        // 翻页时重新绑定这一页窗口显示的存档
        if ( newPage != page )
        {
            page = newPage;

            updateData();
        }

        int row = selection - page * PAGE_SIZE;

        if ( row >= 0 && row < slot.Length )
        {
            slot[ row ].select();
        }
    }

    public void updateData()
    {
        for ( int i = 0 ; i < slot.Length ; i++ )
        {
            int saveIndex = page * PAGE_SIZE + i;

            // 保险：saveInfo 是场景序列化字段，长度可能滞后于 MAX_SAVE
            if ( saveIndex < GameDefine.MAX_SAVE && saveIndex < GameUserData.instance.SaveInfo.Length )
            {
                slot[ i ].gameObject.SetActive( true );
                slot[ i ].setData( GameUserData.instance.SaveInfo[ saveIndex ] , saveIndex );
            }
            else
            {
                slot[ i ].gameObject.SetActive( false );
            }
        }

        updateTitle();
    }

    void updateTitle()
    {
        text.text = title + "  " + ( page + 1 ) + "/" + PageCount;
    }

    public void clear()
    {
        for ( int i = 0 ; i < slot.Length ; i++ )
        {
            slot[ i ].clear();
        }
    }

    public void show( GameSLType t )
    {
        slType = t;

        show();

        title = GameStringData.instance.getString( GameStringType.SL5 + (int)t );
        loading.text = "";

        page = 0;
        selection = 0;

        updateData();
    }

    protected override void onUIFadeIn()
    {
        base.onUIFadeIn();

        select( 0 );
    }

    public void showAskUI( bool b )
    {
        askUI.show( b );

        int row = selection - page * PAGE_SIZE;

        if ( row < 0 )
        {
            row = 0;
        }

        if ( row >= slot.Length )
        {
            row = slot.Length - 1;
        }

        RectTransform trans = slot[ row ].GetComponent<RectTransform>();
        askUI.setPos( trans.anchoredPosition.x + 80 , trans.anchoredPosition.y + 11 );
    }

    public void unShowAskUI()
    {
        askUI.unShow();
    }

    public bool IsShowAskUI { get { return askUI.IsShow; } }
    public bool IsOKAskUI { get { return askUI.IsOK; } }
}


