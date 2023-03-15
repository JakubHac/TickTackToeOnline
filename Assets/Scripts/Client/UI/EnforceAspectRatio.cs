using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnforceAspectRatio : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    
    
    private int lastWidth = 0;
    private int lastHeight = 0;
    
    void Update()
    {
        if (Initializer.isServer || !Application.isFocused || Screen.fullScreen) return;
        
        //check if mouse is outside the window
        if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
        {
            return;
        }

        var width = Screen.width; var height = Screen.height;
 
        if (lastWidth != width) // if the user is changing the width
        {
            // update the height
            var heightAccordingToWidth = width / 4.0 * 3.0;
            Screen.SetResolution(width, (int)Mathf.Round((float)heightAccordingToWidth), false, 0);
        }
        else if (lastHeight != height) // if the user is changing the height
        {
            // update the width
            var widthAccordingToHeight = height / 3.0 * 4.0;
            Screen.SetResolution((int)Mathf.Round((float)widthAccordingToHeight), height, false, 0);
        }
        lastWidth = width;
        lastHeight = height;
    
        
        // if (!Screen.fullScreen)
        // {
        //     int width = Screen.width;
        //     int desiredHeight = width / 4 * 3;
        //     if (desiredHeight != Screen.height)
        //     {
        //         Screen.SetResolution(width, desiredHeight, false);
        //     }
        // }
    }
}
