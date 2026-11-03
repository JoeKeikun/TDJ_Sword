using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameMsgBoxChooseUI : GameUI<GameMsgBoxChooseUI>
{
    Text text;
    Text okText;
    Text cancelText;

    GameAnimation okGameAnimation;
    GameAnimation cancelGameAnimation;

    bool bOK = false;

    public bool IsOK { get { return bOK; } }

    OnEventOver onEventOver;

    Color color;

    public void onClick()
    {
        unShowFade();

        if ( onEventOver != null )
        {
            onEventOver();
        }
    }

    public void onCancelClick()
    {
        bOK = false;

        unShowFade();

        if ( onEventOver != null )
        {
            onEventOver();
        }
    }

    public override void initSingleton()
    {
        text = transform.Find( "text" ).GetComponent<Text>();
        okText = transform.Find( "okText" ).GetComponent<Text>();
        cancelText = transform.Find( "cancelText" ).GetComponent<Text>();

        color = text.color;

        okGameAnimation = transform.Find( "ok" ).GetComponent<GameAnimation>();
        cancelGameAnimation = transform.Find( "cancel" ).GetComponent<GameAnimation>();
    }

    void clear()
    {
        text.text = "";
        okText.text = "";
        cancelText.text = "";

        okGameAnimation.stopAnimation();
        okGameAnimation.clearAnimation();

        cancelGameAnimation.stopAnimation();
        cancelGameAnimation.clearAnimation();
    }
    
    public void select( bool b )
    {
        bOK = b;

        updateAnimations();
    }

    public void updateAnimations()
    {
        if ( bOK )
        {
            okGameAnimation.playAnimation( 1 , 12 );

            cancelGameAnimation.stopAnimation();
            cancelGameAnimation.showFrame( 12 );

            color.a = 1.0f;
            okText.color = color;
            color.a = 0.2f;
            cancelText.color = color;
        }
        else
        {
            okGameAnimation.stopAnimation();
            okGameAnimation.showFrame( 1 );

            cancelGameAnimation.playAnimation( 12 , 23 );

            color.a = 1.0f;
            cancelText.color = color;
            color.a = 0.2f;
            okText.color = color;
        }
    }


    public void showText( string str , string os , string cs , bool b , OnEventOver over )
    {
        show();

        text.text = str;
        okText.text = os;
        cancelText.text = cs;

        select( b );
        
        onEventOver = over;
    }

    protected override void onUIFadeIn()
    {
        base.onUIFadeIn();

        if ( isFadeIn )
        {
            return;
        }

        // 淡入结束会把文字颜色恢复成淡入开始时缓存的不透明颜色，
        // 覆盖掉 select() 里给未选中项设的 0.2 透明度，
        // 导致图标是未选中、文字却还是选中态。这里重新应用一次。
        updateAnimations();
    }

    public override void onUnShow()
    {
        clear();
    }

}


