//mobile specific ui
using UnityEngine;

public class MobileOnlyUI : MonoBehaviour {
    private void Awake() {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        gameObject.SetActive(true);
#else
        gameObject.SetActive(false);
#endif
    }
}