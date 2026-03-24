using UnityEngine;

public class FPS : MonoBehaviour
{
    [SerializeField, Range(1, 240)]
    private int targetFPS = 60; // ŒÅ’è‚µ‚½‚¢FPS

    void Awake()
    {
        // VSync‚ğ–³Œø‰»i”O‚Ì‚½‚ßj
        QualitySettings.vSyncCount = 0;

        // FPS‚ğŒÅ’è
        Application.targetFrameRate = targetFPS;
    }
}
