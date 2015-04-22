/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;

//our about/help/support editor window
public class AboutTDEditor : EditorWindow
{
    [MenuItem("Window/TD Starter Kit/About")]
    static void Init()
    {
        AboutTDEditor aboutWindow = (AboutTDEditor)EditorWindow.GetWindowWithRect
                (typeof(AboutTDEditor), new Rect(0, 0, 300, 300), false, "About");
        aboutWindow.Show();
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect((Screen.width / 2) - 90, 20, 250, 100));
        GUILayout.Label("3D Tower Defense Starter Kit");
        GUILayout.Label("by Rebound Games GbR");
        GUILayout.EndArea();
        GUILayout.Space(70);


        GUILayout.Label("Info", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Homepage");
        if (GUILayout.Button("Visit", GUILayout.Width(100)))
        {
            Help.BrowseURL("www.rebound-games.com");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("YouTube");
        if (GUILayout.Button("Visit", GUILayout.Width(100)))
        {
            Help.BrowseURL("https://www.youtube.com/user/ReboundGamesTV");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Twitter");
        if (GUILayout.Button("Visit", GUILayout.Width(100)))
        {
            Help.BrowseURL("https://twitter.com/Rebound_G");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("Support", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Support Forum");
        if (GUILayout.Button("Visit", GUILayout.Width(100)))
        {
            Help.BrowseURL("http://www.rebound-games.com/forum/");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Unity Forum");
        if (GUILayout.Button("Visit", GUILayout.Width(100)))
        {
            Help.BrowseURL("http://forum.unity3d.com/threads/130124-3D-Tower-Defense-Starter-Kit");
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        GUILayout.Label("Support us!", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Rate/Review");
        if (GUILayout.Button("Visit", GUILayout.Width(100)))
        {
            UnityEditorInternal.AssetStore.Open("content/3933");
        }
        GUILayout.EndHorizontal();
    }
}