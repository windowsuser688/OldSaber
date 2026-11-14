// Geometry routines extracted from supplied 0.10.1 Assembly-CSharp.
#pragma warning disable 0649 // Assigned from extracted bindings via reflection.
using UnityEngine;
namespace OldSaber
{
public class ClassicObstacleCube : MonoBehaviour
{
	private Vector2[] _uvs;

	private Mesh _mesh;

	private MeshRenderer _meshRenderer;

	private void OnDestroy() { if (_mesh) Destroy(_mesh); }
	private void Awake()
	{
		_meshRenderer = GetComponent<MeshRenderer>();
		MeshFilter component = GetComponent<MeshFilter>();
		_mesh = CreateBox();
		component.mesh = _mesh;
	}

	private Mesh CreateBox()
	{
		Mesh mesh = new Mesh();
		float num = 1f;
		float num2 = 1f;
		float num3 = 1f;
		Vector3 vector = new Vector3((0f - num) * 0.5f, (0f - num2) * 0.5f, num3 * 0.5f);
		Vector3 vector2 = new Vector3(num * 0.5f, (0f - num2) * 0.5f, num3 * 0.5f);
		Vector3 vector3 = new Vector3(num * 0.5f, (0f - num2) * 0.5f, (0f - num3) * 0.5f);
		Vector3 vector4 = new Vector3((0f - num) * 0.5f, (0f - num2) * 0.5f, (0f - num3) * 0.5f);
		Vector3 vector5 = new Vector3((0f - num) * 0.5f, num2 * 0.5f, num3 * 0.5f);
		Vector3 vector6 = new Vector3(num * 0.5f, num2 * 0.5f, num3 * 0.5f);
		Vector3 vector7 = new Vector3(num * 0.5f, num2 * 0.5f, (0f - num3) * 0.5f);
		Vector3 vector8 = new Vector3((0f - num) * 0.5f, num2 * 0.5f, (0f - num3) * 0.5f);
		Vector3[] vertices = new Vector3[24]
		{
			vector, vector2, vector3, vector4, vector8, vector5, vector, vector4, vector5, vector6,
			vector2, vector, vector7, vector8, vector4, vector3, vector6, vector7, vector3, vector2,
			vector8, vector7, vector6, vector5
		};
		Vector3 up = Vector3.up;
		Vector3 down = Vector3.down;
		Vector3 forward = Vector3.forward;
		Vector3 back = Vector3.back;
		Vector3 left = Vector3.left;
		Vector3 right = Vector3.right;
		Vector3[] normals = new Vector3[24]
		{
			down, down, down, down, left, left, left, left, forward, forward,
			forward, forward, back, back, back, back, right, right, right, right,
			up, up, up, up
		};
		_uvs = new Vector2[24];
		RecalculateUVs(_uvs);
		int[] triangles = new int[36]
		{
			3, 1, 0, 3, 2, 1, 7, 5, 4, 7,
			6, 5, 11, 9, 8, 11, 10, 9, 15, 13,
			12, 15, 14, 13, 19, 17, 16, 19, 18, 17,
			23, 21, 20, 23, 22, 21
		};
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = _uvs;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		return mesh;
	}

	private void RecalculateUVs(Vector2[] uvs)
	{
		Vector3 lossyScale = transform.lossyScale;
		Vector2[] array = new Vector2[6]
		{
			new Vector2(lossyScale.x, lossyScale.z),
			new Vector2(lossyScale.z, lossyScale.y),
			new Vector2(lossyScale.x, lossyScale.y),
			new Vector2(lossyScale.x, lossyScale.y),
			new Vector2(lossyScale.z, lossyScale.y),
			new Vector2(lossyScale.x, lossyScale.z)
		};
		for (int i = 0; i < array.Length; i++)
		{
			ref Vector2 reference = ref uvs[i * 4];
			reference = array[i];
			ref Vector2 reference2 = ref uvs[i * 4 + 1];
			reference2 = new Vector2(0f, array[i].y);
			ref Vector2 reference3 = ref uvs[i * 4 + 2];
			reference3 = Vector2.zero;
			ref Vector2 reference4 = ref uvs[i * 4 + 3];
			reference4 = new Vector2(array[i].x, 0f);
		}
	}

	public void RefreshUVs()
	{
		RecalculateUVs(_uvs);
		_mesh.uv = _uvs;
	}

	public Bounds GetBounds()
	{
		return _meshRenderer.bounds;
	}
}
public class ClassicObstacleGeometry : MonoBehaviour
{
	[SerializeField]
	private ClassicObstacleCube _stretchableCoreOutside;

	[SerializeField]
	private ClassicObstacleCube _stretchableCoreInside;

	[SerializeField]
	private Transform _stretchableCore;

	[SerializeField]
	private Transform _stretchableFrontTop;

