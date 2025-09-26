using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float _minSpeed, _maxSpeed;

    private float _speed;

    private void Start()
    {
        _speed = Random.Range(_minSpeed, _maxSpeed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Game Over");
            Destroy(collision.gameObject);
        }
    }

    private void Update()
    {
        transform.Translate(_speed * Time.deltaTime * Vector3.down);
    }
}
