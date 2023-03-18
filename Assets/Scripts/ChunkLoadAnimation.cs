using UnityEngine;

public class ChunkLoadAnimation : MonoBehaviour
{
    private const float speed = 3f;
    private Vector3 targetPos;

    private float waitTimer;
    private float timer;
    
    private void Start()
    {
        waitTimer = Random.Range(0f, 3f);
        var transform1 = transform;
        var position = transform1.position;
        targetPos = position;
        position = new Vector3(position.x, -VoxelData.ChunkHeight, position.z);
        transform1.position = position;
    }

    private void Update()
    {
        if (timer < waitTimer)
        {
            timer += Time.deltaTime;   
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
            if (!(targetPos.y - transform.position.y < 0.05f)) return;
            var transform1 = transform;
            transform1.position = targetPos;
            Destroy(this);   
        }
    }
}
