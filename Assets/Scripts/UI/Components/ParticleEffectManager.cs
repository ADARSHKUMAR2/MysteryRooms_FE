using UnityEngine;
using System.Collections;

namespace MysteryRooms.UI
{
    public class ParticleEffectManager : MonoBehaviour
    {
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem ambientParticles;
        [SerializeField] private ParticleSystem selectionParticles;
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private ParticleSystem clickParticles;
        
        [Header("Ambient Settings")]
        [SerializeField] private bool playAmbientOnStart = true;
        [SerializeField] private float ambientIntensity = 10f;
        
        [Header("Effect Prefabs")]
        [SerializeField] private GameObject selectionEffectPrefab;
        [SerializeField] private GameObject successEffectPrefab;
        [SerializeField] private GameObject clickEffectPrefab;

        private void Start()
        {
            if (playAmbientOnStart)
                PlayAmbientEffect();
        }

        public void PlayAmbientEffect()
        {
            if (ambientParticles != null && !ambientParticles.isPlaying)
            {
                var emission = ambientParticles.emission;
                emission.rateOverTime = ambientIntensity;
                ambientParticles.Play();
            }
        }

        public void StopAmbientEffect()
        {
            if (ambientParticles != null && ambientParticles.isPlaying)
                ambientParticles.Stop();
        }

        public void PlaySelectionEffect(Vector3 position)
        {
            if (selectionParticles != null)
            {
                selectionParticles.transform.position = position;
                selectionParticles.Play();
            }
            else if (selectionEffectPrefab != null)
            {
                GameObject effect = Instantiate(selectionEffectPrefab, position, Quaternion.identity, transform);
                Destroy(effect, 2f);
            }
        }

        public void PlaySuccessEffect(Vector3 position)
        {
            if (successParticles != null)
            {
                successParticles.transform.position = position;
                successParticles.Play();
            }
            else if (successEffectPrefab != null)
            {
                GameObject effect = Instantiate(successEffectPrefab, position, Quaternion.identity, transform);
                Destroy(effect, 3f);
            }
        }

        public void PlayClickEffect(Vector3 position)
        {
            if (clickParticles != null)
            {
                clickParticles.transform.position = position;
                clickParticles.Play();
            }
            else if (clickEffectPrefab != null)
            {
                GameObject effect = Instantiate(clickEffectPrefab, position, Quaternion.identity, transform);
                Destroy(effect, 1f);
            }
        }

        public void CreateBurstEffect(Vector3 position, Color color, int particleCount = 20)
        {
            GameObject tempObj = new GameObject("TempParticleEffect");
            tempObj.transform.position = position;
            
            ParticleSystem ps = tempObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startSize = 0.1f;
            main.startSpeed = 5f;
            main.startLifetime = 1f;
            main.maxParticles = particleCount;
            
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, particleCount) });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            ps.Play();
            Destroy(tempObj, 2f);
        }
    }
}
