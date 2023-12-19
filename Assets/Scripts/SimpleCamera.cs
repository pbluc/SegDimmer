using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;

public class SimpleCamera : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI _debugText;
    [SerializeField]
    private GameObject _target;
    [SerializeField]
    private GameObject _plane;
    [SerializeField, Tooltip("Desired width for the camera capture")]
    private int _captureWidth = 1280;
    [SerializeField, Tooltip("Desired height for the camera capture")]
    private int _captureHeight = 720;

    private Camera _mainCamera;

    private MLCamera _camera;
    private MLCamera.CaptureConfig _captureConfig;
    //The identifier can either target the Main or CV cameras.
    private MLCamera.Identifier _identifier = MLCamera.Identifier.Main;
    private MLCamera.ResultExtras _resultExtras;

    //Is true if the camera is ready to be connected.
    private bool _cameraDeviceAvailable;

    private Renderer _planeRenderer;
    private Texture2D _videoTextureRgb;

    private float _waitTime = 2.0f;
    private float _timer = 0.0f;

    //The camera capture state
    bool _isCapturing;

    public const int PIXEL_PROCESSING_STRIDE_X = 1;
    public const int PIXEL_PROCESSING_STRIDE_Y = 1;



    void OnEnable()
    {
        //_debugText.text += String.Format("  Debug logs start\n");
        _planeRenderer = _plane.GetComponent<Renderer>();

        _mainCamera = Camera.main;
        //_debugText.text += String.Format("cam.pixelWidth = {0}, cam.pixelHeight = {1}\n", cam.pixelWidth, cam.pixelHeight);

        //This script assumes that camera permissions were already granted.
        StartCoroutine(EnableMLCamera());
    }

    void OnDisable()
    {
        StopCapture();
    }

    private void Update()
    {
        if (_isCapturing)
        {
            _timer += Time.deltaTime;

            // Check if we have reached beyond 2 seconds.
            // Subtracting two is more accurate over time than resetting to zero.
            if (_timer > _waitTime)
            {
                if (InFrontOfCamera())
                {
                    Rect targetBoundingBox = GetBoundingBox();
                    ProcessPixels(targetBoundingBox);
                }

                // Remove the recorded 2 seconds.
                _timer = _timer - _waitTime;
            }
        }
    }

    private void ProcessPixels(Rect boundingBox)
    {
        Debug.LogFormat("SimpleCamera.cs is _videoTextureRgb null: {0}", _videoTextureRgb == null);
        //_debugText.text += String.Format("SimpleCamera.cs is _videoTextureRgb null: {0}\n", _videoTextureRgb == null);

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
        _debugText.text += String.Format("Average luminance in bounding box: {0}\n", averageLuminance);
    }

    public bool InFrontOfCamera()
    {
        return Vector3.Dot(_mainCamera.transform.forward, _target.transform.position - _mainCamera.transform.position) > 0;
    }

    private Rect GetBoundingBox()
    {
        float scalingValue = 0.25f;

        BoxCollider boxCollider = _target.GetComponent<BoxCollider>();
        _debugText.text += String.Format("  Box collider size: {0}\n", boxCollider.size);

        var extentPoints = new Vector2[] 
        {
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x * scalingValue, -boxCollider.size.y * scalingValue, -boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x * scalingValue, -boxCollider.size.y * scalingValue, -boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x * scalingValue, -boxCollider.size.y * scalingValue, boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x * scalingValue, -boxCollider.size.y * scalingValue, boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x * scalingValue, boxCollider.size.y * scalingValue, -boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x * scalingValue, boxCollider.size.y * scalingValue, -boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x * scalingValue, boxCollider.size.y * scalingValue, boxCollider.size.z * scalingValue) * 0.5f)),
            WorldToScreenPoint(transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x * scalingValue, boxCollider.size.y * scalingValue, boxCollider.size.z * scalingValue) * 0.5f))
        };

        Vector2 lowerLeft = extentPoints[0];
        Vector2 upperRight = extentPoints[0];

        foreach (Vector2 v in extentPoints) {
            lowerLeft = Vector2.Min(lowerLeft, v);
            upperRight = Vector2.Max(upperRight, v);
        }

        float x = lowerLeft.x;
        float y = upperRight.y;
        float width = upperRight.x - lowerLeft.x;
        float height = upperRight.y - lowerLeft.y;

        Debug.LogFormat("Bounding box Rect: ( x = {0}, y = {1}, width = {2}, height {3})", x, y, width, height);
        _debugText.text += String.Format("  Bounding box Rect: ( x = {0}, y = {1}, width = {2}, height {3})\n", x, y, width, height);
        _debugText.text += String.Format("  Texture2D width: {0} and height: {1}\n", _videoTextureRgb.width, _videoTextureRgb.height);

        return new Rect(x, y, width, height);
    }

    private Vector2 WorldToScreenPoint(Vector3 worldPoint)
    {
        Vector3 transformPoint = Vector3.zero;

        MLResult result = MLCVCamera.GetFramePose(_resultExtras.VCamTimestamp, out Matrix4x4 outMatrix);
        if (result.IsOk)
        {
            transformPoint = outMatrix.MultiplyPoint3x4(worldPoint);
            //_debugText.text += String.Format("  World Point After Camera Proj: {0}\n", transformPoint);

            string cameraExtrinsics = "Camera Extrinsics";
            cameraExtrinsics += "Position " + outMatrix.GetPosition(); // (x, y)
            cameraExtrinsics += "Rotation " + outMatrix.rotation; // (r1, r2, r3, 24)
            //_debugText.text += String.Format(cameraExtrinsics + "\n");
        }

        if (_resultExtras.Intrinsics != null)
        {
            float fx = _resultExtras.Intrinsics.Value.FocalLength.x;
            float fy = _resultExtras.Intrinsics.Value.FocalLength.y;
            float px = _resultExtras.Intrinsics.Value.PrincipalPoint.x;
            float py = _resultExtras.Intrinsics.Value.PrincipalPoint.y;

            //_debugText.text += String.Format("  World Point After Camera Proj: {0}\n", transformPoint);
            Vector2 cameraIntPoint = new Vector3
                (
                    Vector3.Dot(new Vector3(fx, 0, px), transformPoint),
                    Vector3.Dot(new Vector3(0, fy, py), transformPoint),
                    Vector3.Dot(new Vector3(0, 0, 1), transformPoint)
                );
            transformPoint = cameraIntPoint;

            string cameraIntrinsics = "Camera Intrinsics";
            cameraIntrinsics += "\n Width " + _resultExtras.Intrinsics.Value.Width;
            cameraIntrinsics += "\n Height " + _resultExtras.Intrinsics.Value.Height;
            cameraIntrinsics += "\n FOV " + _resultExtras.Intrinsics.Value.FOV;
            cameraIntrinsics += "\n FocalLength " + _resultExtras.Intrinsics.Value.FocalLength; // (fx, fy)
            cameraIntrinsics += "\n PrincipalPoint " + _resultExtras.Intrinsics.Value.PrincipalPoint; // (px, py)
            //_debugText.text += String.Format(cameraIntrinsics + "\n");
        }

        //_debugText.text += String.Format("  Screen point: {0}\n", transformPoint);
        return new Vector2(transformPoint.x, transformPoint.y);
    }

    

    //Waits for the camera to be ready and then connects to it.
    private IEnumerator EnableMLCamera()
    {
        //Checks the main camera's availability.
        while (!_cameraDeviceAvailable)
        {
            _debugText.text += String.Format("  Looking for MLCamera\n");
            MLResult result = MLCamera.GetDeviceAvailabilityStatus(_identifier, out _cameraDeviceAvailable);
            if (result.IsOk == false || _cameraDeviceAvailable == false)
            {
                // Wait until camera device is available
                yield return new WaitForSeconds(1.0f);
            }
        }
        ConnectCamera();
    }

    private void ConnectCamera()
    {
        //Once the camera is available, we can connect to it.
        if (_cameraDeviceAvailable)
        {
            MLCamera.ConnectContext connectContext = MLCamera.ConnectContext.Create();
            connectContext.CamId = _identifier;
            //MLCamera.Identifier.Main is the only camera that can access the virtual and mixed reality flags
            connectContext.Flags = MLCamera.ConnectFlag.CamOnly;
            connectContext.EnableVideoStabilization = true;

            _camera = MLCamera.CreateAndConnect(connectContext);
            if (_camera != null)
            {
                _debugText.text += String.Format("  Camera device connected\n");
                ConfigureCameraInput();
                SetCameraCallbacks();
            }
        }
    }

    private void ConfigureCameraInput()
    {
        //Gets the stream capabilities the selected camera. (Supported capture types, formats and resolutions)
        MLCamera.StreamCapability[] streamCapabilities = MLCamera.GetImageStreamCapabilitiesForCamera(_camera, MLCamera.CaptureType.Video);

        if (streamCapabilities.Length == 0)
            return;

        //Set the default capability stream
        MLCamera.StreamCapability defaultCapability = streamCapabilities[0];

        //Try to get the stream that most closely matches the target width and height
        if (MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, _captureWidth, _captureHeight,
                MLCamera.CaptureType.Video, out MLCamera.StreamCapability selectedCapability))
        {
            defaultCapability = selectedCapability;
        }

        //Initialize a new capture config.
        _captureConfig = new MLCamera.CaptureConfig();
        //Set RGBA video as the output
        MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.RGBA_8888;
        //Set the Frame Rate to 30fps
        _captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS;
        //Initialize a camera stream config.
        //The Main Camera can support up to two stream configurations
        _captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
        _captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create(
            defaultCapability, outputFormat
        );
        StartVideoCapture();
    }

    private void StartVideoCapture()
    {
        MLResult result = _camera.PrepareCapture(_captureConfig, out MLCamera.Metadata metaData);
        if (result.IsOk)
        {
            //Trigger auto exposure and auto white balance
            _camera.PreCaptureAEAWB();
            //Starts video capture. This call can also be called asynchronously 
            //Images capture uses the CaptureImage function instead.
            result = _camera.CaptureVideoStart();
            _isCapturing = MLResult.DidNativeCallSucceed(result.Result, nameof(_camera.CaptureVideoStart));
            if (_isCapturing)
            {
                _debugText.text += String.Format("  Video capture started!\n");
            }
            else
            {
                _debugText.text += String.Format("  Could not start camera capture. Result : {0}\n", result);
            }
        }
    }

    private void StopCapture()
    {
        if (_isCapturing)
        {
            _camera.CaptureVideoStop();
        }

        _camera.Disconnect();
        _camera.OnRawVideoFrameAvailable -= RawVideoFrameAvailable;
        _isCapturing = false;
    }

    //Assumes that the capture configure was created with a Video CaptureType
    private void SetCameraCallbacks()
    {
        //Provides frames in either YUV/RGBA format depending on the stream configuration
        _camera.OnRawVideoFrameAvailable += RawVideoFrameAvailable;
    }

    void RawVideoFrameAvailable(MLCamera.CameraOutput output, MLCamera.ResultExtras extras, MLCameraBase.Metadata metadataHandle)
    {
        if (output.Format == MLCamera.OutputFormat.RGBA_8888)
        {
            //Flips the frame vertically so it does not appear upside down.
            MLCamera.FlipFrameVertically(ref output);
            UpdateRGBTexture(ref _videoTextureRgb, output.Planes[0]);
        }

        _resultExtras = extras;
    }

    private void UpdateRGBTexture(ref Texture2D videoTextureRGB, MLCamera.PlaneInfo imagePlane)
    {

        if (videoTextureRGB != null &&
            (videoTextureRGB.width != imagePlane.Width || videoTextureRGB.height != imagePlane.Height))
        {
            Destroy(videoTextureRGB);
            videoTextureRGB = null;
        }

        if (videoTextureRGB == null)
        {
            // Create a new texture that will display the RGB image
            videoTextureRGB = new Texture2D((int)imagePlane.Width, (int)imagePlane.Height, TextureFormat.RGBA32, false);
            videoTextureRGB.filterMode = FilterMode.Bilinear;
        }

        //debugText.text += String.Format("imagePlane.Width = {0}, imagePlane.Height = {1}\n", imagePlane.Width, imagePlane.Height);
        int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);

        // Image width and stride may differ due to padding bytes for memory alignment. Skip over padding bytes when accessing pixel data.
        if (imagePlane.Stride != actualWidth)
        {
            // Create a new array to store the pixel data without padding
            var newTextureChannel = new byte[actualWidth * imagePlane.Height];
            // Loop through each row of the image
            for (int i = 0; i < imagePlane.Height; i++)
            {
                // Copy the pixel data from the original array to the new array, skipping the padding bytes
                Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel, i * actualWidth, actualWidth);
            }
            // Load the new array as the texture data
            videoTextureRGB.LoadRawTextureData(newTextureChannel);
        }
        else // If the stride is equal to the width, no padding bytes are present
        {
            videoTextureRGB.LoadRawTextureData(imagePlane.Data);
        }
        videoTextureRGB.Apply();
        //debugText.text += String.Format("videoTextureRGB.width = {0}, videoTextureRGB.height = {1}\n", videoTextureRGB.width, videoTextureRGB.height);

        // Assign the Plane Texture to the resulting texture
        _planeRenderer.material.mainTexture = videoTextureRGB;
    }

}