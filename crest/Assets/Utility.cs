using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static partial class Utility
{
	public static void Draw3DCross(Vector3 point, float size = 0.1f)
	{
		Draw3DCross(point, Color.white, size);
	}

	public static void Draw3DCross(Vector3 point, float size, float duration)
	{
		Draw3DCross(point, size, duration, Color.white);
	}

	public static void Draw3DCross(Vector3 point, float size, Color color)
	{
		Draw3DCross(point, color, size);
	}

	public static void Draw3DCross(Vector3 point, float size, Color color, float duration)
	{
		Draw3DCross(point, size, duration, color);
	}

	public static void Draw3DCross(Vector3 point, Color color)
	{
		Draw3DCross(point, color, 0.1f);
	}

	public static void Draw3DCross(Vector3 point, Color color, float size)
	{
		Debug.DrawLine(point, point + Vector3.up * size, color);
		Debug.DrawLine(point, point + Vector3.down * size, color);
		Debug.DrawLine(point, point + Vector3.right * size, color);
		Debug.DrawLine(point, point + Vector3.left * size, color);
		Debug.DrawLine(point, point + Vector3.forward * size, color);
		Debug.DrawLine(point, point + Vector3.back * size, color);
	}

	public static void Draw3DCross(Vector3 point, float size, float duration, Color color)
	{
		Debug.DrawLine(point, point + Vector3.up * size, color, duration);
		Debug.DrawLine(point, point + Vector3.down * size, color, duration);
		Debug.DrawLine(point, point + Vector3.right * size, color, duration);
		Debug.DrawLine(point, point + Vector3.left * size, color, duration);
		Debug.DrawLine(point, point + Vector3.forward * size, color, duration);
		Debug.DrawLine(point, point + Vector3.back * size, color, duration);
	}

	public static void DrawDirectionLine(Vector3 p1, Vector3 p2, Color color, float duration = 0, float arrowSize = 0.01f)
	{
		DrawRay(p1, p2 - p1, color, duration, arrowSize);
	}

	public static void DrawDirectionLine(Vector3 p1, Vector3 p2, float duration = 0, float arrowSize = 0.01f)
	{
		DrawRay(p1, p2 - p1, Color.white, duration, arrowSize);
	}

	public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration = 0, float arrowSize = 0.01f)
	{
		if(duration != 0)
			// First draw a regular old ray
			Debug.DrawRay(origin, direction, color, duration);
		else
			Debug.DrawRay(origin, direction, color);

		//		if(duration != 0)
		//		// Now draw an arrow type thing on the head of the thing. Got lazy and just made it a cross
		//			Draw3DCross(origin + direction, color, arrowSize, duration);
		//		else
		//			Draw3DCross(origin + direction, color, arrowSize);

		// Arrow base
		Vector3 arrowTip = origin + direction;
		Vector3 v3;
		Vector3 arrowBase = arrowTip - direction / 10f;
		int arrowSides = 16;

		if(direction.normalized != Vector3.forward)
			v3 = Vector3.Cross(direction, Vector3.forward).normalized * direction.magnitude * arrowSize;
		else
			v3 = Vector3.Cross(direction, Vector3.up).normalized * direction.magnitude * arrowSize;

		Vector3 lastPoint = v3 + arrowBase;

		float change = 360 / (float)arrowSides;

		for(float i = 0; i < arrowSides; i++)
		{
			var q = Quaternion.AngleAxis(change, direction);
			v3 = q * v3;
			var point = arrowBase + v3;

			if(duration == 0)
			{
				Debug.DrawLine(arrowTip, point, color);
				Debug.DrawLine(lastPoint, point, color);
			}
			else
			{
				Debug.DrawLine(arrowTip, point, color, duration);
				Debug.DrawLine(lastPoint, point, color, duration);
			}

			lastPoint = point;
		}
	}

	public static void DrawBox(Bounds bounds, Color color)
	{
		DrawBox(bounds.center, bounds.size, color);
	}

	public static void DrawBox(Vector3 center, Vector3 size, Color color)
	{
		Vector3 topP1 = center + new Vector3(size.x, size.y, size.z) / 2;
		Vector3 topP2 = center + new Vector3(-size.x, size.y, size.z) / 2;
		Vector3 topP3 = center + new Vector3(-size.x, size.y, -size.z) / 2;
		Vector3 topP4 = center + new Vector3(size.x, size.y, -size.z) / 2;

		Vector3 bottomP1 = center + new Vector3(size.x, -size.y, size.z) / 2;
		Vector3 bottomP2 = center + new Vector3(-size.x, -size.y, size.z) / 2;
		Vector3 bottomP3 = center + new Vector3(-size.x, -size.y, -size.z) / 2;
		Vector3 bottomP4 = center + new Vector3(size.x, -size.y, -size.z) / 2;

		Utility.DrawLine(topP1, topP2, color);
		Utility.DrawLine(topP2, topP3, color);
		Utility.DrawLine(topP3, topP4, color);
		Utility.DrawLine(topP4, topP1, color);

		Utility.DrawLine(bottomP1, bottomP2, color);
		Utility.DrawLine(bottomP2, bottomP3, color);
		Utility.DrawLine(bottomP3, bottomP4, color);
		Utility.DrawLine(bottomP4, bottomP1, color);

		Utility.DrawLine(topP1, bottomP1, color);
		Utility.DrawLine(topP2, bottomP2, color);
		Utility.DrawLine(topP3, bottomP3, color);
		Utility.DrawLine(topP4, bottomP4, color);
	}

	#region Arrows

	/// <summary>Will only display in an instant</summary>
	public static void DrawArrow(Vector3 origin, Vector3 directionAndLength)
	{
		DrawArrow(origin, directionAndLength.normalized, directionAndLength.magnitude, 0, Color.white, 16);
	}

	//	public static void DrawArrow(Vector3 origin, Vector3 directionAndLength, float duration)
	//	{
	//		DrawArrow(origin, directionAndLength.normalized, directionAndLength.magnitude, duration, Color.white, 16);
	//	}

	/// <summary>Will only display in an instant</summary>
	public static void DrawArrow(Vector3 origin, Vector3 directionAndLength, Color color)
	{
		DrawArrow(origin, directionAndLength.normalized, directionAndLength.magnitude, 0, color, 16);
	}

	public static void DrawArrow(Vector3 origin, Vector3 directionAndLength, float arrowDuration)
	{
		DrawArrow(origin, directionAndLength.normalized, directionAndLength.magnitude, arrowDuration, Color.white);
	}

	public static void DrawArrow(Vector3 origin, Vector3 direction, float arrowLength, Color color)
	{
		DrawArrow(origin, direction, arrowLength, 0, color, 16);
	}

	public static void DrawArrow(Vector3 origin, Vector3 directionAndLength, Color color, float arrowDuration)
	{
		DrawArrow(origin, directionAndLength.normalized, directionAndLength.magnitude, arrowDuration, color, 16);
	}

	public static void DrawArrow(Vector3 origin, Vector3 direction, float arrowLength, float arrowDuration, Color color, int arrowSides = 16)
	{
		//		if(direction == Vector3.forward)
		//			Debug.LogError("Direction on arrow is forward, this can not render due to some math issue.");

		if(arrowSides < 3)
			arrowSides = 3;

		if(arrowSides > 360)
			arrowSides = 360;

		// First draw a line 

		Vector3 arrowTip = origin + (direction / 2 * arrowLength);

		direction.Normalize();

		float size = arrowLength;

		if(arrowDuration == 0)
			Debug.DrawLine(arrowTip, origin - (direction / 2 * arrowLength), color);
		else
			Debug.DrawLine(arrowTip, origin - (direction / 2 * arrowLength), color, arrowDuration);

		// Arrow base
		Vector3 v3;
		Vector3 arrowBase = arrowTip - (direction / 4 * size);

		if(direction.normalized != Vector3.forward)
			v3 = Vector3.Cross(direction, Vector3.forward).normalized * direction.magnitude * size / 8;
		else
			v3 = Vector3.Cross(direction, Vector3.up).normalized * direction.magnitude * size / 8;

		Vector3 lastPoint = v3 + arrowBase;

		float change = 360 / (float)arrowSides;

		for(float i = 0; i < arrowSides; i++)
		{
			var q = Quaternion.AngleAxis(change, direction);
			v3 = q * v3;
			var point = arrowBase + v3;
			if(arrowDuration == 0)
			{
				Debug.DrawLine(arrowTip, point, color);
				Debug.DrawLine(lastPoint, point, color);
			}
			else
			{
				Debug.DrawLine(arrowTip, point, color, arrowDuration);
				Debug.DrawLine(lastPoint, point, color, arrowDuration);
			}

			lastPoint = point;
		}
	}

	#endregion

	public static void DrawPlane(Vector3 position, Vector3 normal, float size = 0.025f)
	{
		DrawPlane(position, normal, size, Color.gray, 1f, Color.black);
	}

	public static void DrawPlane(Vector3 position, Vector3 normal, float size, Color planeColor, float normalLength, Color normalColour)
	{
		Vector3 v3;

		if(normal.normalized != Vector3.forward)
			v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude * size;
		else
			v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude * size;

		var corner0 = position + v3;
		var corner2 = position - v3;
		var q = Quaternion.AngleAxis(90.0f, normal);
		v3 = q * v3;
		var corner1 = position + v3;
		var corner3 = position - v3;

		Debug.DrawLine(corner0, corner2, planeColor);
		Debug.DrawLine(corner1, corner3, planeColor);
		Debug.DrawLine(corner0, corner1, planeColor);
		Debug.DrawLine(corner1, corner2, planeColor);
		Debug.DrawLine(corner2, corner3, planeColor);
		Debug.DrawLine(corner3, corner0, planeColor);
		Debug.DrawRay(position, normal * size * normalLength, normalColour);
	}


	public static void DrawLine(Vector3 p1, Vector3 p2)
	{
		Color color = Color.white;
		DrawLine(p1, p2, color);
	}

	public static void DrawLine(Vector3 p1, Vector3 p2, Color color, bool drawEnds = false, float endSize = 0.025f)
	{
		Debug.DrawLine(p1, p2, color);
		if(drawEnds == false)
			return;
		Utility.Draw3DCross(p1, Color.green, endSize);
		Utility.Draw3DCross(p2, Color.red, endSize);
	}

	public static void DrawLine(Vector3 p1, Vector3 p2, Color color, float duration, bool drawEnds = false)
	{
		Debug.DrawLine(p1, p2, color, duration);
		if(drawEnds == false)
			return;
		Utility.Draw3DCross(p1, 0.025f, duration, Color.green);
		Utility.Draw3DCross(p2, 0.025f, duration, Color.red);
	}

	public static void DrawLine(Vector3 p1, Vector3 p2, float crossSizes, Color color, bool drawEnds = false)
	{
		Debug.DrawLine(p1, p2, color);
		if(drawEnds == false)
			return;
		Utility.Draw3DCross(p1, Color.green, crossSizes);
		Utility.Draw3DCross(p2, Color.red, crossSizes);
	}

	static Color[] staticColours = new Color[] { Color.cyan, Color.magenta, Color.green, Color.yellow, Colour.Orange, Color.red, Color.black };

	public static void DrawDifferentColourLines(List<Vector3> points)
	{
		int colorIndex = 0;

		for(int i = 0; i < points.Count - 1; i++)
		{
			Color color = staticColours[colorIndex];
			Utility.DrawLine(points[i], points[i + 1], color);
			colorIndex++;
			if(colorIndex >= staticColours.Length)
				colorIndex = 0;
		}
	}

	public static void DrawIndexColouredLine(Vector3 startPoint, Vector3 endPoint, int index)
	{
		while(index >= staticColours.Length)
			index -= staticColours.Length;

		Color color = staticColours[index];

		Utility.DrawLine(startPoint, endPoint, color);
	}

	public static void DrawLines(List<Vector3> points, Color color, bool includePoints = false)
	{
		if(points == null)
			return;

		for(int i = 0; i < points.Count - 1; i++)
		{
			Utility.DrawLine(points[i], points[i + 1], color);
		}
	}

	public static void DrawLines(List<Vector3> points, Color color, float duration)
	{
		if(points == null)
			return;

		for(int i = 0; i < points.Count - 1; i++)
		{
			Utility.DrawLine(points[i], points[i + 1], color, duration);
		}
	}
}

public struct Colour
{	
	public static Color Orange { get { return new Color(1, 69f/255f, 0); } }
	public static Color DarkGreen { get { return new Color(0, 100f/255f, 0); } }
	public static Color FadedWhite { get { return new Color(1, 1, 1, 0.25f); } }
	public static Color FadedBlack { get { return new Color(0, 0, 0, 0.25f); } }
	public static Color Pink { get { return new Color(1, 105f / 255f, 180f / 255f); } }
	public static Color[] BunchOfColours = new Color[] { Color.cyan, Color.magenta, Color.green, Color.yellow, Colour.Orange, Color.red, Color.black };

	public static Color ColourFromIndex(int index)
	{
		return BunchOfColours[index % BunchOfColours.Length];
	}
}