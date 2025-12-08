using UnityEngine;

namespace XRMultiplayer
{
    public class FlyingObject : MonoBehaviour
    {
        public float speed = 5.0f;
        public Vector3 direction = Vector3.right;
        public float lifeTime = 10.0f;

        void Start()
        {
            Destroy(gameObject, lifeTime);
            // Random color for fun
            GetComponent<Renderer>().material.color = Random.ColorHSV();
        }

        void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }
}
