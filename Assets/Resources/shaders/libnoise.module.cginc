#ifndef LIBNOISE_MODULE
#define LIBNOISE_MODULE

#include "libnoise.cginc"

///
/// Base modules
///
Module Perlin_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   float value = 0.0;
   float signal = 0.0;
   float curPersistence = 1.0;
   float nx, ny, nz;
   int seed;

   x *= module.m_frequency;
   y *= module.m_frequency;
   z *= module.m_frequency;

   for (int curOctave = 0; curOctave < module.m_octaveCount && curOctave < MAX_OCTAVE; curOctave++) {

      // Make sure that these floating-point values have the same range as a 32-
      // bit integer so that we can pass them to the coherent-noise functions.
      /*nx = MakeInt32Range(x);
      ny = MakeInt32Range(y);
      nz = MakeInt32Range(z);*/
      nx = x;
      ny = y;
      nz = z;

      // Get the coherent-noise value from the input value and add it to the
      // final result.
      seed = (module.m_seed + curOctave) & 0xffffffff;
      signal = GradientCoherentNoise3D(nx, ny, nz, seed, module.m_noiseQuality);
      value += signal * curPersistence;

      // Prepare the next octave.
      x *= module.m_lacunarity;
      y *= module.m_lacunarity;
      z *= module.m_lacunarity;
      curPersistence *= module.m_persistence;
   }

   module.Value = value;
   return module;
}

Module Billow_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   float value = 0.0;
   float signal = 0.0;
   float curPersistence = 1.0;
   float nx, ny, nz;
   int seed;

   x *= module.m_frequency;
   y *= module.m_frequency;
   z *= module.m_frequency;

   for (int curOctave = 0; curOctave < module.m_octaveCount && curOctave < MAX_OCTAVE; curOctave++) {

      // Make sure that these floating-point values have the same range as a 32-
      // bit integer so that we can pass them to the coherent-noise functions.
      nx = MakeInt32Range(x);
      ny = MakeInt32Range(y);
      nz = MakeInt32Range(z);

      // Get the coherent-noise value from the input value and add it to the
      // final result.

      seed = (module.m_seed + curOctave) & 0xffffffff;
      signal = GradientCoherentNoise3D(nx, ny, nz, seed, module.m_noiseQuality);
      signal = 2.0 * abs(signal) - 1.0;
      value += signal * curPersistence;

      // Prepare the next octave.
      x *= module.m_lacunarity;
      y *= module.m_lacunarity;
      z *= module.m_lacunarity;
      curPersistence *= module.m_persistence;
   }
   value += 0.5;

   module.Value = value;
   return module;
}

Module RidgedMulti_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   x *= module.m_frequency;
   y *= module.m_frequency;
   z *= module.m_frequency;

   float signal = 0.0;
   float value = 0.0;
   float weight = 1.0;


   float specFreq = 1.0;
   float pSpectralWeight = 0;

   for (int curOctave = 0; curOctave < module.m_octaveCount && curOctave < MAX_OCTAVE; curOctave++) {

      // Make sure that these floating-point values have the same range as a 32-
      // bit integer so that we can pass them to the coherent-noise functions.
      float nx, ny, nz;
      nx = MakeInt32Range(x);
      ny = MakeInt32Range(y);
      nz = MakeInt32Range(z);

      // Get the coherent-noise value.
      int seed = (module.m_seed + curOctave) & 0x7fffffff;
      signal = GradientCoherentNoise3D(nx, ny, nz, seed, module.m_noiseQuality);

      // Make the ridges.
      signal = abs(signal);
      signal = module.m_ridged_offset - signal;

      // Square the signal to increase the sharpness of the ridges.
      signal *= signal;

      // The weighting from the previous octave is applied to the signal.
      // Larger values have higher weights, producing sharp points along the
      // ridges.
      signal *= weight;

      // Weight successive contributions by the previous signal.
      weight = signal * module.m_ridged_gain;
      if (weight > 1.0) {
         weight = 1.0;
      }
      if (weight < 0.0) {
         weight = 0.0;
      }

      // Add the signal to the output value.
      pSpectralWeight = pow(specFreq, -module.m_ridged_H);
      specFreq *= module.m_lacunarity;

      value += (signal * pSpectralWeight);

      // Go to the next octave.
      x *= module.m_lacunarity;
      y *= module.m_lacunarity;
      z *= module.m_lacunarity;
   }

   module.Value = (value * 1.25) - 1.0;
   return module;
}

