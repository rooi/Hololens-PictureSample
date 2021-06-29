using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using UnityEngine.Windows.WebCam;


public class PhotoCamera : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;
  
    public GameObject PhotoPrefab;
    
    Resolution cameraResolution;
    
    float ratio = 1.0f;
    AudioSource shutterSound;

    // Use this for initialization
    void Start()
    {
        shutterSound = GetComponent<AudioSource>() as AudioSource;
        Debug.Log("File path " + Application.persistentDataPath);
        cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        
        ratio = (float)cameraResolution.height / (float)cameraResolution.width;

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            Debug.Log("camera ready to take picture");
        });
        
    }
    public void StopCamera()
    {
        // Deactivate our camera
        
        photoCaptureObject?.StopPhotoModeAsync(OnStoppedPhotoMode);
    }
    public void TakePicture()
    {
        CameraParameters cameraParameters = new CameraParameters();
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.cameraResolutionWidth = cameraResolution.width;
        cameraParameters.cameraResolutionHeight = cameraResolution.height;
        cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

        // Activate the camera
        if (photoCaptureObject != null)
        {
            if (shutterSound != null)
            {
                shutterSound.Play();
            }
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into our target texture
        var targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        // Create a gameobject that we can apply our texture to
        
        GameObject newElement = Instantiate<GameObject>(PhotoPrefab);
        GameObject quad = newElement.transform.Find("Quad").gameObject;
        Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material.mainTexture = targetTexture;
        quadRenderer.material = new Material(Shader.Find("AR/HolographicImageBlend"));
        // new Material(Shader.Find("Unlit/Texture"));

        // Set position and rotation 
        // Bug in Hololens v2 and Unity 2019 about PhotoCaptureFrame not having the location data - March 2020
        // 
        if (photoCaptureFrame.hasLocationData)
        {
            Matrix4x4 cameraToWorldMatrix;
            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

            Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

            Matrix4x4 projectionMatrix;
            photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);
            //photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);

            quadRenderer.sharedMaterial.SetTexture("_MainTex", targetTexture);
            quadRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
            quadRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
            quadRenderer.sharedMaterial.SetFloat("_VignetteScale", 1.0f);

            quad.transform.position = position;
            quad.transform.rotation = rotation;
        }


    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown our photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}