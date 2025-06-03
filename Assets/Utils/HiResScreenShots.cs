using System.Collections;
using UnityEngine;
using UnityEngine.U2D;

public class HiResScreenShots : MonoBehaviour
{
    public int resWidth = 1920;
    public int resHeight = 1080;

    private bool takeHiResShot = false;

    public static string ScreenShotName(int width, int height)
    {
        return string.Format(
            "{0}/screenshots/screen_{1}.png",
            Application.dataPath,
            System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
        );
    }

    public void TakeHiResShot()
    {
        takeHiResShot = true;
    }

    void LateUpdate()
    {
        takeHiResShot |= Input.GetKeyDown("k");
        if (takeHiResShot)
        {
            // Try to get PixelPerfectCamera first, fall back to regular Camera
            var pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
            var camera = GetComponent<Camera>();

            if (pixelPerfectCamera != null && camera != null)
            {
                // Use PixelPerfectCamera settings for more accurate pixel-perfect screenshots
                Debug.Log("Taking screenshot with PixelPerfectCamera");
                TakePixelPerfectScreenshot(camera, pixelPerfectCamera);
            }
            else if (camera != null)
            {
                // Fall back to regular camera
                Debug.Log("Taking screenshot with regular Camera");
                TakeRegularScreenshot(camera);
            }
            else
            {
                Debug.LogError("No Camera or PixelPerfectCamera component found!");
            }

            takeHiResShot = false;
        }
    }

    private void TakePixelPerfectScreenshot(Camera camera, PixelPerfectCamera pixelPerfectCamera)
    {
        // For pixel perfect cameras, we might want to use the reference resolution
        // or the current pixel-perfect settings
        int width = resWidth;
        int height = resHeight;

        // Optionally, you can use the pixel perfect camera's reference resolution
        // Uncomment these lines if you want to use the reference resolution instead:
        // width = pixelPerfectCamera.refResolutionX;
        // height = pixelPerfectCamera.refResolutionY;

        RenderTexture rt = new RenderTexture(width, height, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(width, height);
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("Took pixel-perfect screenshot to: {0}", filename));
    }

    private void TakeRegularScreenshot(Camera camera)
    {
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(resWidth, resHeight);
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("Took regular screenshot to: {0}", filename));
    }
}
