using System;

namespace Game.General
{
    /// <summary>
    /// Specifies the category of a law blank. Defines its sprite and author.
    /// </summary>
    [Serializable]
    public enum DocumentType
    {
        Healthcare,
        Infrastructure,
        Science,
        Ecology,
        Education,
        Welfare,
        Agriculture,
        Army,
        Economy,
        Court,
        Diplomacy,
        GlobalProjects
    }
}
