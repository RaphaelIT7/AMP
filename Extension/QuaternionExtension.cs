﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/*
Copyright 2016 Max Kaufmann (max.kaufmann@gmail.com)
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace AMP.Extension {
    public static class QuaternionExtension {

        public static Quaternion SmoothDamp(this Quaternion rot, Quaternion target, ref Vector3 velocity, float time) {
            Vector3 from = rot.eulerAngles;
            Vector3 to = target.eulerAngles;

            return Quaternion.Euler(
                Mathf.SmoothDampAngle(from.x, to.x, ref velocity.x, time),
                Mathf.SmoothDampAngle(from.y, to.y, ref velocity.y, time),
                Mathf.SmoothDampAngle(from.z, to.z, ref velocity.z, time)
            );
        }

    }
}