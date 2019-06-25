using UnityEngine;
using System.Collections;

public class HiResScreenShots : MonoBehaviour
{
    int resWidth = 500;
    int resHeight = 400;

    private bool takeHiResShot = false;
    new Camera camera;

    private void Start()
    {
        camera = gameObject.GetComponentInChildren<Camera>();
        resWidth = camera.targetTexture.width;
        resHeight = camera.targetTexture.height;
    }

    public static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
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
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            RenderTexture.active = camera.targetTexture;
            camera.Render();
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            RenderTexture.active = Camera.main.targetTexture; // JC: added to avoid errors

            byte[] bytes = screenShot.EncodeToPNG();
            string filename = ScreenShotName(resWidth, resHeight);
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
            takeHiResShot = false;
        }
    }
}