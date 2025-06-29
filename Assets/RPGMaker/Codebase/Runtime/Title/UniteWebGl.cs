using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public class UniteWebGl : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void _PatchJsFilesystem();
#endif

    void Awake() {
#if !UNITY_EDITOR && UNITY_WEBGL
        _PatchJsFilesystem();
#endif
        Destroy(this);
    }

}
