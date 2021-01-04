#ifndef LIBNOISE_MODEL
#define LIBNOISE_MODEL

#include "libnoise.cginc"

Module Model_Cylinder_GetModule(Module m_pModule, float angle, float height)
{
   float x, y, z;
   m_pModule.m_X = cos(angle * DEG_TO_RAD);
   m_pModule.m_Y = height;
   m_pModule.m_Z = sin(angle * DEG_TO_RAD);
   return m_pModule;
}

Module Model_Line_GetModule(Module m_pModule, float3 start, float3 end, float p)
{
   m_pModule.m_X = (end.x - start.x) * p + start.x;
   m_pModule.m_Y = (end.y - start.y) * p + start.y;
   m_pModule.m_Z = (end.z - start.z) * p + start.z;

   return m_pModule;
}

float Model_Line_m_Attenuate(Module m_pModule, float p)
{
   m_pModule.Value = p * (1.0 - p) * 4 * m_pModule.Value;
   return m_pModule.Value;
}

Module Sphere_GetValue(Module m_pModule, double lat, double lon)
{
   float3 res = LatLonToXYZ(lat, lon);

   m_pModule.m_X = res.x;
   m_pModule.m_Y = res.y;
   m_pModule.m_Z = res.z;

   return m_pModule;
}


#endif