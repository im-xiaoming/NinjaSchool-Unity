using UnityEngine;

// Menu music and button sounds for the menu screens (title / server, language, select and create character).
// - Music: the game's login track (res/music/0.ogg, Sound.MLogin) loops on the background-loop source while a menu screen
//   is shown. It fades in, and fades out when the game starts (GameScr) or the sound is switched off.
//   Volume = menuMusicVolume x Settings > Âm thanh > Nhạc nền / môi trường.
// - Button sounds: Click() / Tick() play the game's click (res/music/1.ogg, Sound.MBClick) on the menu screens.
// Both can be switched off in Settings (F6) > Âm thanh.
public class MenuAudio : MonoBehaviour
{
    public static bool menuMusic = true;          // Settings > Âm thanh > Nhạc menu
    public static bool uiSounds = true;           // Settings > Âm thanh > Âm thanh nút bấm
    public static float menuMusicVolume = 0.6f;   // loudness of the menu track before the settings volume
    private const float FadeSeconds = 1.2f;       // fade in / fade out time

    private float level;      // 0..1 fade position
    private bool ours;        // the background source is playing our menu track

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        GameObject go = new GameObject("MenuAudio");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<MenuAudio>();
    }

    // The screens that count as "menu".
    public static bool OnMenuScreen
    {
        get
        {
            mScreen s = GameCanvas.currentScreen;
            return s is SelectServerScr || s is LanguageScr || s is SelectCharScr || s is CreateCharScr;
        }
    }

    // Confirm / button press sound on menu screens.
    public static void Click()
    {
        if (uiSounds && OnMenuScreen) Sound.play(Sound.MBClick, 0.5f);
    }

    // Softer sound for moving the selection on menu screens.
    public static void Tick()
    {
        if (uiSounds && OnMenuScreen) Sound.play(Sound.MBClick, 0.25f);
    }

    private static AudioClip MenuClip { get { return Sound.mysound != null && Sound.mysound.Length > Sound.MLogin ? Sound.mysound[Sound.MLogin] : null; } }

    private void Update()
    {
        AudioSource src = Sound.SoundBGLoop;
        AudioClip clip = MenuClip;
        if (src == null || clip == null) return;   // sound not loaded yet
        if (ours && src.clip != clip) { ours = false; level = 0f; }   // something else took the source (map music)

        bool want = menuMusic && Sound.isSound && OnMenuScreen;
        if (want && !ours)
        {
            Sound.playSoundBGLoop(Sound.MLogin, menuMusicVolume);
            ours = src.isPlaying && src.clip == clip;
            level = 0f;
        }
        if (!ours) return;

        level = Mathf.MoveTowards(level, want ? 1f : 0f, Time.unscaledDeltaTime / FadeSeconds);
        src.volume = menuMusicVolume * Sound.ambientVolume * level;
        if (!want && level <= 0f)
        {
            src.Stop();
            ours = false;
        }
    }
}
