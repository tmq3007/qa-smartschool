using System;

namespace QASmartClass.Services
{
    public class KalmanFilter
    {
        private double _q; // Process noise covariance
        private double _r; // Measurement noise covariance
        private double _x; // Estimated state
        private double _p; // Estimation error covariance
        private double _k; // Kalman gain

        public KalmanFilter(double q = 0.08, double r = 4.0, double initialP = 1.0, double initialX = -75.0)
        {
            _q = q;
            _r = r;
            _p = initialP;
            _x = initialX;
        }

        public double Update(double measurement)
        {
            // Prediction
            _p = _p + _q;

            // Measurement update
            _k = _p / (_p + _r);
            _x = _x + _k * (measurement - _x);
            _p = (1 - _k) * _p;

            return _x;
        }

        public void Reset(double newX)
        {
            _x = newX;
            _p = 1.0;
        }

        public double CurrentEstimate => _x;
    }
}
