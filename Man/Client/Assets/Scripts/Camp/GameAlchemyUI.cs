using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameAlchemyUI : GameUI<GameAlchemyUI>
{
    OnEventOver onEventOver;

    GameAlchemyUIBag bagUI0;
    GameAlchemyUIBag bagUI1;

    Text itemText;

    GameObject forgeMessage;
    Text forgeMessageText;
    float forgeMessageTime = 0.0f;

    public override void initSingleton()
    {
        bagUI0 = transform.Find( "bag0" ).GetComponent<GameAlchemyUIBag>();
        bagUI1 = transform.Find( "bag1" ).GetComponent<GameAlchemyUIBag>();

        itemText = transform.Find( "item" ).GetComponent<Text>();
    }

    void clear()
    {
        forgeMessageTime = 0.0f;

        if ( forgeMessage != null )
        {
            forgeMessage.SetActive( false );
        }
    }

    protected override void onUpdate()
    {
        if ( forgeMessageTime <= 0.0f )
        {
            return;
        }

        forgeMessageTime -= Time.deltaTime;

        if ( forgeMessageTime <= 0.0f && forgeMessage != null )
        {
            forgeMessage.SetActive( false );
        }
    }

    public void updateText()
    {
        if ( bagUI0.Enabled )
        {
            itemText.text = bagUI0.getItemDes();
        }
        else
        {
            itemText.text = bagUI1.getItemDes();
        }
    }

    public override void onUnShow()
    {
        clear();

        if ( onEventOver != null )
        {
            onEventOver();
        }
    }

    
    public void select( int n )
    {
        if ( bagUI0.Enabled )
        {
            bagUI0.select( n );
        }
        else
        {
            bagUI1.select( n );
        }

        updateText();
    }


    public bool isShowAskUI()
    {
        return bagUI1.IsShowAskUI;
    }

    public void onClickAskUI()
    {
        unShowAskUI();

        if ( bagUI1.IsOKAskUI )
        {
            GameItem item1 = bagUI0.getItem();
            GameItem item2 = bagUI1.getItem();

            short id = GameUserData.instance.getAlchemyItem( item1.ID , item2.ID );

            GameAlchemyUIBag.Item item = null;

            if ( bagUI0.Selection > bagUI1.Selection )
            {
                item = bagUI0.removeItem();
                bagUI1.removeItem();
            }
            else
            {
                bagUI1.removeItem();
                item = bagUI0.removeItem();
            }

            if ( item.type == GameAlchemyUIType.User )
            {
                GameUnitBase unit = GameUserData.instance.getUnitBase( item.userID );
                unit.addItem( id );
            }
            else
            {
                GameUserData.instance.addItem( id );
            }

            bagUI0.addItems();
            bagUI1.addItems();

            bagUI0.select( 0 );

            showAlchemy( false );

            playForgeMovie( id );
        }
        else
        {

        }
    }
    
    public void unShowAskUI()
    {
        bagUI1.unShowAskUI();
    }

    public void showAskUI( bool b )
    {
        bagUI1.showAskUI( b );
    }

    public bool isOKAskUI()
    {
        return bagUI1.IsOKAskUI;
    }

    public void onClick()
    {
        if ( bagUI0.Enabled )
        {
            if ( bagUI0.getItemEnabled() )
            {
                showAlchemy( true );
            }
        }
    }

    public void onClickAlchemy()
    {
        if ( bagUI1.Enabled )
        {
            bagUI1.showAskUI( true );
        }
    }

    public int getSelection()
    {
        if ( bagUI0.Enabled )
        {
            return bagUI0.Selection;
        }
        else
        {
            return bagUI1.Selection;
        }
    }

    public bool isEnabledAlchemy()
    {
        return bagUI1.Enabled;
    }

    public void showAlchemy( bool b )
    {
        if ( b )
        {
            bagUI1.enable( true );
            bagUI0.enable( false );

            bagUI1.setAlchemySelection( bagUI0.Selection );
            bagUI1.select( 0 );

            bagUI1.gameObject.SetActive( true );
        }
        else
        {
            bagUI1.enable( false );
            bagUI0.enable( true );

            bagUI1.gameObject.SetActive( false );
        }
    }

    public void show( OnEventOver over )
    {
        onEventOver = over;

        show();

        bagUI0.addItems();
        bagUI0.enable( true );
        bagUI0.select( 0 );

        bagUI1.addItems();
        bagUI1.enable( false );
        bagUI1.gameObject.SetActive( false );

        updateText();
    }

    // 炼化成功后播放原作的炼化过场（Movie/Forgeite），过场结束后提示获得的物品
    void playForgeMovie( short id )
    {
        GameItem item = GameItemData.instance.getData( id );

        string name = item != null ? item.Name : "";

        if ( GameMovieManager.instance != null )
        {
            GameMovieManager.instance.playMovie( "Movie/Forgeite" , delegate { showForgeMessage( name ); } );
        }
        else
        {
            showForgeMessage( name );
        }
    }

    void showForgeMessage( string name )
    {
        createForgeMessage();

        forgeMessageText.text = getForgeMessage( name );

        forgeMessage.SetActive( true );
        forgeMessage.transform.SetAsLastSibling();

        forgeMessageTime = 3.0f;
    }

    string getForgeMessage( string name )
    {
        if ( GameMessageData.instance != null )
        {
            GameMessage msg = GameMessageData.instance.getData( GameMessageType.Get1 );

            if ( msg != null && msg.message != null && msg.message.Length > 0 )
            {
                string str = msg.message[ 0 ][ 1 ];

                if ( !string.IsNullOrEmpty( str ) )
                {
                    return str.Replace( "0" , name );
                }
            }
        }

        return "获得 " + name;
    }

    // 提示框是运行时拼的（不依赖场景里没有的预制体），样式沿用营地里其它提示的深底金字
    void createForgeMessage()
    {
        if ( forgeMessage != null )
        {
            return;
        }

        forgeMessage = new GameObject( "forgeMessage" , typeof( RectTransform ) );

        forgeMessage.transform.SetParent( transform , false );

        RectTransform rt = forgeMessage.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2( 0.5f , 0.5f );
        rt.anchorMax = new Vector2( 0.5f , 0.5f );
        rt.pivot = new Vector2( 0.5f , 0.5f );
        rt.anchoredPosition = new Vector2( 0.0f , 20.0f );
        rt.sizeDelta = new Vector2( 300.0f , 56.0f );

        Image background = forgeMessage.AddComponent<Image>();

        background.material = GameMaterial.ui;
        background.color = new Color( 0.07f , 0.06f , 0.04f , 0.88f );
        background.raycastTarget = false;

        GameObject textObj = new GameObject( "text" , typeof( RectTransform ) );

        textObj.transform.SetParent( forgeMessage.transform , false );

        RectTransform trans = textObj.GetComponent<RectTransform>();

        trans.anchorMin = Vector2.zero;
        trans.anchorMax = Vector2.one;
        trans.offsetMin = new Vector2( 10.0f , 6.0f );
        trans.offsetMax = new Vector2( -10.0f , -6.0f );

        forgeMessageText = textObj.AddComponent<Text>();

        forgeMessageText.font = Resources.Load<Font>( "Fonts/GameFont" );
        forgeMessageText.fontSize = 22;
        forgeMessageText.alignment = TextAnchor.MiddleCenter;
        forgeMessageText.color = new Color( 1.0f , 0.9f , 0.65f , 1.0f );
        forgeMessageText.raycastTarget = false;

        Outline outline = textObj.AddComponent<Outline>();

        outline.effectColor = new Color( 0.0f , 0.0f , 0.0f , 0.85f );
        outline.effectDistance = new Vector2( 1.0f , -1.0f );
        outline.useGraphicAlpha = true;

        forgeMessage.SetActive( false );
    }


}


