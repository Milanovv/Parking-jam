using UnityEngine;

public class PedestrianView : MonoBehaviour
{
    private Pedestrian _model;
    private GridController _gridController;

    public Pedestrian Model => _model;

    public void Initialize(Pedestrian model, GridController gridController)
    {
        _model = model;
        _gridController = gridController;
        SnapToModel();
    }

    private void LateUpdate()
    {
        SnapToModel();
    }

    private void SnapToModel()
    {
        if (_model == null || _gridController == null || _model.OccupiedTiles.Length == 0) return;
        transform.position = _gridController.CellToWorld(_model.OccupiedTiles[0]);
    }
}