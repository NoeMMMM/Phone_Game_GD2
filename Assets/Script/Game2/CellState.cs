/// <summary>
/// État logique d'une case de la grille du Snake (Jeu 2).
/// Utilisé par SnakeGrid pour représenter ce qu'occupe chaque case.
/// </summary>
public enum CellState
{
    /// <summary>Case libre.</summary>
    Empty,

    /// <summary>Case occupée par la tête du chevalier.</summary>
    Knight,

    /// <summary>Case occupée par un segment de queue du chevalier.</summary>
    Tail,

    /// <summary>Case occupée par un indice (cible à collecter).</summary>
    Clue,
}
