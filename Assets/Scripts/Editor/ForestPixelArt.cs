using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ForestPixelArtImporter : AssetPostprocessor
{
    internal const string Root = "Assets/ForestRoguelike/Art/PixelForest/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 128;
        importer.wrapMode = assetPath.EndsWith("grass.png", System.StringComparison.Ordinal)
            ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
    }
}

public static class ForestPixelArt
{
    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ForestPixelArtImporter.Root + name + ".png");

    // Only fill unassigned fields so applying the pack keeps manually assigned art.
    public static void Apply(ForestGame game)
    {
        Undo.RecordObject(game, "Assign Forest Pixel Art");
        if (!game.playerArt) game.playerArt = Load("player");
        if (!game.grass) game.grass = Load("grass");
        if (!game.tree) game.tree = Load("tree");
        if (!game.stump) game.stump = Load("stump");
        if (!game.chestArt) game.chestArt = Load("chest");
        if (!game.meleeArt) game.meleeArt = Load("mob_melee");
        if (!game.rangedArt) game.rangedArt = Load("mob_ranged");
        if (!game.miniArt) game.miniArt = Load("miniboss");
        if (!game.bossArt) game.bossArt = Load("boss");
        if (!game.goldArt) game.goldArt = Load("pickup_gold");
        if (!game.heartArt) game.heartArt = Load("pickup_heart");
        if (!game.xpArt) game.xpArt = Load("pickup_xp");
        if (!game.manaArt) game.manaArt = Load("pickup_mana");
        if (!game.staminaArt) game.staminaArt = Load("pickup_stamina");
        if (!game.bookArt) game.bookArt = Load("pickup_book");
        string[] weaponNames = { "weapon_sword", "weapon_staff", "weapon_bow", "weapon_pistol" };
        for (int i = 0; i < Mathf.Min(game.weapons.Length, weaponNames.Length); i++)
            if (!game.weapons[i].sprite) game.weapons[i].sprite = Load(weaponNames[i]);
        string[] activeNames = { "spell_ward", "spell_fireball", "spell_heal" };
        string[] passiveNames = { "passive_magnet", "passive_vitality", "passive_quick_shield" };
        for (int i = 0; i < 3; i++)
        {
            if (i < game.activeArt.Length && !game.activeArt[i]) game.activeArt[i] = Load(activeNames[i]);
            if (i < game.passiveArt.Length && !game.passiveArt[i]) game.passiveArt[i] = Load(passiveNames[i]);
        }
        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
    }

    [MenuItem("Forest Roguelike/Assign Pixel Art to Open Scene")]
    public static void ApplyToScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Exit Play mode before assigning the art pack.");
            return;
        }
        var games = Object.FindObjectsByType<ForestGame>(FindObjectsInactive.Include);
        foreach (ForestGame game in games) Apply(game);
        if (games.Length == 0)
            Debug.LogWarning("Open the Forest scene first using Forest Roguelike > Create or Open Game Scene.");
        else
            Debug.Log("Pixel Forest art assigned. Save the scene to keep the assignments.");
    }
}
