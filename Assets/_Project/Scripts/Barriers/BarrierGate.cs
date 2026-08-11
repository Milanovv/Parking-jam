using UnityEngine;

public class BarrierGate : MonoBehaviour
{
    [SerializeField] private Transform _crossbarPivot;
    private bool _open;

    private static readonly Quaternion OpenRotation = Quaternion.Euler(0f, -90f, 0f);

    public Transform CrossbarPivot
    {
        get => _crossbarPivot;
        set => _crossbarPivot = value;
    }

    public bool IsOpen => _open;

    private void OnEnable()
    {
        RegisterWithGameManager();
    }

    private void Start()
    {
        RegisterWithGameManager();
    }

    private void RegisterWithGameManager()
    {
        GameManager.Instance?.RegisterBarrierGate(this);
    }

    private void OnDisable()
    {
        GameManager.Instance?.UnregisterBarrierGate(this);
    }

    public void SetLocked(bool locked)
    {
        _open = !locked;
        if (_crossbarPivot == null) return;
        _crossbarPivot.localRotation = _open ? OpenRotation : Quaternion.identity;
    }
}
