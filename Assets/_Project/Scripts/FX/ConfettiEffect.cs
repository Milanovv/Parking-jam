using UnityEngine;

public class ConfettiEffect : MonoBehaviour
{
    public static ConfettiEffect Spawn(Vector3 position)
    {
        var go = new GameObject("Confetti");
        go.transform.position = position;
        var effect = go.AddComponent<ConfettiEffect>();
        effect.Configure();
        return effect;
    }

    private void Configure()
    {
        var ps = gameObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 6f;
        main.startSize = 0.25f;
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 80)
        });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;
        ps.Play();
        Destroy(gameObject, 3f);
    }
}