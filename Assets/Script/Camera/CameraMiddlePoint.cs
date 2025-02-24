using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMiddlePoint : MonoBehaviour
{
    public BoardManager gridManager;
    private float x, y;
    private Camera cameraB;

    void Start()
    {
        if (gridManager == null){
        gridManager = FindObjectOfType<BoardManager>();
        }
        cameraB = GetComponent<Camera>(); // Kamera bileşenini al

        x = gridManager.width - 1;
        y = gridManager.height - 1;

        // Kamerayı ortalamak
        transform.position = new Vector3(x / 2, y / 2, transform.position.z);

        // Çözünürlüğe göre kamera boyutunu ayarlamak
        AdjustCameraSize();
    }

    void AdjustCameraSize()
    {
        // Ekran çözünürlüğüne göre oran hesapla
        float aspectRatio = (float)Screen.width / Screen.height;
        aspectRatio *= 0.5f;
        // Grid boyutlarına göre bir referans kameranın size'ını ayarla
        float gridAspect = x / y;

        // Kameranın ortalama size'ını belirle
        if (Screen.width> Screen.height)
        {
            // Ekran yatayda daha genişse, y'ye odaklan
            cameraB.orthographicSize = y / (1f * aspectRatio);
        }
        else
        {
            // Ekran dikeyde daha genişse, x'ye odaklan
            cameraB.orthographicSize = x / (2 * aspectRatio);
        }
    }
}