using UnityEngine;

namespace XRMultiplayer
{
    public class FlyingObjectSpawner : MonoBehaviour
    {
        [SerializeField] float m_SpawnInterval = 2.0f;
        [SerializeField] float m_SphereSpeed = 3.0f;
        
        // Spawn area defaults relative to origin
        [SerializeField] Vector3 m_SpawnOrigin = new Vector3(-5, 1, 3);
        [SerializeField] Vector3 m_SpawnVariance = new Vector3(0, 1, 1); // Random range modifiers

        float m_Timer = 0;

        void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer >= m_SpawnInterval)
            {
                SpawnSphere();
                m_Timer = 0;
            }
        }

        void SpawnSphere()
        {
            // Create a primitive sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            // Calculate random position
            Vector3 randomPos = m_SpawnOrigin + new Vector3(
                0, 
                Random.Range(-m_SpawnVariance.y, m_SpawnVariance.y),
                Random.Range(-m_SpawnVariance.z, m_SpawnVariance.z)
            );

            sphere.transform.position = randomPos;
            sphere.transform.localScale = Vector3.one * 0.3f; // Smaller spheres

            // Add movement script
            FlyingObject mover = sphere.AddComponent<FlyingObject>();
            mover.speed = m_SphereSpeed;
            mover.direction = Vector3.right; // Move Right
        }
    }
}