Module Checkerboard_GetValue(Module module)
{
   int ix = (int)(floor(MakeInt32Range(module.m_X)));
   int iy = (int)(floor(MakeInt32Range(module.m_Y)));
   int iz = (int)(floor(MakeInt32Range(module.m_Z)));

   module.Value = (ix & 1 ^ iy & 1 ^ iz & 1) ? -1.0 : 1.0;

   return module;
}

Module Cylinders_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   x *= module.m_frequency;
   z *= module.m_frequency;

   float distFromCenter = sqrt(x * x + z * z);
   float distFromSmallerSphere = distFromCenter - floor(distFromCenter);
   float distFromLargerSphere = 1.0 - distFromSmallerSphere;
   float nearestDist = min(distFromSmallerSphere, distFromLargerSphere);

   module.Value = 1.0 - (nearestDist * 4.0);
   return module; // Puts it in the -1.0 to +1.0 range.
}

Module Spheres_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   x *= module.m_frequency;
   y *= module.m_frequency;
   z *= module.m_frequency;

   float distFromCenter = sqrt(x * x + y * y + z * z);
   float distFromSmallerSphere = distFromCenter - floor(distFromCenter);
   float distFromLargerSphere = 1.0 - distFromSmallerSphere;
   float nearestDist = min(distFromSmallerSphere, distFromLargerSphere);

   module.Value = 1.0 - (nearestDist * 4.0); // Puts it in the -1.0 to +1.0 range.
   return module;
}

Module Voronoi_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   // This method could be more efficient by caching the seed values.  Fix
   // later.

   x *= module.m_frequency;
   y *= module.m_frequency;
   z *= module.m_frequency;

   int xInt = (x > 0.0 ? (int)x : (int)x - 1);
   int yInt = (y > 0.0 ? (int)y : (int)y - 1);
   int zInt = (z > 0.0 ? (int)z : (int)z - 1);

   float minDist = 2147483647.0;
   float xCandidate = 0;
   float yCandidate = 0;
   float zCandidate = 0;

   // Inside each unit cube, there is a seed point at a random position.  Go
   // through each of the nearby cubes until we find a cube with a seed point
   // that is closest to the specified position.
   for (int zCur = zInt - 2; zCur <= zInt + 2; zCur++) {
      for (int yCur = yInt - 2; yCur <= yInt + 2; yCur++) {
         for (int xCur = xInt - 2; xCur <= xInt + 2; xCur++) {

            // Calculate the position and distance to the seed point inside of
            // this unit cube.
            float xPos = xCur + ValueNoise3D(xCur, yCur, zCur, module.m_seed);
            float yPos = yCur + ValueNoise3D(xCur, yCur, zCur, module.m_seed + 1);
            float zPos = zCur + ValueNoise3D(xCur, yCur, zCur, module.m_seed + 2);
            float xDist = xPos - x;
            float yDist = yPos - y;
            float zDist = zPos - z;
            float dist = xDist * xDist + yDist * yDist + zDist * zDist;

            if (dist < minDist) {
               // This seed point is closer to any others found so far, so record
               // this seed point.
               minDist = dist;
               xCandidate = xPos;
               yCandidate = yPos;
               zCandidate = zPos;
            }
         }
      }
   }

   float value;
   if (module.m_voronoi_enableDistance) {
      // Determine the distance to the nearest seed point.
      float xDist = xCandidate - x;
      float yDist = yCandidate - y;
      float zDist = zCandidate - z;
      value = (sqrt(xDist * xDist + yDist * yDist + zDist * zDist)
         ) * SQRT_3 - 1.0;
   }
   else {
      value = 0.0;
   }

   // Return the calculated distance with the displacement value applied.
   module.Value = value + (module.m_voronoi_displacement * (float)ValueNoise3D(
      (int)(floor(xCandidate)),
      (int)(floor(yCandidate)),
      (int)(floor(zCandidate))));

   return module;
}

///
/// Acts on value of module
///
Module ABS_GetValue(Module module)
{
   module.Value = abs(module.Value);
   return module;
}

