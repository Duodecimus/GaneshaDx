using System;
using System.Collections.Generic;
using System.Numerics;

namespace GaneshaDx.Common {
	public class TerrainReference {

		/// <summary>
		/// Takes a list of 2 to 4 points, and returns a translation, rotation, and scale to bring those points to the location, orientation and size of the terrain grid
		/// </summary>
		/// <param name="points"></param>
		/// <returns></returns>
		public static (Vector3 translation, Matrix4x4 rotationMatrix, float scaleFactor) GetTerrainReferenceTransformation(List<Vector3> points) {

			Vector3 translation;
			Matrix4x4 rotationMatrix;
			float scaleFactor;
			if (points.Count == 2) { //Untested
				//Not sure if possible to do in GLBs, I couldn't get blender to export a GLB with an object of only a line
				Vector3 p0;
				Vector3 p1;
				//the point closest to 0,0,0 is chosen as the translation
				if (Vector3.Distance(points[0], Vector3.Zero) >=
					Vector3.Distance(points[1], Vector3.Zero)) {
					p1 = points[0];
					p0 = points[1];
				}
				else {
					p0 = points[0];
					p1 = points[1];
				}
				translation = new Vector3(-p0.X, 0, -p0.Z); // Translate only in X and Z
				p0 += translation; // p0 will be at (0, p0.Y, 0)
				p1 += translation; // Translate p1 to align with p0's new X and Z
								   // Step 2: Scale the line so that the distance between the points is 28

				float currentDistance = Vector3.Distance(p0, p1);
				scaleFactor = 28f / currentDistance;
				p0 *= scaleFactor;
				p1 *= scaleFactor;

				// Rotate p1 to align it with (28, p0.Y, 0)
				// We need to compute the rotation that aligns p1 with (28, p0.Y, 0)
				Vector3 target = new(28, p0.Y, 0); // Target position for p1
				Vector3 p1Direction = Vector3.Normalize(p1 - p0); // Direction of the line from p0 to p1
				Vector3 targetDirection = Vector3.Normalize(target - p0); // Direction of the target line

				// Compute the rotation axis (cross product)
				Vector3 rotationAxis = Vector3.Cross(p1Direction, targetDirection);
				rotationAxis = Vector3.Normalize(rotationAxis);

				// Compute the angle of rotation
				float angle = (float)Math.Acos(Vector3.Dot(p1Direction, targetDirection));

				// Create the rotation matrix using axis-angle representation
				rotationMatrix = Matrix4x4.CreateFromAxisAngle(rotationAxis, angle);

				p0 = Vector3.Transform(p0, rotationMatrix);
				p1 = Vector3.Transform(p1, rotationMatrix);

				if (float.IsNaN(p0.X) || float.IsNaN(p0.Y) || float.IsNaN(p0.Z) ||
					float.IsNaN(p1.X) || float.IsNaN(p1.Y) || float.IsNaN(p1.Z)) {
					OverlayConsole.AddMessage($"Failed to apply terrain reference, transform produced a NaN");
					return (Vector3.Zero, Matrix4x4.Identity, 1);
				}
				return (translation, rotationMatrix, scaleFactor);
			}
			else {
				// if given a quad or a tri
				Vector3 point0;    //point closest to origin
								   //Vector3 pointX;  //point on the X axis, X should be positive
								   //Vector3 pointZ;  //point on the Z axis, Z should be positive
								   //Vector3 pointXZ; //the far corner of the quad, discard it

				// Remove the farthest point if there are 4 points (i.e., a square)
				if (points.Count == 4) {
					// Find the point farthest from the origin
					Vector3 origin = Vector3.Zero;
					Vector3 farthestPoint = points[0];
					float maxDistance = Vector3.Distance(farthestPoint, origin);

					foreach (var point in points) {
						float dist = Vector3.Distance(point, origin);
						if (dist > maxDistance) {
							maxDistance = dist;
							farthestPoint = point;
						}
					}

					// Remove the farthest point
					points.Remove(farthestPoint);
				}

				try {
					point0 = FindRightAngle(points);
					points.Remove(point0);

				}
				catch (Exception) {
					OverlayConsole.AddMessage($"Failed to apply terrain reference, Shape not a line or a right angle");
					return (Vector3.Zero, Matrix4x4.Identity, 1);
				}

				// Translate the triangle so the right-angle vertex is at (0, point0.Y, 0)
				translation = new Vector3(-point0.X, 0, -point0.Z); // Translate only in X and Z, keeping Y the same
				point0 += translation;
				var point1 = points[0] + translation;
				var point2 = points[1] + translation;

				//identify which arm will end up on which axis
				var (pointX, pointZ) = GetTriangleArms(point0, point1, point2);

				// Scale the triangle so the arms are at (28, point0.Y, 0) and (0, point0.Y, 28)
				scaleFactor = 28f / Vector3.Distance(point0, pointX); //just scale everything on X, we're never going to see a non-square reference shape.

				pointX *= scaleFactor;
				pointZ *= scaleFactor;

				// Rotate the triangle to align the arms along the X and Z axes
				rotationMatrix = GetRotationMatrixForPositiveQuadrant(point0, pointX, pointZ);

				point0 = Vector3.Transform(point0, rotationMatrix);
				pointX = Vector3.Transform(pointX, rotationMatrix);
				pointZ = Vector3.Transform(pointZ, rotationMatrix);

				// Compute the rotation needed to align pointX with targetPointx and pointZ with targetPointZ
				// The target positions are (28, p0.Y, 0) for pointX and (0, p0.Y, 28) for pointZ.
				// This rotation step will mainly take care of a slanted reference shape
				Vector3 targetPointX = new(28, point0.Y, 0);
				Vector3 targetPointZ = new(0, point0.Y, 28);

				// Align pointX to (28, p0.Y, 0)
				Vector3 directionPointX = pointX - point0;
				Vector3 directionTargetPointX = targetPointX - point0;
				Matrix4x4 rotationMatrixPointX = GetRotationMatrix(directionPointX, directionTargetPointX);

				// Align pointZ to (0, p0.Y, 28)
				Vector3 directionPointZ = pointZ - point0;
				Vector3 directionTargetPointZ = targetPointZ - point0;
				Matrix4x4 rotationMatrixPointZ = GetRotationMatrix(directionPointZ, directionTargetPointZ);

				point0 = Vector3.Transform(point0, rotationMatrixPointX * rotationMatrixPointZ);
				pointX = Vector3.Transform(pointX, rotationMatrixPointX * rotationMatrixPointZ);
				pointZ = Vector3.Transform(pointZ, rotationMatrixPointX * rotationMatrixPointZ);

				// Combine rotations (first rotate pointX, then pointZ)
				rotationMatrix = rotationMatrix * rotationMatrixPointX * rotationMatrixPointZ;


				// For some reason the model is rotated an additional 90 degrees when it gets rendered. This is thus a quick fix to make it render right
				//TODO: Figure out why it does this and if there is a better fix
				var bandaidRotation = Matrix4x4.CreateRotationY(-90f * (MathF.PI / 180f), Vector3.Zero);

				point0 = Vector3.Transform(point0, bandaidRotation);
				pointX = Vector3.Transform(pointX, bandaidRotation);
				pointZ = Vector3.Transform(pointZ, bandaidRotation);

				if (float.IsNaN(point0.X) || float.IsNaN(point0.Y) || float.IsNaN(point0.Z) ||
					float.IsNaN(pointX.X) || float.IsNaN(pointX.Y) || float.IsNaN(pointX.Z) ||
					float.IsNaN(pointZ.X) || float.IsNaN(pointZ.Y) || float.IsNaN(pointZ.Z)) {
					OverlayConsole.AddMessage($"Failed to apply terrain reference, transform produced a NaN");
					return (Vector3.Zero, Matrix4x4.Identity, 1);
				}

				rotationMatrix *= bandaidRotation;
				return (translation, rotationMatrix, scaleFactor);
			}
		}

