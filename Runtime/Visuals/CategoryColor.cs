using EldritchGames.EldritchLogger;
using System;
using UnityEngine;

[Serializable]
public class CategoryColor
{
    public LogCategory category;
    public Color color;

    public CategoryColor(LogCategory category, Color color)
    {
        this.category = category;
        this.color = color;
    }
}