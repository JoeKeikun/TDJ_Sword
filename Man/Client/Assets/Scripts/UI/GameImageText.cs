﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameImageText : Text
{
    [SerializeField]
    UIVertex uiVertex;

    GameAnimation gameAnimation;

    float speed = 0.0f;
    bool start = false;
    string text1 = "";
    int showIndex = 0;
    bool isOver = false;
    float time = 0.0f;

    bool isStopLine = false;

    public bool IsStopLine { get { return isStopLine; } }
    public bool IsOver { get { return isOver; } }

    protected override void Awake()
    {
        base.Awake();

        gameAnimation = GetComponentInChildren<GameAnimation>();
    }

    public void clear()
    {
        text = "";

        gameAnimation.stopAnimation();
        gameAnimation.clearAnimation();
    }

    public void showText( string str )
    {
        text = "";
        isOver = false;
        start = true;
        text1 = str;
        speed = 0.1f;
        time = 0.0f;
        showIndex = 0;
    }

    protected override void OnPopulateMesh( VertexHelper toFill )
    {
        base.OnPopulateMesh( toFill );

        if ( toFill.currentVertCount == 0 )
            return;

        toFill.PopulateUIVertex( ref uiVertex , toFill.currentVertCount - 1 );
    }

    void updateText()
    {
        showIndex++;

        if ( showIndex > text1.Length )
        {
            text = text1;

            finishText();

            return;
        }

        string current = text1.Substring( 0 , showIndex );

        int fit;

        // 按实际排版高度判断当前页是否写满，不能只数换行符：
        // 长对白会自动折行，超出文本框的行会被 Truncate 截掉。
        if ( isOverflow( current , out fit ) )
        {
            string page = text1.Substring( 0 , fit );
            string rest = text1.Substring( fit );

            // 翻页后开头的换行符会显示成空行，去掉
            while ( rest.Length > 0 && rest[ 0 ] == '\n' )
            {
                rest = rest.Remove( 0 , 1 );
            }

            // 只显示能完整放进文本框的部分，剩余文字等玩家点击后翻页继续
            text = page;

            if ( rest.Length == 0 )
            {
                finishText();

                return;
            }

            isStopLine = true;
            speed = 0.1f;

            text1 = rest;
            showIndex = 0;

            gameAnimation.offsetX = (int)( ( uiVertex.position.x + 20 ) / 2.0f );
            gameAnimation.offsetY = (int)( ( uiVertex.position.y + 20 ) / 2.0f );
            gameAnimation.playAnimation( 27 , 41 );

            return;
        }

        text = current;
    }

    void finishText()
    {
        start = false;
        isOver = true;

        gameAnimation.offsetX = (int)( ( uiVertex.position.x + 20 ) / 2.0f );
        gameAnimation.offsetY = (int)( ( uiVertex.position.y + 20 ) / 2.0f );
        gameAnimation.playAnimation( 27 , 41 );
    }

    /// <summary>判断文字是否超出文本框；超出时 fit 返回能完整显示的最大字符数</summary>
    bool isOverflow( string str , out int fit )
    {
        fit = str.Length;

        float visible = rectTransform.rect.height;

        if ( getPreferredHeight( str ) <= visible + 0.01f )
        {
            return false;
        }

        int lo = 1;
        int hi = str.Length - 1;
        int best = 0;

        while ( lo <= hi )
        {
            int mid = ( lo + hi ) / 2;

            if ( getPreferredHeight( str.Substring( 0 , mid ) ) <= visible + 0.01f )
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        fit = best > 0 ? best : 1;

        return true;
    }

    /// <summary>按当前文本框宽度换行后，文字需要的总高度</summary>
    float getPreferredHeight( string str )
    {
        TextGenerationSettings settings = GetGenerationSettings( rectTransform.rect.size );

        return cachedTextGeneratorForLayout.GetPreferredHeight( str , settings ) / pixelsPerUnit;
    }

    public void onClick()
    {
        if ( isStopLine )
        {
            isStopLine = false;
            gameAnimation.stopAnimation();
            gameAnimation.clearAnimation();
            return;
        }

        speed = 0.0f;
    }

    void Update()
    {
        if ( !start ||
            isStopLine )
        {
            return;
        }

        time += Time.deltaTime;

        if ( time < speed )
        {
            return;
        }

        time = 0.0f;

        updateText();
    }
}
