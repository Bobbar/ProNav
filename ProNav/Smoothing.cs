using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav
{
    /// <summary>
    /// Provides a round-robin running average for a float value over time.
    /// </summary>
    public class SmoothFloat
    {
        private List<float> _values = new List<float>();
        private int _max;
        private int _position = 0;
        private float _current;
        private float _maxDelta = 0f;
        /// <summary>
        /// Current average.
        /// </summary>
        public float Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Creates a new instance with the specified max number of values.
        /// </summary>
        /// <param name="max">The max number of values to maintain an average of.</param>
        public SmoothFloat(int max)
        {
            _max = max;
        }

        public SmoothFloat(int max, float maxDelta)
        {
            _max = max;
            _maxDelta = maxDelta;
        }

        /// <summary>
        /// Add a new value to the averaged collection.
        /// </summary>
        /// <param name="value">Value to be added to the collection and averaged.</param>
        /// <returns>Returns the new accumulative average value.</returns>
        public float Add(float value)
        {
            lock (_values)
            {
                if (_maxDelta > 0f && _position > 0 && Math.Abs(value - _current) > _maxDelta)
                {
                    Debug.WriteLine($"InstantVal: {value}   Cur: {_current}");

                    Clear();
                    Add(value);
                    return value;
                }


                // Add new values until the collection is full, then do round robin.
                if (_values.Count < _max)
                {
                    _values.Add(value);
                }
                else
                {
                    _values[_position] = value;
                }

                // Sum all values and compute the average.
                double total = 0;
                for (int i = 0; i < _values.Count; i++)
                {
                    total += _values[i];
                }

                _current = (float)total / _values.Count;

                // Move to next position.
                _position = (_position + 1) % _max;

                return _current;
            }

        }

        public void Clear()
        {
            lock (_values)
            {
                _values.Clear();
                _position = 0;
            }

        }

        public void Resize(int newSize)
        {
            _values.Clear();
            _position = 0;
            _max = newSize;
        }
    }

    public class SmoothDouble
    {
        private List<double> _values = new List<double>();
        private int _max;
        private int _position = 0;
        private double _current;
        private double _maxDelta = 0f;
        /// <summary>
        /// Current average.
        /// </summary>
        public double Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Creates a new instance with the specified max number of values.
        /// </summary>
        /// <param name="max">The max number of values to maintain an average of.</param>
        public SmoothDouble(int max)
        {
            _max = max;
        }

        public SmoothDouble(int max, double maxDelta)
        {
            _max = max;
            _maxDelta = maxDelta;
        }

        /// <summary>
        /// Add a new value to the averaged collection.
        /// </summary>
        /// <param name="value">Value to be added to the collection and averaged.</param>
        /// <returns>Returns the new accumulative average value.</returns>
        public double Add(double value)
        {
            lock (_values)
            {
                if (_maxDelta > 0f && _position > 0 && Math.Abs(value - _current) > _maxDelta)
                {
                    Debug.WriteLine($"InstantVal: {value}   Cur: {_current}");

                    Clear();
                    Add(value);
                    return value;
                }


                // Add new values until the collection is full, then do round robin.
                if (_values.Count < _max)
                {
                    _values.Add(value);
                }
                else
                {
                    _values[_position] = value;
                }

                // Sum all values and compute the average.
                double total = 0;
                for (int i = 0; i < _values.Count; i++)
                {
                    total += _values[i];
                }

                _current = total / _values.Count;

                // Move to next position.
                _position = (_position + 1) % _max;

                return _current;
            }

        }

        public void Clear()
        {
            lock (_values)
            {
                _values.Clear();
                _position = 0;
            }

        }

        public void Resize(int newSize)
        {
            _values.Clear();
            _position = 0;
            _max = newSize;
        }
    }


    public class SmoothPos
    {
        private List<D2DPoint> _values = new List<D2DPoint>();
        private int _max;
        private int _position = 0;
        private D2DPoint _current;

        /// <summary>
        /// Current average.
        /// </summary>
        public D2DPoint Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Creates a new instance with the specified max number of values.
        /// </summary>
        /// <param name="max">The max number of values to maintain an average of.</param>
        public SmoothPos(int max)
        {
            _max = max;
        }

        /// <summary>
        /// Add a new value to the averaged collection.
        /// </summary>
        /// <param name="value">Value to be added to the collection and averaged.</param>
        /// <returns>Returns the new accumulative average value.</returns>
        public D2DPoint Add(D2DPoint value)
        {
            // Reset the position if we reach the end of the collection.
            if (_position >= _max)
                _position = 0;

            // Add new values until the collection is full, then do round robin.
            if (_values.Count < _max)
            {
                _values.Add(value);
            }
            else
            {
                _values[_position] = value;
            }

            // Sum all values and compute the average.
            D2DPoint total = D2DPoint.Zero;
            for (int i = 0; i < _values.Count; i++)
            {
                total += _values[i];
            }

            _current = total / _values.Count;

            // Move to next position.
            _position++;

            return _current;
        }
    }
}
