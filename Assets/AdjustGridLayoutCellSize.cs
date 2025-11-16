using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(UnityEngine.UI.GridLayoutGroup))]
public class AdjustGridLayoutCellSize : MonoBehaviour
{
    public enum ExpandSetting { X, Y };

    public ExpandSetting expandingSetting;
    GridLayoutGroup _gridlayout;

    private GridLayoutGroup gridlayout
    {
        get
        {
            if (!_gridlayout)
                _gridlayout = GetComponent<GridLayoutGroup>();
            return _gridlayout;
        }
    }

    int maxConstraintCount = 0;
    RectTransform layoutRect;

    // Start is called before the first frame update
    void Start()
    {
        UpdateCellSize();
    }


    private void OnValidate()
    {
        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        maxConstraintCount = gridlayout.constraintCount;
        layoutRect = gridlayout.gameObject.GetComponent<RectTransform>();

        if (expandingSetting == ExpandSetting.X)
        {
            float spaceForSpacing = (maxConstraintCount - 1) * gridlayout.spacing.x;
            float width = layoutRect.rect.width - spaceForSpacing;

            float sizePerCell = width / maxConstraintCount;
            gridlayout.cellSize = new Vector2(sizePerCell, gridlayout.cellSize.y);
        }
        else if (expandingSetting == ExpandSetting.Y)
        {
            float spaceForSpacing = (maxConstraintCount - 1) * gridlayout.spacing.y;
            float height = layoutRect.rect.height - spaceForSpacing;
            float sizePerCell = height / maxConstraintCount;
            gridlayout.cellSize = new Vector2(gridlayout.cellSize.x, sizePerCell);
        }
    }
}