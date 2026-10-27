using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI 渲染诊断：延迟两帧后打印所有 Image 的材质/贴图/透明状态。</summary>
public static class UIDiag
{
    public static IEnumerator dump( Transform root , string tag )
    {
        // 等淡入动画跑完，否则读到的是中间帧的 alpha
        for ( int f = 0 ; f < 60 ; f++ )
        {
            yield return null;
        }

        if ( root == null )
        {
            yield break;
        }

        Image[] imgs = root.GetComponentsInChildren<Image>( true );

        Material def = Graphic.defaultGraphicMaterial;

        Diag.write( "[UIDUMP] " + tag + " images=" + imgs.Length
            + " defaultMat=" + ( def == null ? "null" : def.name + "/" + ( def.shader == null ? "noShader" : def.shader.name ) ) );

        StringBuilder sb = new StringBuilder();

        for ( int i = 0 ; i < imgs.Length ; i++ )
        {
            Image img = imgs[ i ];

            if ( img == null )
            {
                continue;
            }

            Sprite sp = img.sprite;
            Material m = img.material;
            Material cr = img.canvasRenderer.GetMaterial();

            sb.Length = 0;
            sb.Append( "[UIDUMP] " ).Append( tag ).Append( ' ' ).Append( path( img.transform , root ) );
            sb.Append( " act=" ).Append( img.gameObject.activeInHierarchy );
            sb.Append( " en=" ).Append( img.enabled );
            sb.Append( " col=" ).Append( img.color.a.ToString( "0.00" ) );
            sb.Append( " rect=" ).Append( img.rectTransform.rect.width.ToString( "0" ) )
              .Append( 'x' ).Append( img.rectTransform.rect.height.ToString( "0" ) );
            sb.Append( " sp=" ).Append( sp == null ? "null" : sp.name );
            sb.Append( " mat=" ).Append( m == null ? "null" : m.name + "/" + ( m.shader == null ? "noShader" : m.shader.name ) );
            sb.Append( " cr=" ).Append( cr == null ? "null" : cr.name + "/" + ( cr.shader == null ? "noShader" : cr.shader.name ) );
            sb.Append( " a=" ).Append( img.canvasRenderer.GetAlpha().ToString( "0.00" ) );

            Diag.write( sb.ToString() );
        }
    }

    static string path( Transform t , Transform root )
    {
        if ( t == root || t == null )
        {
            return root == null ? "?" : root.name;
        }

        return path( t.parent , root ) + "/" + t.name;
    }
}
