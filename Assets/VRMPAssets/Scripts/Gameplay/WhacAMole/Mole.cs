using UnityEngine;
using System.Collections;

namespace XRMultiplayer
{
    public class Mole : MonoBehaviour
    {
        public bool isVisible = false;
        
        Vector3 m_HiddenPosition;
        Vector3 m_VisiblePosition;
        float m_Speed = 5.0f;
        float m_PopDuration = 1.0f; 
        Coroutine m_PopRoutine;

        void Awake()
        {
            m_HiddenPosition = transform.localPosition;
            m_VisiblePosition = m_HiddenPosition + Vector3.up * 0.2f; // Pop up 0.2 units
        }

        public void PopUp()
        {
            if (m_PopRoutine != null) StopCoroutine(m_PopRoutine);
            m_PopRoutine = StartCoroutine(PopRoutine());
        }

        public void OnHit()
        {
            if (!isVisible) return;
            
            // Visual feedback (change color temporarily)
            StartCoroutine(FlashColor());

            // Hide immediately
            if (m_PopRoutine != null) StopCoroutine(m_PopRoutine);
            StartCoroutine(MoveTo(m_HiddenPosition));
            isVisible = false;
        }

        IEnumerator PopRoutine()
        {
            isVisible = true;
            yield return StartCoroutine(MoveTo(m_VisiblePosition));
            yield return new WaitForSeconds(m_PopDuration);
            yield return StartCoroutine(MoveTo(m_HiddenPosition));
            isVisible = false;
        }

        IEnumerator MoveTo(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.localPosition, targetPos) > 0.01f)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, m_Speed * Time.deltaTime);
                yield return null;
            }
            transform.localPosition = targetPos;
        }

        IEnumerator FlashColor()
        {
            Renderer rend = GetComponent<Renderer>();
            Color original = rend.material.color;
            rend.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            rend.material.color = original;
        }
    }
}
