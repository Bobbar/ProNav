namespace ProNav.GameObjects
{
    public class RandomVariationFloat
    {
        private float _curTime = 0f;
        private float _nextTime = 0f;
        private float _target = 0f;
        private float _prevTarget = 0f;
        private float _minTime = 0f;
        private float _maxTime = 0f;
        private float _minValue = 0f;
        private float _maxValue = 0f;

        public float Value { get; private set; } = 0f;

        public RandomVariationFloat(float minValue, float maxValue, float minTime, float maxTime)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _minTime = minTime;
            _maxTime = maxTime;

            _target = Helpers.Rnd.NextFloat(_minValue, _maxValue);
            _prevTarget = _target;
            Value = _target;
            _nextTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
        }

        public void Update(float dt)
        {
            if (_curTime >= _nextTime)
            {
                _target = Helpers.Rnd.NextFloat(_minValue, _maxValue);
                _nextTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
                _curTime = 0f;
                _prevTarget = Value;
            }

            Value = Helpers.Lerp(_prevTarget, _target, Helpers.Factor(_curTime, _nextTime));

            _curTime += dt;
        }
    }

    public class RandomVariationPoint
    {
        private float _curTime = 0f;
        private float _nextTime = 0f;
        private D2DPoint _target = D2DPoint.Zero;
        private D2DPoint _prevTarget = D2DPoint.Zero;
        private float _minTime = 0f;
        private float _maxTime = 0f;
        private float _minValue = 0f;
        private float _maxValue = 0f;

        public D2DPoint Value { get; private set; } = D2DPoint.Zero;


        public RandomVariationPoint(float minMaxValue, float minTime, float maxTime)
        {
            _minValue = -minMaxValue;
            _maxValue = minMaxValue;
            _minTime = minTime;
            _maxTime = maxTime;

            _target = new D2DPoint(Helpers.Rnd.NextFloat(_minValue, _maxValue), Helpers.Rnd.NextFloat(_minValue, _maxValue));
            _prevTarget = _target;
            Value = _target;
            _nextTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
        }

        public RandomVariationPoint(float minValue, float maxValue, float minTime, float maxTime)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _minTime = minTime;
            _maxTime = maxTime;

            _target = new D2DPoint(Helpers.Rnd.NextFloat(_minValue, _maxValue), Helpers.Rnd.NextFloat(_minValue, _maxValue));
            _prevTarget = _target;
            Value = _target;
            _nextTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
        }

        public void Update(float dt)
        {
            if (_curTime >= _nextTime)
            {
                _target = new D2DPoint(Helpers.Rnd.NextFloat(_minValue, _maxValue), Helpers.Rnd.NextFloat(_minValue, _maxValue));
                _nextTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
                _curTime = 0f;
                _prevTarget = Value;
            }

            Value = Helpers.LerpPoints(_prevTarget, _target, Helpers.Factor(_curTime, _nextTime));

            _curTime += dt;
        }
    }

    public class RandomVariationVector
    {
        private float _curMagTime = 0f;
        private float _nextMagTime = 0f;

        private float _curDirTime = 0f;
        private float _nextDirTime = 0f;

        private float _targetMag = 0f;
        private float _currentMag = 0f;
        private float _prevTargetMag = 0f;

        private float _targetDir = 0f;
        private float _currentDir = 0f;
        private float _prevTargetDir = 0f;

        private float _minTime = 0f;
        private float _maxTime = 0f;
        private float _minMag = 0f;
        private float _maxMag = 0f;

        public D2DPoint Value
        {
            get
            {
                var vec = Helpers.AngleToVectorDegrees(_currentDir) * _currentMag;
                return vec;
            }
        }

        public RandomVariationVector(float minMaxMagnitude, float minTime, float maxTime)
        {
            _minMag = -minMaxMagnitude;
            _maxMag = minMaxMagnitude;
            _minTime = minTime;
            _maxTime = maxTime;

            _targetMag = Helpers.Rnd.NextFloat(_minMag, _maxMag);
            _prevTargetMag = _targetMag;
            _currentMag = _targetMag;

            _targetDir = Helpers.Rnd.NextFloat(0f, 360f);
            _prevTargetDir = _targetDir;
            _currentDir = _targetDir;

            _nextMagTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
            _nextDirTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
        }

        public void Update(float dt)
        {
            if (_curMagTime >= _nextMagTime)
            {
                _targetMag = Helpers.Rnd.NextFloat(_minMag, _maxMag);
                _nextMagTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
                _curMagTime = 0f;
                _prevTargetMag = _currentMag;
            }

            _currentMag = Helpers.Lerp(_prevTargetMag, _targetMag, Helpers.Factor(_curMagTime, _nextMagTime));
            _curMagTime += dt;


            if (_curDirTime >= _nextDirTime)
            {
                _targetDir = Helpers.Rnd.NextFloat(0f, 360f);
                _nextDirTime = Helpers.Rnd.NextFloat(_minTime, _maxTime);
                _curDirTime = 0f;
                _prevTargetDir = _currentDir;
            }

            //_currentDir = Helpers.Lerp(_prevTargetDir, _targetDir, Helpers.Factor(_curDirTime, _nextDirTime));
            _currentDir = Helpers.LerpAngle(_prevTargetDir, _targetDir, Helpers.Factor(_curDirTime, _nextDirTime));

            _curDirTime += dt;
        }
    }

}
