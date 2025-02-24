using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialType {
    None,
    Striped,
    Bomb,
    Rainbow
}

public class Piece : MonoBehaviour {
    public int row;          // Grid'deki X pozisyonu (horizontal)
    public int col;          // Grid'deki Y pozisyonu (vertical)
    public int colorIndex;   // Normal parçalar için renk indeksi (ör. 0-5)
    public SpecialType specialType = SpecialType.None;  // Özel taş tipi (varsayılan normal)
    public BoardManager board; // BoardManager referansı
    public bool isMoving { get; private set; } = false; // Parça hareket halinde mi?

    // Parçayı hedef pozisyona yumuşakça hareket ettirir.
    public IEnumerator MoveCoroutine(Vector3 targetPos, float duration) {
        isMoving = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < duration) {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
    }

    // Parçanın yok olma animasyonu (küçülerek) ve sonrasında parçayı yok eder.
    public IEnumerator AnimateDisappearance(float duration) {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration) {
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }

    // Grid koordinatlarını günceller.
    public void SetPosition(int newRow, int newCol) {
        row = newRow;
        col = newCol;
    }

    // Özel taş efektini tetikler.
    public void ActivateSpecialEffect() {
        StartCoroutine(ActivateSpecialEffectRoutine());
    }

    // Özel efektin coroutine'i: ilgili efekti board üzerinden çalıştırır, ardından parçayı yok eder.
    public IEnumerator ActivateSpecialEffectRoutine() {
        StartCoroutine(AnimateDisappearance(board.disappearDuration));
        switch (specialType) {
            case SpecialType.Striped:
                Debug.Log("Striped efekt tetiklendi!");
                yield return board.ProcessSpecialEffect(SpecialType.Striped, row, col, colorIndex);
                break;
            case SpecialType.Bomb:
                Debug.Log("Bomb efekt tetiklendi!");
                yield return board.ProcessSpecialEffect(SpecialType.Bomb, row, col, colorIndex);
                break;
            case SpecialType.Rainbow:
                Debug.Log("Rainbow efekt tetiklendi!");
                yield return board.ProcessSpecialEffect(SpecialType.Rainbow, row, col, colorIndex);
                break;
            default:
                break;
        }
        // Efekt çalıştıktan sonra parçayı animasyonla yok et.
        
    }
}