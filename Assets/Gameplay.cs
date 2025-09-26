using UnityEngine;

public class Gameplay : MonoBehaviour
{
    [SerializeField] private Camera _cam;
    [SerializeField] private GameObject _obstaclePrefab;
    [SerializeField] private float _inverval;

    private float _timer;

    private void Start()
    {
        _timer = 0;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _inverval)
        {
            _timer = 0;
            SpawnObstacle();
        }
    }

    private void SpawnObstacle()
    {
        var obstacle = Instantiate(_obstaclePrefab);
        obstacle.transform.position = new Vector3(Random.Range(-4f, 4f), Random.Range(-4f, 4f), 0);
        Destroy(obstacle, 5);
    }
}
