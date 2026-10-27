using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 默认材质。真机构建里 Graphic.defaultGraphicMaterial 可能 shader 被裁剪，
/// 材质对象还在但 shader 为空，Image 会画成白块，所以这里做一次有效性校验。
/// </summary>
public static class GameMaterial
{
    static Material cache = null;

    public static Material ui
    {
        get
        {
            if ( cache != null && cache.shader != null )
            {
                return cache;
            }

            Material def = Graphic.defaultGraphicMaterial;

            if ( def != null && def.shader != null )
            {
                cache = def;

                return cache;
            }

            Shader shader = Shader.Find( "UI/Default" );

            if ( shader == null )
            {
                shader = Shader.Find( "Sprites/Default" );
            }

            if ( shader != null )
            {
                cache = new Material( shader );
            }
            else
            {
                cache = def;
            }

            return cache;
        }
    }
}
