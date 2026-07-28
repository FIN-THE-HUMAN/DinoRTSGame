using UnityEngine;

namespace RTSFramework.Combat
{
    public static class RTSCombatVFXManager
    {
        public static void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
        {
            GameObject flashObj = new GameObject("MuzzleFlash_VFX");
            flashObj.transform.position = position;
            flashObj.transform.rotation = rotation;

            var ps = flashObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 12));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.05f;

            var psr = flashObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Shader particlesShader = Shader.Find("Legacy Shaders/Particles/Additive");
                if (particlesShader == null) particlesShader = Shader.Find("Sprites/Default");
                if (particlesShader != null)
                {
                    psr.material = new Material(particlesShader);
                }
            }

            ps.Play();
        }

        public static void SpawnImpactEffect(Vector3 position)
        {
            GameObject impactObj = new GameObject("Impact_VFX");
            impactObj.transform.position = position;

            var ps = impactObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.15f, 0.95f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 18));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            var psr = impactObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Shader particlesShader = Shader.Find("Legacy Shaders/Particles/Additive");
                if (particlesShader == null) particlesShader = Shader.Find("Sprites/Default");
                if (particlesShader != null)
                {
                    psr.material = new Material(particlesShader);
                }
            }

            ps.Play();
        }

        public static void SpawnMeleeImpactEffect(Vector3 position)
        {
            GameObject slashObj = new GameObject("MeleeImpact_VFX");
            slashObj.transform.position = position;

            var ps = slashObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.9f, 0.95f, 0.85f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 6));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var psr = slashObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Shader particlesShader = Shader.Find("Sprites/Default");
                if (particlesShader != null)
                {
                    psr.material = new Material(particlesShader);
                }
            }

            ps.Play();
        }

        public static void SpawnBuildingDestructionEffect(Vector3 position)
        {
            // Main dust cloud system
            GameObject debrisObj = new GameObject("BuildingDestruction_VFX");
            debrisObj.transform.position = position;

            var ps = debrisObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = false;
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.42f, 0.4f, 0.6f)); // stone dust
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 30));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3f, 0.5f, 3f);

            var psr = debrisObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Shader particlesShader = Shader.Find("Sprites/Default");
                if (particlesShader != null)
                {
                    psr.material = new Material(particlesShader);
                }
            }

            // Sub-system for flying stone chunks
            GameObject chunksObj = new GameObject("StoneChunks");
            chunksObj.transform.SetParent(debrisObj.transform, false);
            chunksObj.transform.localPosition = Vector3.zero;

            var ps2 = chunksObj.AddComponent<ParticleSystem>();
            var main2 = ps2.main;
            main2.duration = 1.0f;
            main2.loop = false;
            main2.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
            main2.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            main2.startLifetime = new ParticleSystem.MinMaxCurve(0.8f);
            main2.startColor = new ParticleSystem.MinMaxGradient(new Color(0.28f, 0.28f, 0.28f, 1f));
            main2.gravityModifier = new ParticleSystem.MinMaxCurve(1.5f);
            main2.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission2 = ps2.emission;
            emission2.rateOverTime = 0f;
            emission2.burstCount = 1;
            emission2.SetBurst(0, new ParticleSystem.Burst(0f, 20));

            var shape2 = ps2.shape;
            shape2.shapeType = ParticleSystemShapeType.Hemisphere;
            shape2.radius = 1.2f;

            var psr2 = chunksObj.GetComponent<ParticleSystemRenderer>();
            if (psr2 != null)
            {
                Shader particlesShader = Shader.Find("Sprites/Default");
                if (particlesShader != null)
                {
                    psr2.material = new Material(particlesShader);
                }
            }

            ps.Play();
            ps2.Play();
        }
    }
}
