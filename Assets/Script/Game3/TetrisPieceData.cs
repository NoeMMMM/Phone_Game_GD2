using UnityEngine;

/// <summary>
/// Types de pièces Tetris. L'ordre des valeurs sert d'index dans les tableaux
/// _offsets et _colors. Ne pas réordonner sans réordonner aussi ces tableaux.
/// </summary>
public enum PieceType { I, O, T, L, J, S, Z }

/// <summary>
/// Données statiques des 7 pièces Tetris (SRS simplifié, sans wall kicks).
///
/// Chaque pièce est définie par 4 rotations (0 = état initial, 1 = 90° horaire,
/// 2 = 180°, 3 = 270° horaire). Chaque rotation contient 4 offsets Vector2Int
/// relatifs au pivot (0, 0) de la pièce.
///
/// Convention des axes : Y monte vers le haut (Vector2Int.up = (0, 1)).
///
/// Classe purement statique : aucun état, aucun MonoBehaviour.
/// </summary>
public static class TetrisPieceData
{
    // ── Offsets par pièce et par rotation ────────────────────────────────────
    // Dimensions : [PieceType][rotation 0-3][bloc 0-3]

    private static readonly Vector2Int[][][] _offsets = new Vector2Int[][][]
    {
        // ── I (cyan) ─────────────────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },  // rot 0 — horizontal
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(0,-1), new Vector2Int(0,-2) },  // rot 1 — vertical
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },  // rot 2 — identique à 0
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(0,-1), new Vector2Int(0,-2) },  // rot 3 — identique à 1
        },

        // ── O (jaune) ─────────────────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0,-1), new Vector2Int(1,-1) },   // rot 0
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0,-1), new Vector2Int(1,-1) },   // rot 1 — O ne tourne pas
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0,-1), new Vector2Int(1,-1) },   // rot 2
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0,-1), new Vector2Int(1,-1) },   // rot 3
        },

        // ── T (violet) ───────────────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0,-1) },  // rot 0 — barre + bras bas
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(-1,0), new Vector2Int(0,-1) },  // rot 1 — barre + bras gauche
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },  // rot 2 — barre + bras haut
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0,-1) },  // rot 3 — barre + bras droit
        },

        // ── L (orange) ───────────────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },  // rot 0 — barre + coin haut-droit
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(0,-1), new Vector2Int(1,-1) },  // rot 1 — colonne + coin bas-droit
            new[] { new Vector2Int(-1,-1), new Vector2Int(-1,0), new Vector2Int(0, 0), new Vector2Int(1, 0) },  // rot 2 — barre + coin bas-gauche
            new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0,-1) },  // rot 3 — colonne + coin haut-gauche
        },

        // ── J (bleu, miroir de L) ─────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(-1, 1), new Vector2Int(-1,0), new Vector2Int(0, 0), new Vector2Int(1, 0) },  // rot 0 — barre + coin haut-gauche
            new[] { new Vector2Int(0, 1),  new Vector2Int(1, 1), new Vector2Int(0, 0), new Vector2Int(0,-1) },  // rot 1 — colonne + coin haut-droit
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1,-1) },  // rot 2 — barre + coin bas-droit
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(-1,-1),new Vector2Int(0,-1) },  // rot 3 — colonne + coin bas-gauche
        },

        // ── S (vert) ──────────────────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },  // rot 0 — S horizontal
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1,-1) },  // rot 1 — S vertical
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },  // rot 2 — identique à 0
            new[] { new Vector2Int(0, 1),  new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1,-1) },  // rot 3 — identique à 1
        },

        // ── Z (rouge, miroir de S) ────────────────────────────────────────────
        new Vector2Int[][]
        {
            new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) },  // rot 0 — Z horizontal
            new[] { new Vector2Int(1, 1),  new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0,-1) },  // rot 1 — Z vertical
            new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) },  // rot 2 — identique à 0
            new[] { new Vector2Int(1, 1),  new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0,-1) },  // rot 3 — identique à 1
        },
    };

    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color[] _colors = new Color[]
    {
        Color.cyan,                      // I
        Color.yellow,                    // O
        new Color(0.6f, 0f,   0.8f),    // T — violet
        new Color(1f,   0.5f, 0f),      // L — orange
        Color.blue,                      // J
        Color.green,                     // S
        Color.red,                       // Z
    };

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne les offsets de la pièce pour la rotation donnée.
    /// Le tableau retourné est une référence directe aux données internes — ne pas le modifier.
    /// La rotation est normalisée dans [0, 3] pour éviter les valeurs hors bornes
    /// ou négatives (ex. rotation = -1 → 3).
    /// </summary>
    /// <param name="type">Type de pièce.</param>
    /// <param name="rotation">Index de rotation. Normalisé automatiquement.</param>
    /// <returns>Tableau de 4 offsets Vector2Int relatifs au pivot.</returns>
    public static Vector2Int[] GetOffsets(PieceType type, int rotation)
    {
        int normalizedRotation = ((rotation % 4) + 4) % 4;
        return _offsets[(int)type][normalizedRotation];
    }

    /// <summary>
    /// Retourne la couleur de teinte associée à la pièce.
    /// </summary>
    public static Color GetColor(PieceType type)
    {
        return _colors[(int)type];
    }

    /// <summary>
    /// Retourne tous les types de pièces dans l'ordre I, O, T, L, J, S, Z.
    /// Utilisé par TetrisSpawner pour construire le bag de tirage aléatoire.
    /// </summary>
    public static PieceType[] GetAllTypes()
    {
        return new[] { PieceType.I, PieceType.O, PieceType.T, PieceType.L, PieceType.J, PieceType.S, PieceType.Z };
    }
}
