using UnityEngine;

// Example of a script you attach to your own scene object: it mirrors the local player's state into the Inspector.
// Copy this file as a starting point for your own scripts; the game's classes (Char, TileMap, GameCanvas...) are global.
public class NsoPlayerInfo : MonoBehaviour
{
    [Header("Read-only (updated while playing)")]
    public string playerName;
    public string map;
    public int level;
    public int x;
    public int y;
    public int hp;
    public int maxHp;

    private void Update()
    {
        if (!Application.isPlaying) return;
        Char me = Char.getMyChar();
        if (me == null) return;
        playerName = me.cName;
        map = TileMap.mapName;
        level = me.clevel;
        x = me.cx;
        y = me.cy;
        hp = me.cHP;
        maxHp = me.cMaxHP;
    }
}
