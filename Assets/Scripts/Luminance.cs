using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Luminance : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI debugText;

    private Texture2D _videoTextureRgb;

    public const int PIXEL_PROCESSING_STRIDE_X = 1;
    public const int PIXEL_PROCESSING_STRIDE_Y = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Process the brightness values at each pixel in the 2D bounding box
    private void ProcessPixels(Rect boundingBox)
    {
        Debug.LogFormat("SimpleCamera.cs is _videoTextureRgb null: {0}", _videoTextureRgb == null);
        debugText.text += String.Format("SimpleCamera.cs is _videoTextureRgb null: {0}\n", _videoTextureRgb == null);

        int minX = (int)boundingBox.x;
        int maxX = (int)(boundingBox.x + boundingBox.width);
        int minY = (int)boundingBox.y;
        int maxY = (int)(boundingBox.y + boundingBox.height);

        float averageLuminance = 0;
        int numPixels = 0;
        for (int i = minX; i < maxX; i += PIXEL_PROCESSING_STRIDE_X)
        {
            for (int j = minY; j < minY; j += PIXEL_PROCESSING_STRIDE_Y)
            {
                Color pixel = _videoTextureRgb.GetPixel(i, j);
                float luminance = 0.2126f * pixel.r +
                                  0.7152f * pixel.g +
                                  0.0722f * pixel.b;

                averageLuminance += luminance;
                ++numPixels;
            }
        }
        averageLuminance /= numPixels;
        Debug.LogFormat("Average luminance in bounding box: {0}", averageLuminance);
        debugText.text += String.Format("Average luminance in bounding box: {0}\n", averageLuminance);
    }
}
