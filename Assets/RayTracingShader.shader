Shader"RayTracingShader"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct Interpolator
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(Interpolator v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 ViewParams;
            float4x4 CameraRotation;
            uint MaxBounces;

            float RandomValue(inout uint rngState)
            {
                rngState = rngState * 747796405 + 2891336453;
                uint result = ((rngState >> ((rngState >> 28) + 4)) ^ rngState) * 277803737;
                result = (result >> 22) ^ result;
                return result / 4294967295.0;
            }

            float RandomValueNormalDistribution(inout uint rngState)
            {
                float theta = 2 * 3.14159265359 * RandomValue(rngState);
                float rho = sqrt(-2 * log(RandomValue(rngState)));
                return rho * cos(theta);
            }

            float3 RandomDirection(inout uint rngState)
            {
                float x = RandomValueNormalDistribution(rngState);
                    float y = RandomValueNormalDistribution(rngState);
                    float z = RandomValueNormalDistribution(rngState);
                    return normalize(float3(x,y,z));
            }

            struct RayTracingMaterial
            {
                float4 color;
            };
            
            struct Ray
            {
                float3 origin;
                float3 direction;
            };

            struct HitInfo
            {
                bool didHit;
                float3 hitPoint, normal;
                float distance;
                RayTracingMaterial material;
            };

            struct Triangle
            {
                float3 p1, p2, p3;
                float n1, n2, n3;
            };

            struct MeshObject
            {
                float4x4 LocalToWorldMatrix;
                int FirstIndex;
                int IndexCount;
                float3 BoundsMin;
                float3 BoundsMax;
                //RayTracingMaterial material;
            };

            bool RayBoundingBox(Ray ray, float3 boundsMin, float3 boundsMax)
            {
                float3 boundsCenter = (boundsMin + boundsMax) / 2;
                float3 tmin;
                float3 tmax;
                
                float3 invDir = 1 / ray.direction;
                int3 sign;
                sign.x = invDir.x >= 0 ? 0 : 1;
                sign.y = invDir.y >= 0 ? 0 : 1;
                sign.z = invDir.z >= 0 ? 0 : 1;

                float2x3 bounds = float2x3(boundsMin, boundsMax);
                tmin.x = (bounds[sign.x].x - ray.origin.x) * invDir.x;
                tmax.x = (bounds[1 - sign.x].x - ray.origin.x) * invDir.x;
                tmin.y = (bounds[sign.y].y - ray.origin.y) * invDir.y;
                tmax.y = (bounds[1 - sign.y].y - ray.origin.y) * invDir.y;

                if ((tmin.x > tmax.y) || (tmin.y > tmax.x))
                    return false;
                
                if (tmin.y > tmin.x)
                    tmin.x = tmin.y;
                if (tmax.y < tmax.x)
                    tmax.x = tmax.y;

                tmin.z = (bounds[sign.z].z - ray.origin.z) * invDir.z;
                tmax.z = (bounds[1 - sign.z].z - ray.origin.z) * invDir.z;

                if ((tmin.x > tmax.z) || (tmin.z > tmax.x))
                    return false;

                /*if (tmin.z > tmin.x)
                    tmin.x = tmin.z;
                if (tmax.z < tmax.x)
                    tmax.x = tmax.z;*/
                
                return true; 
            }
            
            HitInfo RayTriangle(Ray ray, Triangle tri)
            {
                float3 edgeAB = tri.p2 - tri.p1;
                float3 edgeAC = tri.p3 - tri.p1;
                float3 normal = (cross(edgeAB, edgeAC));
                float3 avg = (tri.p1 + tri.p2 + tri.p3) / 3;
                float3 ao = ray.origin - avg;
                float3 dao = cross(ao, ray.direction);

                float determinant = -dot(ray.direction, normal);
                float invDeterminant = 1 / determinant;

                float dst = dot(ao, normal) * invDeterminant;
                float u = dot(edgeAC, dao) * invDeterminant;
                float v = -dot(edgeAB, dao) * invDeterminant;
                float w = 1 - u - v;

                HitInfo hitInfo = (HitInfo)0;
                hitInfo.didHit = determinant >= 1E-6 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
                hitInfo.hitPoint = ray.origin + ray.direction * dst;
                hitInfo.normal = normalize(tri.n1 * w + tri.n2 * u + tri.n3 * v);
                hitInfo.distance = dst;
                return hitInfo;
            }

            HitInfo RaySphere(Ray ray, float3 sphereCenter, float sphereRadius)
            {
                HitInfo hitInfo = (HitInfo)0;
                float3 offsetRayOrigin = ray.origin - sphereCenter;
                float a = 1;
                float b = 2 * dot(offsetRayOrigin, ray.direction);
                float c = dot(offsetRayOrigin, offsetRayOrigin) - sphereRadius * sphereRadius;

                float discriminant = b * b - 4 * a * c;

                if(discriminant >= 0)
                {
                    float dst = (-b - sqrt(discriminant)) / (2 * a);

                    if(dst >= 0)
                    {
                        hitInfo.didHit = true;
                        hitInfo.distance = dst;
                        hitInfo.hitPoint = ray.origin + ray.direction * dst;
                        hitInfo.normal = normalize(hitInfo.hitPoint - sphereCenter);
                    }
                }

                return hitInfo;
            }

            StructuredBuffer<MeshObject> _meshes;
            StructuredBuffer<float3> _vertices;
            StructuredBuffer<float3> _normals;
            StructuredBuffer<int> _indices;

            HitInfo RayMesh(Ray ray, MeshObject mesh)
            {
                HitInfo closestHit = (HitInfo)0;
                closestHit.distance = 1.#INF;
                
                if(!RayBoundingBox(ray, mesh.BoundsMin, mesh.BoundsMax))
                    return closestHit;
                
                //closestHit.didHit = true;
                //return closestHit;
                
                uint offset = mesh.FirstIndex;
                uint count = offset + mesh.IndexCount;

                Triangle tri;
                for (uint i = offset; i < count; i += 3)
                {
                    tri.p1 = (mul(mesh.LocalToWorldMatrix, float4(_vertices[_indices[i]], 1))).xyz;
                    tri.p2 = (mul(mesh.LocalToWorldMatrix, float4(_vertices[_indices[i + 1]], 1))).xyz;
                    tri.p3 = (mul(mesh.LocalToWorldMatrix, float4(_vertices[_indices[i + 2]], 1))).xyz;

                    tri.n1 = (mul(mesh.LocalToWorldMatrix, float4(_normals[_indices[i]], 1))).xyz;
                    tri.n2 = (mul(mesh.LocalToWorldMatrix, float4(_normals[_indices[i + 1]], 1))).xyz;
                    tri.n3 = (mul(mesh.LocalToWorldMatrix, float4(_normals[_indices[i + 2]], 1))).xyz;

                    HitInfo hitInfo = RayTriangle(ray, tri);

                    if (hitInfo.didHit)
                    {
                        if (hitInfo.distance > 0 && hitInfo.distance < closestHit.distance)
                        {
                            closestHit.distance = hitInfo.distance;
                            closestHit.hitPoint = ray.origin + hitInfo.distance * ray.direction;
                            closestHit.normal = hitInfo.normal;
                        }
                    }
                }
                return closestHit;
            }

            /*HitInfo RayCollision(Ray ray)
            {
                HitInfo closestHit = (HitInfo)0;
                closestHit.distance = 1.#INF;

                for(int meshIndex = 0; meshIndex < 1; meshIndex++)
                {
                    Mesh mesh = _Meshes[meshIndex];
                    if(!RayBoundingBox(ray, mesh.boundsMin, mesh.boundsMax))
                        continue;

                    for(uint i = 0; i < mesh.indexCount; i++)
                    {
                        int triIndex = mesh.firstIndex + i;
                        Triangle tri = Triangles[triIndex];
                        HitInfo hitInfo = RayTriangle(ray, tri);
                        
                        if(hitInfo.didHit && hitInfo.distance < closestHit.distance)
                        {
                            closestHit = hitInfo;
                            closestHit.material = meshInfo.material;
                        }
                    }
                }
                return closestHit;
            }*/

            HitInfo RayAll(Ray ray)
            {
                HitInfo closestHit = (HitInfo)0;
                closestHit.distance = 1.#INF;
                uint count, stride;
                _meshes.GetDimensions(count, stride);
                
                for (uint i = 0; i < count; i++)
                {
                    HitInfo hitInfo = RayMesh(ray, _meshes[i]);
                    if(hitInfo.distance < closestHit.distance)
                        closestHit = hitInfo;
                }
                
                return closestHit;
            }

            uint FrameCount;

            float4 frag (v2f i) : SV_Target
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.x + pixelCoord.y * numPixels.x;
                uint rngState = pixelIndex + FrameCount * 16389147;
                
                float3 localViewPoint = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CameraRotation, localViewPoint);

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.direction = normalize(viewPoint);

                //return RayBoundingBox(ray, float3(-0.5,-0.5,-0.5),float3(0.5,0.5,0.5));

                return RayAll(ray).didHit;
            }
            ENDCG
        }
    }
}
