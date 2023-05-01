using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeospectraMauiDemo.Helpers
{
    public class ChartItem : INotifyPropertyChanged
    {
        private double _ay;

        public string Ax { get; set; }
        public double Ay
        {
            get
            {
                return _ay;
            }
            set
            {
                if (_ay != value)
                {
                    _ay = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ay)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class MonotoneCubicSplineInterpolation
    {
        private double[] xs { set; get; }
        private double[] ys { set; get; }
        private double[] m { set; get; }
        public double Interpolate(double x)
        {
            if (xs == null || ys == null || m == null)
            {
                throw new Exception("please create spline first.");
            }
            int i;
            for (i = xs.Length - 2; i > 0; --i)
            {
                if (xs[i] <= x)
                {
                    break;
                }
            }
            var h = xs[i + 1] - xs[i];
            var t = (x - xs[i]) / h;
            var t2 = Math.Pow(t, 2);
            var t3 = Math.Pow(t, 3);
            var h00 = 2 * t3 - 3 * t2 + 1;
            var h10 = t3 - 2 * t2 + t;
            var h01 = -2 * t3 + 3 * t2;
            var h11 = t3 - t2;
            var y_interp = h00 * ys[i] + h10 * h * m[i] + h01 * ys[i + 1] + h11 * h * m[i + 1];

            return y_interp;
        }
        public void createMonotoneCubicSpline(List<double> xs, List<double> ys)
        {
            var length = xs.Count;

            // Deal with length issues
            if (length != ys.Count)
            {
                //IPDevLoggerWrapper.Error("Need an equal count of xs and ys");
                throw new Exception("Need an equal count of xs and ys");
            }
            if (length == 0)
            {
                return;
            }
            /*
            if (length == 1)
            {
                return new double[] { ys[0] };
            }
            */
            this.xs = xs.ToArray();
            this.ys = ys.ToArray();
            // Get consecutive differences and slopes
            var delta = new double[length - 1];
            var m = new double[length];

            for (int i = 0; i < length - 1; i++)
            {
                delta[i] = (ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]);
                if (i > 0)
                {
                    m[i] = (delta[i - 1] + delta[i]) / 2;
                }
            }
            var toFix = new List<int>();
            for (int i = 1; i < length - 1; i++)
            {
                if ((delta[i] > 0 && delta[i - 1] < 0) || (delta[i] < 0 && delta[i - 1] > 0))
                {
                    toFix.Add(i);
                }
            }
            foreach (var val in toFix)
            {
                m[val] = 0;
            }

            m[0] = delta[0];
            m[length - 1] = delta[length - 2];

            toFix.Clear();
            for (int i = 0; i < length - 1; i++)
            {
                if (delta[i] == 0)
                {
                    toFix.Add(i);
                }
            }
            foreach (var val in toFix)
            {
                m[val] = 0;
                m[val + 1] = 0;
            }

            var alpha = new double[length - 1];
            var beta = new double[length - 1];
            var dist = new double[length - 1];
            var tau = new double[length - 1];
            for (int i = 0; i < length - 1; i++)
            {
                alpha[i] = m[i] / delta[i];
                beta[i] = m[i + 1] / delta[i];
                dist[i] = Math.Pow(alpha[i], 2) + Math.Pow(beta[i], 2);
                tau[i] = 3 / Math.Sqrt(dist[i]);
            }

            toFix.Clear();
            for (int i = 0; i < length - 1; i++)
            {
                if (dist[i] > 9)
                {
                    toFix.Add(i);
                }
            }

            foreach (var val in toFix)
            {
                m[val] = tau[val] * alpha[val] * delta[val];
                m[val + 1] = tau[val] * beta[val] * delta[val];
            }

            this.m = m;
        }


    }
    internal class Common
    {
    }
}