	[SerializeField]
	private Transform _stretchableFrontBottom;

	[SerializeField]
	private Transform _stretchableFrontLeft;

	[SerializeField]
	private Transform _stretchableFrontRight;

	[SerializeField]
	private Transform _stretchableBackTop;

	[SerializeField]
	private Transform _stretchableBackBottom;

	[SerializeField]
	private Transform _stretchableBackLeft;

	[SerializeField]
	private Transform _stretchableBackRight;

	[SerializeField]
	private Transform _stretchableMiddleTopLeft;

	[SerializeField]
	private Transform _stretchableMiddleTopRight;

	[SerializeField]
	private Transform _stretchableMiddleBottomLeft;

	[SerializeField]
	private Transform _stretchableMiddleBottomRight;

	[SerializeField]
	private Transform _cornerFrontTopLeft;

	[SerializeField]
	private Transform _cornerFrontTopRight;

	[SerializeField]
	private Transform _cornerFrontBottomLeft;

	[SerializeField]
	private Transform _cornerFrontBottomRight;

	[SerializeField]
	private Transform _cornerBackTopLeft;

	[SerializeField]
	private Transform _cornerBackTopRight;

	[SerializeField]
	private Transform _cornerBackBottomLeft;

	[SerializeField]
	private Transform _cornerBackBottomRight;

	private Bounds _bounds;

	public Bounds bounds => _bounds;

	public void SetSize(float width, float height, float length)
	{
		_bounds = new Bounds(new Vector3(0f, height * 0.5f, length * 0.5f), new Vector3(width, height, length));
		_stretchableCore.localScale = new Vector3(width, height, length);
		_stretchableCore.localPosition = new Vector3(0f, height * 0.5f, length * 0.5f);
		_stretchableCoreOutside.RefreshUVs();
		_stretchableCoreInside.RefreshUVs();
		Vector3 localScale = _stretchableFrontTop.localScale;
		localScale.x = width;
		_stretchableFrontTop.localScale = localScale;
		_stretchableFrontTop.localPosition = new Vector3(0f, height, 0f);
		_stretchableFrontBottom.localScale = localScale;
		_stretchableFrontBottom.localPosition = new Vector3(0f, 0f, 0f);
		_stretchableBackTop.localScale = localScale;
		_stretchableBackTop.localPosition = new Vector3(0f, height, length);
		_stretchableBackBottom.localScale = localScale;
		_stretchableBackBottom.localPosition = new Vector3(0f, 0f, length);
		localScale = _stretchableFrontLeft.localScale;
		localScale.y = height;
		_stretchableFrontLeft.localScale = localScale;
		_stretchableFrontLeft.localPosition = new Vector3((0f - width) * 0.5f, height * 0.5f, 0f);
		_stretchableFrontRight.localScale = localScale;
		_stretchableFrontRight.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f);
		_stretchableBackLeft.localScale = localScale;
		_stretchableBackLeft.localPosition = new Vector3((0f - width) * 0.5f, height * 0.5f, length);
		_stretchableBackRight.localScale = localScale;
		_stretchableBackRight.localPosition = new Vector3(width * 0.5f, height * 0.5f, length);
		localScale = _stretchableMiddleBottomLeft.localScale;
		localScale.z = length;
		_stretchableMiddleTopLeft.localScale = localScale;
		_stretchableMiddleTopLeft.localPosition = new Vector3((0f - width) * 0.5f, height, length * 0.5f);
		_stretchableMiddleTopRight.localScale = localScale;
		_stretchableMiddleTopRight.localPosition = new Vector3(width * 0.5f, height, length * 0.5f);
		_stretchableMiddleBottomLeft.localScale = localScale;
		_stretchableMiddleBottomLeft.localPosition = new Vector3((0f - width) * 0.5f, 0f, length * 0.5f);
		_stretchableMiddleBottomRight.localScale = localScale;
		_stretchableMiddleBottomRight.localPosition = new Vector3(width * 0.5f, 0f, length * 0.5f);
		_cornerFrontTopLeft.localPosition = new Vector3((0f - width) * 0.5f, height, 0f);
		_cornerFrontTopRight.localPosition = new Vector3(width * 0.5f, height, 0f);
		_cornerFrontBottomLeft.localPosition = new Vector3((0f - width) * 0.5f, 0f, 0f);
		_cornerFrontBottomRight.localPosition = new Vector3(width * 0.5f, 0f, 0f);
		_cornerBackTopLeft.localPosition = new Vector3((0f - width) * 0.5f, height, length);
		_cornerBackTopRight.localPosition = new Vector3(width * 0.5f, height, length);
		_cornerBackBottomLeft.localPosition = new Vector3((0f - width) * 0.5f, 0f, length);
		_cornerBackBottomRight.localPosition = new Vector3(width * 0.5f, 0f, length);
	}
}

}