Module Curve_GetValue(Module module, StructuredBuffer<ControlPoint> m_pControlPoints, int m_controlPointCount)
{
   // Get the output value from the source module.
   float sourceModuleValue = module.Value;

   // Find the first element in the control point array that has an input value
   // larger than the output value from the source module.
   int indexPos;
   for (indexPos = 0; indexPos < m_controlPointCount; indexPos++) {
      if (sourceModuleValue < m_pControlPoints[indexPos].inputValue) {
         break;
      }
   }

   // Find the four nearest control points so that we can perform cubic
   // interpolation.
   int index0 = ClampValue(indexPos - 2, 0, m_controlPointCount - 1);
   int index1 = ClampValue(indexPos - 1, 0, m_controlPointCount - 1);
   int index2 = ClampValue(indexPos, 0, m_controlPointCount - 1);
   int index3 = ClampValue(indexPos + 1, 0, m_controlPointCount - 1);

   // If some control points are missing (which occurs if the value from the
   // source module is greater than the largest input value or less than the
   // smallest input value of the control point array), get the corresponding
   // output value of the nearest control point and exit now.
   if (index1 == index2) {
      module.Value = m_pControlPoints[index1].outputValue;
      return module;
   }

   // Compute the alpha value used for cubic interpolation.
   float input0 = m_pControlPoints[index1].inputValue;
   float input1 = m_pControlPoints[index2].inputValue;
   float alpha = (sourceModuleValue - input0) / (input1 - input0);

   // Now perform the cubic interpolation given the alpha value.
   module.Value = CubicInterp(
      m_pControlPoints[index0].outputValue,
      m_pControlPoints[index1].outputValue,
      m_pControlPoints[index2].outputValue,
      m_pControlPoints[index3].outputValue,
      alpha);

   return module;
}

Module Curve_GetValue(Module module, ControlPoint m_pControlPoints[MAX_CONTROL_POINTS], int m_controlPointCount)
{
   // Get the output value from the source module.
   float sourceModuleValue = module.Value;

   // Find the first element in the control point array that has an input value
   // larger than the output value from the source module.
   int indexPos;
   for (indexPos = 0; indexPos < m_controlPointCount; indexPos++) {
      if (sourceModuleValue < m_pControlPoints[indexPos].inputValue) {
         break;
      }
   }

   // Find the four nearest control points so that we can perform cubic
   // interpolation.
   int index0 = ClampValue(indexPos - 2, 0, m_controlPointCount - 1);
   int index1 = ClampValue(indexPos - 1, 0, m_controlPointCount - 1);
   int index2 = ClampValue(indexPos, 0, m_controlPointCount - 1);
   int index3 = ClampValue(indexPos + 1, 0, m_controlPointCount - 1);

   // If some control points are missing (which occurs if the value from the
   // source module is greater than the largest input value or less than the
   // smallest input value of the control point array), get the corresponding
   // output value of the nearest control point and exit now.
   if (index1 == index2) {
      module.Value = m_pControlPoints[index1].outputValue;
      return module;
   }

   // Compute the alpha value used for cubic interpolation.
   float input0 = m_pControlPoints[index1].inputValue;
   float input1 = m_pControlPoints[index2].inputValue;
   float alpha = (sourceModuleValue - input0) / (input1 - input0);

   // Now perform the cubic interpolation given the alpha value.
   module.Value = CubicInterp(
      m_pControlPoints[index0].outputValue,
      m_pControlPoints[index1].outputValue,
      m_pControlPoints[index2].outputValue,
      m_pControlPoints[index3].outputValue,
      alpha);

   return module;
}

Module Exponent_GetValue(Module module, float m_exponent)
{
   float value = module.Value;
   module.Value = (pow(abs((value + 1.0) / 2.0), m_exponent) * 2.0 - 1.0);
   return module;
}

Module Invert_GetValue(Module module)
{
   module.Value = -(module.Value);
   return module;
}

Module ScaleBias_GetValue(Module module, float m_scale, float m_bias)
{
   module.Value = module.Value * m_scale + m_bias;
   return module;
}



///
/// Acts on other attributes of module.
///
Module Displace_GetModule(Module module, Module dm_X, Module dm_Y, Module dm_Z)
{
   // Get the output values from the three displacement modules.  Add each
   // value to the corresponding coordinate in the input value.
   module.m_X = module.m_X + (dm_X.Value);
   module.m_Y = module.m_Y + (dm_Y.Value);
   module.m_Z = module.m_Z + (dm_Z.Value);

   // Retrieve the output value using the offsetted input value instead of
   // the original input value.
   return module;
}

