using UnityEngine;

namespace MingleGame.Core.Components;

[DisallowMultipleComponent]
internal class Pony : MonoBehaviour
{
    private Vector3? _currentTarget;

    public Vector3 StartPosition 
    { 
        get;
        set => _currentTarget = field = value;
    }

    public Vector3 EndPosition { get; set; }

    public float Speed { get; set; } = 0.5f;

    private void Update()
    {
        if (_currentTarget is null)
            return;
        
        if (Vector3.Distance(transform.position, _currentTarget.Value) < 0.1f)
            _currentTarget = _currentTarget == StartPosition ? EndPosition : StartPosition;

        transform.position = Vector3.MoveTowards(transform.position, _currentTarget.Value, Speed * Time.deltaTime);
    }
}
