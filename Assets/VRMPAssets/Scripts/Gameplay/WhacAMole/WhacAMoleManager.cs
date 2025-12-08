using UnityEngine;
using System.Collections.Generic;

namespace XRMultiplayer
{
    public class WhacAMoleManager : MonoBehaviour
    {
        List<Mole> m_Moles = new List<Mole>();
        Transform m_TableTransform;

        float m_GameTimer = 0;
        float m_PopInterval = 1.5f;

        void Start()
        {
            SetupGame();
        }

        void SetupGame()
        {
            // Try to find the specific table by name, handling potential hierarchy issues
            Transform tableTrans = FindDeepChild(null, "long_table");
            
            if (tableTrans == null)
            {
                // Try finding "mainHallArea" first
                Transform mainHall = FindDeepChild(null, "mainHallArea");
                if (mainHall != null)
                {
                    tableTrans = FindDeepChild(mainHall, "long_table");
                }
            }

            if (tableTrans == null)
            {
                Debug.LogWarning("WhacAMoleManager: 'long_table' not found via deep search. Creating dummy table.");
                GameObject tableObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableObj.name = "long_table_dummy";
                tableObj.transform.position = new Vector3(0, 0.8f, 5);
                tableObj.transform.localScale = new Vector3(2, 0.1f, 1);
                m_TableTransform = tableObj.transform;
            }
            else
            {
                m_TableTransform = tableTrans;
                Debug.Log($"WhacAMoleManager: Found table at {m_TableTransform.position}");
            }

            CreateMoles();
        }

        // Helper to find child recursively by name (case insensitive partial match)
        Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
            {
                // Search all root objects
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var result = FindDeepChild(root.transform, name);
                    if (result != null) return result;
                }
                return null;
            }

            if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase) || parent.name.ToLower().Contains(name.ToLower()))
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        void CreateMoles()
        {
            // Create 3 holes/moles on the table
            for (int i = 0; i < 3; i++)
            {
                // Create Hole Visual
                GameObject holeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                holeObj.name = $"Hole_{i}";
                holeObj.transform.SetParent(m_TableTransform);
                holeObj.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f); // Flat cylinder
                
                float xOffset = (i - 1) * 0.6f;
                Vector3 position = new Vector3(xOffset, 0.01f, 0); // Slightly above table
                holeObj.transform.localPosition = position;
                
                holeObj.GetComponent<Renderer>().material.color = Color.black;
                Destroy(holeObj.GetComponent<Collider>()); // Remove collider from hole

                // Create Mole
                GameObject moleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                moleObj.name = $"Mole_{i}";
                moleObj.transform.SetParent(m_TableTransform);
                moleObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                moleObj.transform.localPosition = position; // Start at hole position (will be hidden by script)
                
                // Add script
                Mole moleScript = moleObj.AddComponent<Mole>();
                
                // Adjust material
                moleObj.GetComponent<Renderer>().material.color = Color.gray;

                m_Moles.Add(moleScript);
            }
        }

        void Update()
        {
            if (m_Moles.Count == 0) return;

            m_GameTimer += Time.deltaTime;
            if (m_GameTimer >= m_PopInterval)
            {
                m_GameTimer = 0;
                PickRandomMole();
            }
        }

        void PickRandomMole()
        {
            int index = Random.Range(0, m_Moles.Count);
            if (!m_Moles[index].isVisible)
            {
                m_Moles[index].PopUp();
            }
        }
    }
}