		/// <summary>
		/// Function to find the right-angle vertex in the triangle
		/// </summary>
		/// <param name="points"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		private static Vector3 FindRightAngle(List<Vector3> points) {
			if (points.Count != 3) throw new ArgumentException("There must be exactly 3 points to define a triangle.");

			// Check each vertex to determine which one forms a right angle by checking dot product
			Vector3 a = points[0], b = points[1], c = points[2];

			Vector3 ab = b - a;
			Vector3 ac = c - a;
			Vector3 bc = c - b;

			if (Vector3.Dot(ab, ac) == 0) return a; // Right angle at a
			if (Vector3.Dot(-ab, bc) == 0) return b; // Right angle at b
			if (Vector3.Dot(-ac, -bc) == 0) return c; // Right angle at c

			throw new InvalidOperationException("No right angle found.");
		}

		/// <summary>
		/// Function to identify which arm of the trinagle should be on X, and which on Z
		/// </summary>
		/// <param name="point0"></param>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <returns></returns>
		public static (Vector3 PointX, Vector3 PointZ) GetTriangleArms(Vector3 point0, Vector3 point1, Vector3 point2) {
			// Ensure the vectors are on the XZ plane by setting their Y components to zero
			point0.Y = 0;
			point1.Y = 0;
			point2.Y = 0;

			// Compute vectors from point0 to point1 and point0 to point2
			Vector3 v01 = point1 - point0;
			Vector3 v02 = point2 - point0;

			// Compute the 2D cross product (ignoring the Y component as it's 0)
			float crossProduct = v01.X * v02.Z - v01.Z * v02.X;

			// If the cross product is positive, the triangle turns counterclockwise (pointZ)
			if (crossProduct > 0) {
				return (point1, point2); // point1 is counter-clockwise of the right angle, point2 is clockwise
			}
			else {
				return (point2, point1); // point2 is counter-clockwise of the right angle
			}
		}