Module RotatePoint_SetAngles(Module module, float3 angle)
{
   float xAngle = angle.x;
   float yAngle = angle.y;
   float zAngle = angle.z;

   float xCos;
   float yCos;
   float zCos;
   float xSin;
   float ySin;
   float zSin;

   xCos = cos(xAngle * DEG_TO_RAD);
   yCos = cos(yAngle * DEG_TO_RAD);
   zCos = cos(zAngle * DEG_TO_RAD);
   xSin = sin(xAngle * DEG_TO_RAD);
   ySin = sin(yAngle * DEG_TO_RAD);
   zSin = sin(zAngle * DEG_TO_RAD);

   module.m_x1Matrix = ySin * xSin * zSin + yCos * zCos;
   module.m_y1Matrix = xCos * zSin;
   module.m_z1Matrix = ySin * zCos - yCos * xSin * zSin;
   module.m_x2Matrix = ySin * xSin * zCos - yCos * zSin;
   module.m_y2Matrix = xCos * zCos;
   module.m_z2Matrix = -yCos * xSin * zCos - ySin * zSin;
   module.m_x3Matrix = -ySin * xCos;
   module.m_y3Matrix = xSin;
   module.m_z3Matrix = yCos * xCos;

   module.m_xAngle = xAngle;
   module.m_yAngle = yAngle;
   module.m_zAngle = zAngle;

   return module;
}

Module RotatePoint_GetModule(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   module.m_X = (module.m_x1Matrix * x) + (module.m_y1Matrix * y) + (module.m_z1Matrix * z);
   module.m_Y = (module.m_x2Matrix * x) + (module.m_y2Matrix * y) + (module.m_z2Matrix * z);
   module.m_Z = (module.m_x3Matrix * x) + (module.m_y3Matrix * y) + (module.m_z3Matrix * z);

   return module;
}

Module RotatePoint_GetModule(Module module, float3 angle)
{
   RotatePoint_SetAngles(module, angle);
   RotatePoint_GetModule(module);

   return module;
}

Module ScalePoint_GetModule(Module module, float3 m_scale)
{
   module.m_X *= m_scale.x;
   module.m_Y *= m_scale.y;
   module.m_Z *= m_scale.z;

   return module;
}

Module Select_SetEdgeFalloff(Module module, float edgeFalloff)
{
   // Make sure that the edge falloff curves do not overlap.
   float boundSize = module.m_upperBound - module.m_lowerBound;
   module.m_edgeFalloff = (edgeFalloff > boundSize / 2) ? boundSize / 2 : edgeFalloff;

   return module;
}

Module Select_SetBounds(Module module, float lowerBound, float upperBound)
{
   module.m_lowerBound = lowerBound;
   module.m_upperBound = upperBound;

   // Make sure that the edge falloff curves do not overlap.
   Select_SetEdgeFalloff(module, module.m_edgeFalloff);

   return module;
}

Module TranslatePoint_GetModule(Module module, float3 m_Translation)
{
   module.m_X += m_Translation.x;
   module.m_Y += m_Translation.y;
   module.m_Z += m_Translation.z;

   return module;
}

Module Turbulence_GetValue(Module module)
{
   float x = module.m_X;
   float y = module.m_Y;
   float z = module.m_Z;

   // Get the values from the three noise::module::Perlin noise modules and
   // add each value to each coordinate of the input value.  There are also
   // some offsets added to the coordinates of the input values.  This prevents
   // the distortion modules from returning zero if the (x, y, z) coordinates,
   // when multiplied by the frequency, are near an integer boundary.  This is
   // due to a property of gradient coherent noise, which returns zero at
   // integer boundaries.
   float x0, y0, z0;
   float x1, y1, z1;
   float x2, y2, z2;
   x0 = x + (12414.0 / 65536.0);
   y0 = y + (65124.0 / 65536.0);
   z0 = z + (31337.0 / 65536.0);
   x1 = x + (26519.0 / 65536.0);
   y1 = y + (18128.0 / 65536.0);
   z1 = z + (60493.0 / 65536.0);
   x2 = x + (53820.0 / 65536.0);
   y2 = y + (11213.0 / 65536.0);
   z2 = z + (44845.0 / 65536.0);

   Module m_DistortModule = GetModule(x0, y0, z0);
   m_DistortModule.m_seed = module.m_turb_seed;
   m_DistortModule.m_frequency = module.m_turb_frequency;
   m_DistortModule.m_octaveCount = module.m_turb_roughness;
   float xDistort = x + (Perlin_GetValue(m_DistortModule).Value * module.m_turb_power);

   m_DistortModule = GetModule(x1, y1, z1);
   m_DistortModule.m_seed = module.m_turb_seed + 1;
   m_DistortModule.m_frequency = module.m_turb_frequency;
   m_DistortModule.m_octaveCount = module.m_turb_roughness;
   float yDistort = y + (Perlin_GetValue(m_DistortModule).Value * module.m_turb_power);

   m_DistortModule = GetModule(x2, y2, z2);
   m_DistortModule.m_seed = module.m_turb_seed + 2;
   m_DistortModule.m_frequency = module.m_turb_frequency;
   m_DistortModule.m_octaveCount = module.m_turb_roughness;
   float zDistort = z + (Perlin_GetValue(m_DistortModule).Value * module.m_turb_power);

   // Retrieve the output value at the offsetted input value instead of the
   // original input value.
   module.m_X = xDistort;
   module.m_Y = yDistort;
   module.m_Z = zDistort;
   
   return module;
}

