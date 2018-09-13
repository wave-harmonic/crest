using UnityEngine;

//To save space so we don't have to send millions of parameters to each method
public struct TriangleData
{
    //The corners of this triangle in global coordinates
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;

    //The center of the triangle
    public Vector3 center;

    //The distance to the surface from the center of the triangle
    public float distanceToSurface;

    //The normal to the triangle
    public Vector3 normal;

    //The area of the triangle
    public float area;

    //The velocity of the triangle at the center
    public Vector3 velocity;

    //The velocity normalized
    public Vector3 velocityDir;

    //The angle between the normal and the velocity
    //Negative if pointing in the opposite direction
    //Positive if pointing in the same direction
    public float cosTheta;

    public TriangleData(Vector3 p1, Vector3 p2, Vector3 p3, Rigidbody boatRB, float timeSinceStart)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;

        //Center of the triangle
        this.center = (p1 + p2 + p3) / 3f;

        //Distance to the surface from the center of the triangle
        float waveYPos = 0f;
        Crest.OceanRenderer.Instance.CollisionProvider.SampleHeight(ref center, ref waveYPos);
        distanceToSurface = waveYPos - center.y;

        //Normal to the triangle
        this.normal = Vector3.Cross(p2 - p1, p3 - p1).normalized;

        //Area of the triangle
        this.area = BoatPhysicsMath.GetTriangleArea(p1, p2, p3);

        //Velocity vector of the triangle at the center
        this.velocity = BoatPhysicsMath.GetTriangleVelocity(boatRB, this.center);

        //Velocity direction
        this.velocityDir = this.velocity.normalized;

        //Angle between the normal and the velocity
        //Negative if pointing in the opposite direction
        //Positive if pointing in the same direction
        this.cosTheta = Vector3.Dot(this.velocityDir, this.normal);
    }
}