		/// <summary>
		/// Function to detect which quadrant a right angle triangle spans and rotate accordingly
		/// </summary>
		/// <param name="point0">Right angle of the triangle</param>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <returns></returns>
		private static Matrix4x4 GetRotationMatrixForPositiveQuadrant(Vector3 point0, Vector3 point1, Vector3 point2) {
			// point0 is the corner of the right angle
			// Define the two arms of the triangle
			Vector3 arm1 = point1 - point0; // arm from right-angle point to point1
			Vector3 arm2 = point2 - point0; // arm from right-angle point to point2
			arm1.Y = 0;
			arm2.Y = 0;

			// Normalize the arms
			arm1 = Vector3.Normalize(arm1);
			arm2 = Vector3.Normalize(arm2);

			// Calculate the angle between arm1 and the positive X axis
			float angleToX1 = MathF.Atan2(arm1.Z, arm1.X); // Calculate the angle of rotation around Y to align arm1 with X axis
			float angleToX2 = MathF.Atan2(arm2.Z, arm2.X); // Calculate the angle of rotation around Y to align arm2 with X axis

			//based on the magnitude of the angles, determine which arm will end up parallel to X
			//with +x +z  angleToX1 = 0			angleToX2 =  pi/2	correct rotation is angleToX1 (matrix 1)
			//with +x -z  angleToX1 = 0			angleToX2 = -pi/2	correct rotation is angleToX2 (matrix 2)
			//with -x +z  angleToX1 =  pi/2		angleToX2 =  pi		correct rotation is angleToX1 (matrix 1)
			//with -x -z  angleToX1 = -pi/2		angleToX2 =  pi		correct rotation is angleToX2 (matrix 2)

			// Create the rotation matrix around the Y axis
			Matrix4x4 rotationMatrix1 = Matrix4x4.CreateRotationY(angleToX1, Vector3.Zero);
			Matrix4x4 rotationMatrix2 = Matrix4x4.CreateRotationY(angleToX2, Vector3.Zero);

			// Apply the rotation matrix to the arms and check if it places them along the positive X and Z axes
			var rotatedArm1 = Vector3.Transform(arm1, rotationMatrix1);
			var rotatedArm2 = Vector3.Transform(arm2, rotationMatrix1);
			//var rotatedArmA = Vector3.Transform(arm1, rotationMatrix2);
			//var rotatedArmB = Vector3.Transform(arm2, rotationMatrix2);

			//float nonsense often sets one of these to some tiny fraction like 2e-8, flatten them
			rotatedArm1 = new(MathF.Round(rotatedArm1.X), MathF.Round(rotatedArm1.Y), MathF.Round(rotatedArm1.Z));
			rotatedArm2 = new(MathF.Round(rotatedArm2.X), MathF.Round(rotatedArm2.Y), MathF.Round(rotatedArm2.Z));

			// If arm1 and arm2 are aligned correctly with positive X and Z, the rotation is valid.
			// Return the rotation matrix that performs this transformation.
			if ((rotatedArm1 == new Vector3(1, 0, 0) && rotatedArm2 == new Vector3(0, 0, 1)) ||
				(rotatedArm1 == new Vector3(0, 0, 1) && rotatedArm2 == new Vector3(1, 0, 0))) {
				return rotationMatrix1;
			}
			else {
				return rotationMatrix2;
			}
		}

		/// <summary>
		/// Helper function to calculate a rotation matrix to align two vectors
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		private static Matrix4x4 GetRotationMatrix(Vector3 from, Vector3 to) {
			from = Vector3.Normalize(from);
			to = Vector3.Normalize(to);

			float cosTheta = Vector3.Dot(from, to);
			Vector3 rotationAxis = Vector3.Cross(from, to);

			if (rotationAxis.Length() < 0.001f) {
				return Matrix4x4.Identity; // No rotation needed if vectors are already aligned
			}

			rotationAxis = Vector3.Normalize(rotationAxis);
			float angle = (float)Math.Acos(cosTheta);
			return Matrix4x4.CreateFromAxisAngle(rotationAxis, angle);
		}
	}
}