///
/// Only returns a new float value. Does not modify module
///
float Blend_GetValue(Module module_1, Module module_2, Module module_alpha)
{
   double v0 = module_1.Value;
   double v1 = module_2.Value;
   double alpha = (module_alpha.Value + 1.0) / 2.0;

   return LinearInterp(v0, v1, alpha);
}

float Max_GetValue(Module module_1, Module module_2)
{
   float v0 = module_1.Value;
   float v1 = module_2.Value;
   return max(v0, v1);
}

float Min_GetValue(Module module_1, Module module_2)
{
   float v0 = module_1.Value;
   float v1 = module_2.Value;
   return min(v0, v1);
}

float Add_GetValue(Module module_1, Module module_2)
{
   return module_1.Value + module_2.Value;
}

float Multiply_GetValue(Module module_1, Module module_2)
{
   return module_1.Value * module_2.Value;
}

float Subtract_GetValue(Module module_1, Module module_2)
{
   return module_1.Value - module_2.Value;
}

float Divide_GetValue(Module module_1, Module module_2)
{
   return module_1.Value / module_2.Value;
}

float Power_GetValue(Module module_1, Module module_2)
{
   return pow(module_1.Value, module_2.Value);
}

float Select_GetValue(Module module_1, Module module_2, Module control_mod)
{
   float value;

   float controlValue = control_mod.Value;
   float m_edgeFalloff = control_mod.m_edgeFalloff;
   float m_lowerBound = control_mod.m_lowerBound;
   float m_upperBound = control_mod.m_upperBound;

   
   float alpha;
   if (m_edgeFalloff > 0.0) {
      if (controlValue < (m_lowerBound - m_edgeFalloff)) {
         // The output value from the control module is below the selector
         // threshold; return the output value from the first source module.
         value = module_1.Value;

      }
      else if (controlValue < (m_lowerBound + m_edgeFalloff)) {
         // The output value from the control module is near the lower end of the
         // selector threshold and within the smooth curve. Interpolate between
         // the output values from the first and second source modules.
         float lowerCurve = (m_lowerBound - m_edgeFalloff);
         float upperCurve = (m_lowerBound + m_edgeFalloff);
         alpha = SCurve3((controlValue - lowerCurve) / (upperCurve - lowerCurve));

         value = LinearInterp(module_1.Value, module_2.Value, alpha);

      }
      else if (controlValue < (m_upperBound - m_edgeFalloff)) {
         // The output value from the control module is within the selector
         // threshold; return the output value from the second source module.
         value = module_2.Value;

      }
      else if (controlValue < (m_upperBound + m_edgeFalloff)) {
         // The output value from the control module is near the upper end of the
         // selector threshold and within the smooth curve. Interpolate between
         // the output values from the first and second source modules.
         float lowerCurve = (m_upperBound - m_edgeFalloff);
         float upperCurve = (m_upperBound + m_edgeFalloff);
         alpha = SCurve3((controlValue - lowerCurve) / (upperCurve - lowerCurve));

         value = LinearInterp(module_2.Value, module_1.Value, alpha);

      }
      else {
         // Output value from the control module is above the selector threshold;
         // return the output value from the first source module.
         value = module_1.Value;
      }
   }
   else {
      if (controlValue < m_lowerBound || controlValue > m_upperBound) {
         value = module_1.Value;
      }
      else {
         value = module_2.Value;
      }
   }

   return value;
}

#endif