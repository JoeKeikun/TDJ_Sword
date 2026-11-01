﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;


public class GameSceneTitle : Singleton<GameSceneTitle>
{
    public override void initSingleton()
    {

    }

    private void Start()
    {
#if UNITY_IOS && !IOS_PERSONAL_DEV
        // 官方iOS版本因兼容性跳过开场视频
        onTitleMovieOver();
#elif UNITY_IOS && IOS_PERSONAL_DEV
        // 个人开发版：尝试播放视频，失败则自动跳过
        StartCoroutine(tryPlayTitleMovie());
#else
        GameMovieManager.instance.playMovieCenter( "Movie/Title" , onTitleMovieOver );
#endif
    }

#if UNITY_IOS && IOS_PERSONAL_DEV
    IEnumerator tryPlayTitleMovie()
    {
        // 等待一帧确保VideoPlayer初始化完成
        yield return null;

        VideoClip clip = Resources.Load<VideoClip>( "Movie/Title" );
        if ( clip == null )
        {
            Debug.LogWarning( "[iOS] Title movie not found, skipping." );
            onTitleMovieOver();
            yield break;
        }

        try
        {
            GameMovieManager.instance.playMovieCenter( "Movie/Title" , onTitleMovieOver );
        }
        catch ( System.Exception ex )
        {
            Debug.LogWarning( "[iOS] Failed to play title movie: " + ex.Message );
            onTitleMovieOver();
        }
    }
#endif

    void onTitleMovieOver()
    {
        GameTitleUI.instance.show();
        GameTitleUI.instance.select( 0 );
        GameTitleUI.instance.showFade();

        GameTouchCenterUI.instance.showUI();
    }
}

