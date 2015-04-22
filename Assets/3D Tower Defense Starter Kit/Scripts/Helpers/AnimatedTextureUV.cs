//This script was posted at the Unify Wiki
//http://www.unifycommunity.com/wiki/index.php?title=Animating_Tiled_texture
//Original Author: Joachim Ante
//I only rewrote this to C# and edited the comments.

using UnityEngine;
using System.Collections;

//animate spritesheet with custom uvs
public class AnimatedTextureUV : MonoBehaviour
{
    //number of colums of your sheet
    public int uvAnimationTileX = 24;
    //rows of your sheet
    public int uvAnimationTileY = 1;
    //how fast the spritesheet should animate
    public float framesPerSecond = 10.0f;

    void Update () 
    {
        //calculate index based on time
        int index = (int)(Time.time * framesPerSecond);
        //repeat when exhausting all frames
        index = index % (uvAnimationTileX * uvAnimationTileY);
   
        //get size of every tile
        Vector2 size = new Vector2 (1.0f / uvAnimationTileX, 1.0f / uvAnimationTileY);
   
        //split into horizontal and vertical index
        int uIndex = index % uvAnimationTileX;
        int vIndex = index / uvAnimationTileX;

        //build offset
        //v coordinate is the bottom of the image in opengl so we need to invert.
        Vector2 offset = new Vector2 (uIndex * size.x, 1.0f - size.y - vIndex * size.y);
   
        //flips through all frames by setting the texture offset
        renderer.material.SetTextureOffset ("_MainTex", offset);
        //sets the placement scale of this texture
        renderer.material.SetTextureScale ("_MainTex", size);
    }
}

