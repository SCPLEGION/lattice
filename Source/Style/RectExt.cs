using UnityEngine;

namespace Lattice;

public static class RectExt
{
	public static Rect MovedBy(this Rect rect, Vector2 offset) => new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);
}
