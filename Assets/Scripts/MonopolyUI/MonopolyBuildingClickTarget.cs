using UnityEngine;

public class MonopolyBuildingClickTarget : MonoBehaviour
{
    [SerializeField] private BoardGridView gridView;

    public BoardGridView GridView => gridView;

    public void Bind(BoardGridView targetGrid)
    {
        gridView = targetGrid;
    }
}
