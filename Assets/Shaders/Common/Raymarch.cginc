//-------------------------------------------------------------------------------------
//                              DISTANCE FIELD FUNCTIONS
//-------------------------------------------------------------------------------------

// Torus
// t.x: diameter
// t.y: thickness
float dfTorus (float3 p, float2 t) {
	float2 q = float2(length(p.xz) - t.x, p.y);
	return length(q) - t.y;
}

// Box
// b: size of box in x/y/z
float dfBox (float3 p, float3 b) {
	float3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}

// Sphere
float dfSphere(float3 p, float s) {
	return length(p) - s;
}

// Round box
float dfRoundBox(float3 p, float3 b, float r) {
	return length(max(abs(p) - b, 0.0)) - r;
}

//-------------------------------------------------------------------------------------
//                                 OBJECTS COMBINATION
//-------------------------------------------------------------------------------------

// Union (with material data)
float2 dfUnion(float2 d1, float2 d2) {
	return (d1.x < d2.x) ? d1 : d2;
}

// Substraction
float dfSubstract(float d1, float d2) {
	return max(-d1, d2);
}

// Intersection
float dfIntersect(float d1, float d2) {
	return max(d1, d2);
}

//-------------------------------------------------------------------------------------
//                                 COMMON UTILITIES
//-------------------------------------------------------------------------------------

// This is the distance field function. The distance field
// represents the closest distance to the surface of any
// object we put in the scene. If the given point (point p)
// is inside of an object, we return a negative answer.
// \return.x : Distance field value
// \return.y : Color of closest object (0 - 1)
float2 map (float3 p) {	
	float2 d_torus = float2(dfTorus(p, float2(1, 0.2)), 0.5);
	float2 d_box = float2(dfBox(p - float3(-3, 0, 0), float3(0.75, 0.5, 0.5)), 0.25);
	float2 d_r_box = float2(dfRoundBox(p - float3(-3, -1.3, 0), float3(0.75, 0.3, 0.2), 0.3), 0.83);
	float2 d_sph = float2(dfSphere(p - float3(-3, 0, 0), 0.6), 0);
	float2 d_sphere = float2(dfSphere(p - float3(3, 0, 0), 1), 0.75);

	float2 ret = dfUnion(d_torus, d_box);
	ret = dfUnion(ret, d_r_box);
	ret = dfUnion(ret, d_sph);
	ret = dfUnion(ret, d_sphere);
	return ret;
}

// TODO: too expensive technique!!!
float3 calcNormal(in float3 pos) {
	// Epsilon - used to approximate dx when taking the derivative
	const float2 eps = float2(0.001, 0.0);

	// The idea here is to find the "gradient" of the distance field at pos
	// Essentially we are approximating the derivative of the distance field
	// at this point.
	float3 norm = float3(
		map(pos + eps.xyy).x - map(pos - eps.xyy).x,
		map(pos + eps.yxy).x - map(pos - eps.yxy).x,
		map(pos + eps.yyx).x - map(pos - eps.yyx).x
	);
	return normalize(norm);
}

// Raymarch along the given ray
// ro: ray origin
// rd: ray direction
fixed4 raymarch(float3 ro, float3 rd, float depthBuffer) {
	fixed4 ret = fixed4(0, 0, 0, 0);
	const int maxStep = 64;
	const float tolerance = 0.001;
	// Draw distance in unity units
	const float drawDistance = 40;
	// Current distance travelled along ray
	float t = 0;
	for (int i = 0; i < maxStep; ++i) {
		// If we run past the depth buffer, stop and return nothing
		// (transparent pixel). This way raymarched objects and meshes
		// can coexist!
		if (t >= depthBuffer || t > drawDistance) {
			ret = fixed4(0, 0, 0, 0);
			break;
		}
		// World space position of sample
		float3 p = ro + rd * t;
		// Sample of distance field (see map())
		float2 d = map(p);

		// If the sample <= 0 we have hit something (see map())
		if (d.x < tolerance) {
			// Lambertian Lighting
			float3 norm = calcNormal(p);
			float light = dot(-_LightDir.xyz, norm);

			// tex2D don't work in loops (Unity 2017.1.0b6)
			// Compiler insists on loop unrolling and we have incorrect result.
			// So we use tex2Dlod instead
			ret = fixed4(tex2Dlod(_ColorRamp, float4(d.y, 0, 0, 0)).xyz * light, 1);
			break;
		}

		// If the sample > 0, we haven't hit anything yet so we should
		// march forward. We step forward by distance d, because d is the
		// minimum distance possible to intersect an object (see map())
		t += d;
	}
	return ret;
}

fixed4 raymarchPerfTest(float3 ro, float3 rd, float s) {
	const int maxstep = 64;
	// Draw distance in unity units
	const float drawdist = 40;
	float t = 0;
	for (int i = 0; i < maxstep; i++) {
		float3 p = ro + rd * t;
		float2 d = map(p);
		if (d.x < 0.001 || t > drawdist) {
			float perf = (float)i / maxstep;
			return fixed4(tex2Dlod(_PerfRamp, float4(perf, 0, 0, 0)).xyz, 1);
		}
		t += d;
	}
	return fixed4(tex2Dlod(_PerfRamp, float4(1, 0, 0, 0)).xyz, 1);
}