#if !UNITY_6000_0_OR_NEWER||UNITE_ASMREF_URP2D
#define ENABLE_PIXEL_PERFECT_CAMERA_URP
#endif

using UnityEngine;
namespace RPGMaker.Codebase.Runtime.Common
{
    public class CameraResolutionManager : ResolutionManager
    {
        RMUPixelPerfectCamera mPixelPerfectCamera = null;
#if ENABLE_PIXEL_PERFECT_CAMERA_URP
        RMUPixelPerfectCameraURP mPixelPerfectCameraUrp = null;
#endif
        protected override void UpdateResolution() {
#if ENABLE_PIXEL_PERFECT_CAMERA_URP
            if (Commons.IsURP())
            {
                if (mPixelPerfectCameraUrp == null)
                {
                    // 一応多重追加が無いようにGetしてからAddする
                    mPixelPerfectCameraUrp = GetComponent<RMUPixelPerfectCameraURP>();
                    if (mPixelPerfectCameraUrp == null)
                    {
                        mPixelPerfectCameraUrp = gameObject.AddComponent<RMUPixelPerfectCameraURP>();
                    }
                }

                mPixelPerfectCameraUrp.refResolutionX = (int) _screenWidth;
                mPixelPerfectCameraUrp.refResolutionY = (int) _screenHeight;

                if (_screenHeight / RESOLUTION_RATIO_HEIGHT > _screenWidth / RESOLUTION_RATIO_WIDTH)
                {
                    mPixelPerfectCameraUrp.assetsPPU = (int) (_screenWidth / RESOLUTION_WIDTH * 96f);
                }
                else
                {
                    mPixelPerfectCameraUrp.assetsPPU = (int) (_screenHeight / RESOLUTION_HEIGHT * 96f);
                }
            }
            else
#endif
            {
                if (mPixelPerfectCamera == null)
                {
                    // 一応多重追加が無いようにGetしてからAddする
                    mPixelPerfectCamera = GetComponent<RMUPixelPerfectCamera>();
                    if (mPixelPerfectCamera == null)
                    {
                        mPixelPerfectCamera = gameObject.AddComponent<RMUPixelPerfectCamera>();
                    }
                }

                mPixelPerfectCamera.refResolutionX = (int) _screenWidth;
                mPixelPerfectCamera.refResolutionY = (int) _screenHeight;

                if (_screenHeight / RESOLUTION_RATIO_HEIGHT > _screenWidth / RESOLUTION_RATIO_WIDTH)
                {
                    mPixelPerfectCamera.assetsPPU = (int) (_screenWidth / RESOLUTION_WIDTH * 96f);
                }
                else
                {
                    mPixelPerfectCamera.assetsPPU = (int) (_screenHeight / RESOLUTION_HEIGHT * 96f);
                }
            }
        }
    }

    public class RMUPixelPerfectCamera : UnityEngine.U2D.PixelPerfectCamera
    {
        //PixelPerfectCameraのOnGUIで解像度が奇数の場合のエラーログが出るので抑止する
        new void OnGUI() { }
    }

#if ENABLE_PIXEL_PERFECT_CAMERA_URP
#if UNITE_ASMREF_URP2D
    public class RMUPixelPerfectCameraURP : UnityEngine.Rendering.Universal.PixelPerfectCamera
    {
        //PixelPerfectCameraのOnGUIで解像度が奇数の場合のエラーログが出るので抑止する
        new void OnGUI() { }
    }
#elif !UNITY_6000_0_OR_NEWER
    public class RMUPixelPerfectCameraURP : UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera
    {
        //PixelPerfectCameraのOnGUIで解像度が奇数の場合のエラーログが出るので抑止する
        new void OnGUI() { }
    }
#endif
#endif
}
