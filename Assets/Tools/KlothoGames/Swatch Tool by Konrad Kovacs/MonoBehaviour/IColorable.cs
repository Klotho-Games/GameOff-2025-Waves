using UnityEngine;

/// <summary>
/// Interface for components that can have their color controlled by the Swatch system.
/// </summary>
public interface IColorable
{
    Color color { get; set; }
}