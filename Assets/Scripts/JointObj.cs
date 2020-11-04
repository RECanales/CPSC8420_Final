using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointObj : MonoBehaviour
{
	// this class is used to compute the transformation for a joint (without Unity)
	public Quaternion rotation;
	public Vector3 position;

	[SerializeField]
	List<JointObj> children = new List<JointObj>();

	Matrix4x4 thisMatrix = Matrix4x4.identity;
	Matrix4x4 translation = Matrix4x4.identity;
	Matrix4x4 rotationX = Matrix4x4.identity;
	Matrix4x4 rotationY = Matrix4x4.identity;
	Matrix4x4 rotationZ = Matrix4x4.identity;
	Matrix4x4 rotationMult = Matrix4x4.identity;
	Quaternion q_rotation = Quaternion.identity;
	Matrix4x4 boneTranslation = Matrix4x4.identity;
	public Transform boneRef;
	float boneLength = 0.0f; // dist between child (if any) and this joint

	private void Start()
	{
	}

	public void AddChild(JointObj j)
	{
		children.Add(j);
	}

	public void UpdateSkeleton(Matrix4x4 parentMat)
	{
		Vector4 col = parentMat.GetColumn(3);
		boneTranslation.SetColumn(3, new Vector4(0, boneLength + col.y, 0, 1));
		thisMatrix = boneTranslation * thisMatrix;  // translate up
		UpdateTransform();
	}

	public void SetBoneLength(float b)
	{
		boneLength = b;
	}

	public JointObj GetChild(int index)
	{
		return (index < children.Count) ? children[index] : null;
	}

	public void RotateJoint(Vector3 _rotation)
	{
		// Get rotation matrices for euler angles
		rotationX = RotateX(Mathf.Deg2Rad * _rotation.x);
		rotationY = RotateY(Mathf.Deg2Rad * _rotation.y);
		rotationZ = RotateZ(Mathf.Deg2Rad * _rotation.z);

		// Concatenate the rotations
		rotationMult = rotationZ * rotationY * rotationX;
	}

	public void TranslateJoint(Vector3 _translation)
	{
		translation.SetColumn(3, new Vector4(_translation.x, _translation.y, _translation.z, 1));
	}

	public void SetMatrix(Matrix4x4 M)
	{
		thisMatrix = M;
	}

	public Vector3 GetPosition()
	{
		return thisMatrix.GetColumn(3);
	}

	public Matrix4x4 GetMatrix()
	{
		return thisMatrix;
	}

	public Matrix4x4 GetRotation()
	{
		return rotationMult;
	}

	public Matrix4x4 GetTranslation()
	{
		return translation;
	}

	public Matrix4x4 GetArticulation()
	{
		return boneTranslation;
	}

	public void ResetJoint()
	{
		thisMatrix = thisMatrix.inverse * thisMatrix;
		translation = Matrix4x4.identity;
		rotationMult = Matrix4x4.identity;
	}

	public void UpdateTransform()
	{
		// translate gameobject by extracting translation from matrix
		gameObject.transform.position = thisMatrix.GetColumn(3);

		// rotate gameobject by extracting rotations from matrix then converting to quaternion
		transform.rotation = QuaternionFromMatrix(thisMatrix);

		// Reset translation & rotation matrices
		rotationMult = Matrix4x4.identity;
		translation = Matrix4x4.identity;
	}

	Quaternion QuaternionFromMatrix(Matrix4x4 m)
	{
		return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
		/*
		// Found on Unity Forum: https://answers.unity.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html
		Quaternion q = new Quaternion();
		q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
		q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
		q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
		q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
		q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
		q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
		q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
		return q;*/
	}

	Matrix4x4 RotateX(float angle)
	{
		Vector4 r1 = new Vector4(1, 0, 0, 0);
		Vector4 r2 = new Vector4(0, Mathf.Cos(angle), Mathf.Sin(angle), 0);
		Vector4 r3 = new Vector4(0, -Mathf.Sin(angle), Mathf.Cos(angle), 0);
		Vector4 r4 = new Vector4(0, 0, 0, 1);
		return new Matrix4x4(r1, r2, r3, r4).transpose;
	}

	Matrix4x4 RotateY(float angle)
	{
		Vector4 r1 = new Vector4(Mathf.Cos(angle), 0, -Mathf.Sin(angle), 0);
		Vector4 r2 = new Vector4(0, 1, 0, 0);
		Vector4 r3 = new Vector4(Mathf.Sin(angle), 0, Mathf.Cos(angle), 0);
		Vector4 r4 = new Vector4(0, 0, 0, 1);
		return new Matrix4x4(r1, r2, r3, r4).transpose;
	}

	Matrix4x4 RotateZ(float angle)
	{
		Vector4 r1 = new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), 0, 0);
		Vector4 r2 = new Vector4(-Mathf.Sin(angle), Mathf.Cos(angle), 0, 0);
		Vector4 r3 = new Vector4(0, 0, 1, 0);
		Vector4 r4 = new Vector4(0, 0, 0, 1);
		return new Matrix4x4(r1, r2, r3, r4).transpose;
	}
}
