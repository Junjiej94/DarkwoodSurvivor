using UnityEngine;

public partial class ForestGame
{
    [Header("Optional animated visual prefabs")]
    public ForestAnimatedActor playerPrefab, meleePrefab, rangedPrefab, minibossPrefab, bossPrefab;
    ForestAnimatedActor playerAnimation;
    public bool AnimationPaused => paused || choosing || won;

    ForestAnimatedActor SpawnActor(ForestAnimatedActor prefab, Vector2 point, float size, int order)
    {
        if (!prefab) return null;
        ForestAnimatedActor actor = Instantiate(prefab, (Vector3)point, Quaternion.identity, visuals);
        actor.transform.localScale *= size;
        actor.Initialize(this, order);
        if (actor.mainSprite) return actor;
        Debug.LogWarning("Animated prefab needs a SpriteRenderer. Using the static sprite.", prefab);
        Destroy(actor.gameObject);
        return null;
    }
}
