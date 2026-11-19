using UnityEngine;

// Attach to a prefab root. Animate a child so clips cannot move the gameplay root.
public class ForestAnimatedActor : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer mainSprite;
    [Min(0)] public float deathSeconds = 1;
    ForestGame owner;
    Vector3 previous;
    float deathRemaining = -1;
    readonly System.Collections.Generic.Dictionary<string, AnimatorControllerParameterType> parameters =
        new System.Collections.Generic.Dictionary<string, AnimatorControllerParameterType>();

    public void Initialize(ForestGame game, int order)
    {
        owner = game;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!mainSprite) mainSprite = GetComponentInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.sortingOrder = order;
        if (animator && animator.runtimeAnimatorController)
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                parameters[parameter.name] = parameter.type;
        previous = transform.position;
    }

    public void Trigger(string action)
    {
        if (animator && parameters.TryGetValue(action, out var type) && type == AnimatorControllerParameterType.Trigger)
            animator.SetTrigger(action);
    }

    void Float(string name, float value)
    {
        if (animator && parameters.TryGetValue(name, out var type) && type == AnimatorControllerParameterType.Float)
            animator.SetFloat(name, value);
    }

    void Bool(string name, bool value)
    {
        if (animator && parameters.TryGetValue(name, out var type) && type == AnimatorControllerParameterType.Bool)
            animator.SetBool(name, value);
    }

    public void Die(bool remove)
    {
        Bool("Dead", true);
        Bool("Moving", false);
        Float("Speed", 0);
        Trigger("Die");
        if (remove) deathRemaining = deathSeconds;
    }

    void LateUpdate()
    {
        bool frozen = owner && owner.AnimationPaused;
        if (animator) animator.speed = frozen ? 0 : 1;
        Vector2 delta = transform.position - previous;
        previous = transform.position;
        if (frozen) return;
        if (deathRemaining >= 0)
        {
            deathRemaining -= Time.deltaTime;
            if (deathRemaining <= 0) Destroy(gameObject);
            return;
        }
        float speed = delta.magnitude / Mathf.Max(.0001f, Time.deltaTime);
        Float("Speed", speed);
        Bool("Moving", speed > .01f);
        if (speed > .01f)
        {
            Float("MoveX", delta.normalized.x);
            Float("MoveY", delta.normalized.y);
        }
    }
}
