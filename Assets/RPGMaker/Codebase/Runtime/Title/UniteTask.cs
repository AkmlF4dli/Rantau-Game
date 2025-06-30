using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public class UniteTask : MonoBehaviour
{
    static UniteTask _self = null;

    void Awake() {
        _self = this;
        DontDestroyOnLoad(_self);
    }

    public static async Task Delay(int millisecondsDelay) {
        if (millisecondsDelay <= 0) return;
        var tcs = new TaskCompletionSource<bool>();
        _self.Call(millisecondsDelay / 1000.0f, tcs);
        await tcs.Task;
    }

    public static async Task WaitForTime(float sec) {
        if (sec <= 0) return;
        var tcs = new TaskCompletionSource<bool>();
        _self.Call(sec, tcs);
        await tcs.Task;
    }

    private void Call(float sec, TaskCompletionSource<bool> tcs) {
        StartCoroutine(WaitForTime(sec, tcs));
    }

    private IEnumerator WaitForTime(float sec, TaskCompletionSource<bool> tcs) {
        yield return new WaitForSeconds(sec);
        tcs.SetResult(true);
    }

}
