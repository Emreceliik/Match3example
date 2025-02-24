using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Board Ayarları")]
    public int width = 8;
    public int height = 8;
    public GameObject[] piecePrefabs; // Farklı renk/visual için prefab dizisi
    public Transform boardParent;     // Board için isteğe bağlı parent nesne

    [Header("Animasyon Ayarları")]
    public float swapDuration = 0.3f;
    public float fallDuration = 0.5f;
    public float disappearDuration = 0.3f;

    [Header("Oyun Değerleri")]
    public int numColors = 6;
    public Text scoreText;
    public Text movesText;
    private int score = 0;
    private int moves = 0;

    private Piece[,] grid;
    private Piece selectedPiece;
    private Vector2 touchStart;
    private bool needsResetCheck = false;

    void Start()
    {
        grid = new Piece[width, height];
        InitializeBoard();
        StartCoroutine(CheckInitialBoard());
    }

    void Update()
    {
        HandleInput();
        // Board sabitken deadlock kontrolü: Eğer hamle yoksa board tamamen resetlensin.
        if (!IsAnyPieceMoving() && needsResetCheck)
        {
            needsResetCheck = false;
            if (!HasPossibleMoves())
            {
                StartCoroutine(ResetBoard());
            }
        }
    }

    #region Board Başlatma ve Parça Oluşturma
    void InitializeBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnNewPiece(x, y, true);
            }
        }
    }

    void SpawnNewPiece(int x, int y, bool initializing = false)
    {
        Vector3 pos = new Vector3(x, y, 0);
        int randomColor = Random.Range(0, numColors);
        GameObject newObj = Instantiate(piecePrefabs[randomColor], pos, Quaternion.identity, boardParent);
        Piece piece = newObj.GetComponent<Piece>();
        if (piece == null)
        {
            Debug.LogError($"Index {randomColor}'daki prefab, Piece component'ine sahip değil!");
            Destroy(newObj);
            return;
        }
        piece.board = this;
        piece.SetPosition(x, y);
        piece.colorIndex = randomColor;
        grid[x, y] = piece;
        if (!initializing)
        {
            // Yeni taş yukarıdan spawn olup düşüş animasyonu uygulasın.
            newObj.transform.position = new Vector3(x, height + 1, 0);
            StartCoroutine(piece.MoveCoroutine(pos, fallDuration));
        }
    }
    #endregion

    #region Girdi İşleme
    void HandleInput()
    {
        if (Application.isEditor)
        {
            // Editor'de hem fare hem dokunmatik girdileri simüle et
            HandlePCInput();
            HandleMobileInput();
        }
        else if (Application.isMobilePlatform)
        {
            HandleMobileInput();
        }
        else
        {
            HandlePCInput();
        }
    }

    void HandleMobileInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(touch.position);
                Vector2 touchPos = new Vector2(worldPos.x, worldPos.y);
                Collider2D hit = Physics2D.OverlapPoint(touchPos);
                if (hit != null)
                {
                    Piece piece = hit.GetComponent<Piece>();
                    if (piece != null)
                    {
                        selectedPiece = piece;
                        touchStart = touchPos;
                    }
                }
            }

            if (touch.phase == TouchPhase.Ended && selectedPiece != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(touch.position);
                Vector2 touchEnd = new Vector2(worldPos.x, worldPos.y);
                Vector2 swipe = touchEnd - touchStart;
                if (swipe.magnitude > 0.5f)
                {
                    if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                    {
                        if (swipe.x > 0)
                            TrySwap(selectedPiece, GetPieceAt(selectedPiece.row + 1, selectedPiece.col));
                        else
                            TrySwap(selectedPiece, GetPieceAt(selectedPiece.row - 1, selectedPiece.col));
                    }
                    else
                    {
                        if (swipe.y > 0)
                            TrySwap(selectedPiece, GetPieceAt(selectedPiece.row, selectedPiece.col + 1));
                        else
                            TrySwap(selectedPiece, GetPieceAt(selectedPiece.row, selectedPiece.col - 1));
                    }
                }
                selectedPiece = null;
            }
        }
    }

    void HandlePCInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 touchPos = new Vector2(worldPos.x, worldPos.y);
            Collider2D hit = Physics2D.OverlapPoint(touchPos);
            if (hit != null)
            {
                Piece piece = hit.GetComponent<Piece>();
                if (piece != null)
                {
                    selectedPiece = piece;
                    touchStart = touchPos;
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && selectedPiece != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 touchEnd = new Vector2(worldPos.x, worldPos.y);
            Vector2 swipe = touchEnd - touchStart;
            if (swipe.magnitude > 0.5f)
            {
                if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                {
                    if (swipe.x > 0)
                        TrySwap(selectedPiece, GetPieceAt(selectedPiece.row + 1, selectedPiece.col));
                    else
                        TrySwap(selectedPiece, GetPieceAt(selectedPiece.row - 1, selectedPiece.col));
                }
                else
                {
                    if (swipe.y > 0)
                        TrySwap(selectedPiece, GetPieceAt(selectedPiece.row, selectedPiece.col + 1));
                    else
                        TrySwap(selectedPiece, GetPieceAt(selectedPiece.row, selectedPiece.col - 1));
                }
            }
            selectedPiece = null;
        }
    }
    // void HandleInput()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //         Vector2 touchPos = new Vector2(worldPos.x, worldPos.y);
    //         Collider2D hit = Physics2D.OverlapPoint(touchPos);
    //         if (hit != null)
    //         {
    //             Piece piece = hit.GetComponent<Piece>();
    //             if (piece != null)
    //             {
    //                 selectedPiece = piece;
    //                 touchStart = touchPos;
    //             }
    //         }
    //     }

    //     if (Input.GetMouseButtonUp(0) && selectedPiece != null)
    //     {
    //         Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //         Vector2 touchEnd = new Vector2(worldPos.x, worldPos.y);
    //         Vector2 swipe = touchEnd - touchStart;
    //         if (swipe.magnitude > 0.5f)
    //         {
    //             if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
    //             {
    //                 if (swipe.x > 0)
    //                     TrySwap(selectedPiece, GetPieceAt(selectedPiece.row + 1, selectedPiece.col));
    //                 else
    //                     TrySwap(selectedPiece, GetPieceAt(selectedPiece.row - 1, selectedPiece.col));
    //             }
    //             else
    //             {
    //                 if (swipe.y > 0)
    //                     TrySwap(selectedPiece, GetPieceAt(selectedPiece.row, selectedPiece.col + 1));
    //                 else
    //                     TrySwap(selectedPiece, GetPieceAt(selectedPiece.row, selectedPiece.col - 1));
    //             }
    //         }
    //         selectedPiece = null;
    //     }
    // }
    #endregion

    #region Parça Yönetimi
    Piece GetPieceAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;
        return grid[x, y];
    }

    public void RemovePieceAt(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            grid[x, y] = null;
    }

    void TrySwap(Piece a, Piece b)
    {
        if (a == null || b == null) return;
        moves++;
        UpdateUI();
        StartCoroutine(SwapAndCheck(a, b));
        needsResetCheck = true; // Swap sonrası deadlock kontrolü yapılacak.
    }

    IEnumerator SwapAndCheck(Piece a, Piece b)
    {
        int ax = a.row, ay = a.col;
        int bx = b.row, by = b.col;
        grid[ax, ay] = b;
        grid[bx, by] = a;

        a.SetPosition(bx, by);
        b.SetPosition(ax, ay);

        Vector3 aTarget = new Vector3(bx, by, 0);
        Vector3 bTarget = new Vector3(ax, ay, 0);

        yield return StartCoroutine(a.MoveCoroutine(aTarget, swapDuration));
        yield return StartCoroutine(b.MoveCoroutine(bTarget, swapDuration));

        List<Piece> matches = GetMatches();
        if (matches.Count > 0)
        {
            yield return StartCoroutine(ProcessMatches(matches));
        }
        else
        {
            // Eşleşme yoksa swap'ı geri al.
            grid[ax, ay] = a;
            grid[bx, by] = b;
            a.SetPosition(ax, ay);
            b.SetPosition(bx, by);
            yield return StartCoroutine(a.MoveCoroutine(new Vector3(ax, ay, 0), swapDuration));
            yield return StartCoroutine(b.MoveCoroutine(new Vector3(bx, by, 0), swapDuration));
        }
    }
    #endregion

    #region Eşleşme Tespiti ve İşlenmesi
    bool IsMatch(Piece a, Piece b, Piece c)
    {
        if (a == null || b == null || c == null) return false;
        List<int> colors = new List<int>();
        if (a.specialType != SpecialType.Rainbow) colors.Add(a.colorIndex);
        if (b.specialType != SpecialType.Rainbow) colors.Add(b.colorIndex);
        if (c.specialType != SpecialType.Rainbow) colors.Add(c.colorIndex);
        if (colors.Count == 0) return true; // Tüm taşlar Rainbow ise
        int first = colors[0];
        foreach (int col in colors)
        {
            if (col != first) return false;
        }
        return true;
    }

    List<Piece> GetMatches()
    {
        List<Piece> matchingPieces = new List<Piece>();
        // Yatay eşleşmeler
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                Piece p1 = grid[x, y];
                Piece p2 = grid[x + 1, y];
                Piece p3 = grid[x + 2, y];
                if (IsMatch(p1, p2, p3))
                {
                    if (p1 != null && !matchingPieces.Contains(p1)) matchingPieces.Add(p1);
                    if (p2 != null && !matchingPieces.Contains(p2)) matchingPieces.Add(p2);
                    if (p3 != null && !matchingPieces.Contains(p3)) matchingPieces.Add(p3);
                }
            }
        }
        // Dikey eşleşmeler
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                Piece p1 = grid[x, y];
                Piece p2 = grid[x, y + 1];
                Piece p3 = grid[x, y + 2];
                if (IsMatch(p1, p2, p3))
                {
                    if (p1 != null && !matchingPieces.Contains(p1)) matchingPieces.Add(p1);
                    if (p2 != null && !matchingPieces.Contains(p2)) matchingPieces.Add(p2);
                    if (p3 != null && !matchingPieces.Contains(p3)) matchingPieces.Add(p3);
                }
            }
        }
        return matchingPieces;
    }

    IEnumerator ProcessMatches(List<Piece> matches)
    {
        // Özel taş varsa, önce onun efektini tetikle
        List<Piece> normalPieces = new List<Piece>();
        foreach (Piece p in matches)
        {
            if (p != null)
            {
                if (p.specialType != SpecialType.None)
                {
                    yield return StartCoroutine(p.ActivateSpecialEffectRoutine());
                }
                else
                {
                    normalPieces.Add(p);
                }
            }
        }
        // Normal taşların yok olma animasyonu ve grid'den kaldırılması
        foreach (Piece p in normalPieces)
        {
            if (p != null)
            {
                StartCoroutine(p.AnimateDisappearance(disappearDuration));
                RemovePieceAt(p.row, p.col);
            }
        }
        yield return new WaitForSeconds(disappearDuration);
        score += normalPieces.Count * 10;
        UpdateUI();
        yield return StartCoroutine(FillBoard());
        List<Piece> newMatches = GetMatches();
        if (newMatches.Count > 0)
            yield return StartCoroutine(ProcessMatches(newMatches));
    }

    IEnumerator FillBoard()
    {
        bool needsRefill = false;
        // Taşları aşağı kaydır
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    for (int ny = y + 1; ny < height; ny++)
                    {
                        if (grid[x, ny] != null)
                        {
                            grid[x, y] = grid[x, ny];
                            grid[x, ny] = null;
                            grid[x, y].SetPosition(x, y);
                            Vector3 targetPos = new Vector3(x, y, 0);
                            StartCoroutine(grid[x, y].MoveCoroutine(targetPos, fallDuration));
                            needsRefill = true;
                            break;
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(fallDuration);

        // Eksik hücrelere yeni taş ekle
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    SpawnNewPiece(x, y);
                    needsRefill = true;
                }
            }
        }
        yield return new WaitForSeconds(fallDuration);

        if (needsRefill)
            yield return StartCoroutine(ClearAllMatches());
    }

    IEnumerator ClearAllMatches()
    {
        List<Piece> matches = GetMatches();
        if (matches.Count > 0)
            yield return StartCoroutine(ProcessMatches(matches));
    }
    #endregion

    #region Özel Efekt İşleme
    public IEnumerator ProcessSpecialEffect(SpecialType type, int row, int col, int colorIndex)
    {
        List<Piece> piecesToClear = new List<Piece>();
        switch (type)
        {
            case SpecialType.Striped:
                // Hem satır hem sütundaki taşları temizle.
                for (int x = 0; x < width; x++)
                {
                    Piece p = grid[x, col];
                    if (p != null && !piecesToClear.Contains(p))
                        piecesToClear.Add(p);
                }
                for (int y = 0; y < height; y++)
                {
                    Piece p = grid[row, y];
                    if (p != null && !piecesToClear.Contains(p))
                        piecesToClear.Add(p);
                }
                break;
            case SpecialType.Bomb:
                for (int x = row - 1; x <= row + 1; x++)
                {
                    for (int y = col - 1; y <= col + 1; y++)
                    {
                        Piece p = GetPieceAt(x, y);
                        if (p != null && !piecesToClear.Contains(p))
                            piecesToClear.Add(p);
                    }
                }
                break;
            case SpecialType.Rainbow:
                // Rainbow efektinde, tetikleyicinin belirlediği renge sahip normal taşları (diğer Rainbow taşları hariç) topla.
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Piece p = grid[x, y];
                        if (p != null && p.colorIndex == colorIndex && p.specialType != SpecialType.Rainbow && !piecesToClear.Contains(p))
                            piecesToClear.Add(p);
                    }
                }
                break;
        }
        // Tetikleyici taş zaten board'dan kaldırıldıysa listeden çıkar.
        Piece trigger = GetPieceAt(row, col);
        if (trigger != null)
            piecesToClear.Remove(trigger);

        // Zincirleme özel efektleri önlemek için, efekt kapsamındaki tüm taşların specialType'ını None yap.
        foreach (Piece p in piecesToClear)
        {
            if (p != null)
                p.specialType = SpecialType.None;
        }

        if (piecesToClear.Count > 0)
        {
            if (type == SpecialType.Rainbow)
            {
                // Rainbow efektinde ProcessMatches çağrılmadan direkt taşlar yok edilir.
                foreach (Piece p in piecesToClear)
                {
                    if (p != null)
                    {
                        StartCoroutine(p.AnimateDisappearance(disappearDuration));
                        RemovePieceAt(p.row, p.col);
                    }
                }
                yield return new WaitForSeconds(disappearDuration);
                score += piecesToClear.Count * 10;
                UpdateUI();
                yield return StartCoroutine(FillBoard());
            }
            else
            {
                yield return StartCoroutine(ProcessMatches(piecesToClear));
            }
        }
    }
    #endregion

    #region UI ve Yardımcı Metodlar
    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (movesText) movesText.text = "Moves: " + moves;
    }

    bool IsAnyPieceMoving()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null && grid[x, y].isMoving)
                    return true;
            }
        }
        return false;
    }
    #endregion

    #region Deadlock Tespiti ve Board Resetleme
    // Hamle olup olmadığını kontrol eder.
    bool HasPossibleMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Piece current = grid[x, y];
                if (x < width - 1)
                {
                    Piece right = grid[x + 1, y];
                    if (CheckSwapForMatches(current, right))
                        return true;
                }
                if (y < height - 1)
                {
                    Piece top = grid[x, y + 1];
                    if (CheckSwapForMatches(current, top))
                        return true;
                }
            }
        }
        return false;
    }

    bool CheckSwapForMatches(Piece a, Piece b)
    {
        if (a == null || b == null) return false;
        int ax = a.row, ay = a.col;
        int bx = b.row, by = b.col;
        grid[ax, ay] = b;
        grid[bx, by] = a;
        bool matchFound = GetMatches().Count > 0;
        grid[ax, ay] = a;
        grid[bx, by] = b;
        return matchFound;
    }

    // Deadlock tespit edildiğinde board'u tamamen temizleyip yeniden oluşturur.
    IEnumerator ResetBoard()
    {
        // Tüm board'u temizle.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }
        yield return new WaitForSeconds(0.1f);

        // Board'u yeniden başlat.
        InitializeBoard();
        yield return new WaitForSeconds(fallDuration);

        // Eğer hâlâ hamle yoksa tekrar resetle.
        if (!HasPossibleMoves())
        {
            StartCoroutine(ResetBoard());
        }
    }

    IEnumerator CheckInitialBoard()
    {
        yield return StartCoroutine(ClearAllMatches());
        if (!HasPossibleMoves())
        {
            StartCoroutine(ResetBoard());
        }
    }
    #endregion
}
