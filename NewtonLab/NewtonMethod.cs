using System;
using System.Collections.Generic;

namespace NewtonLab {
  public class Iteration {
    public double x;
    public double value;
    public double first;
    public double second;
    public double correction;
    public double movement;
    public double left;
    public double right;
    public double errorBound;
    public string kind;
  }

  public class NewtonMethod {
    public List<Iteration> steps;
    public List<double> points;
    public List<string> stepNames;
    public double minimumX;
    public double minimumY;
    public double errorBound;
    public double accuracy;
    public bool isBoundary;
    public string stopReason;
    public string searchDescription;
    private Formula formula;
    private double domainLeft;
    private double domainRight;
    private int searchCount;
    private int iterationLimit;

    public NewtonMethod(Formula function) {
      formula = function;
      steps = new List<Iteration>();
      points = new List<double>();
      stepNames = new List<string>();
      searchCount = 256;
      iterationLimit = 300;
    }

    private bool oppositeSigns(FunctionValue leftValue, FunctionValue rightValue) {
      return leftValue.first < 0.0 && rightValue.first > 0.0;
    }

    private bool inspectTurningPoint(ref double left, ref double right) {
      double lower;
      double upper;
      double middle;
      FunctionValue lowerValue;
      FunctionValue upperValue;
      FunctionValue middleValue;
      int refinementIndex;

      lower = left;
      upper = right;
      lowerValue = formula.evaluate(lower);
      upperValue = formula.evaluate(upper);
      if (!((lowerValue.second < 0.0 && upperValue.second > 0.0)
          || (lowerValue.second > 0.0 && upperValue.second < 0.0))) {
        return false;
      }
      for (refinementIndex = 0; refinementIndex < 60; refinementIndex++) {
        middle = lower + (upper - lower) / 2.0;
        if (middle == lower || middle == upper) {
          break;
        }
        middleValue = formula.evaluate(middle);
        if (oppositeSigns(lowerValue, middleValue)) {
          left = lower;
          right = middle;
          return true;
        }
        if (oppositeSigns(middleValue, upperValue)) {
          left = middle;
          right = upper;
          return true;
        }
        if ((lowerValue.second < 0.0 && middleValue.second < 0.0)
            || (lowerValue.second > 0.0 && middleValue.second > 0.0)) {
          lower = middle;
          lowerValue = middleValue;
        } else {
          upper = middle;
          upperValue = middleValue;
        }
      }
      return false;
    }

    private bool findBracket(ref double left, ref double right) {
      double middle;
      double sampleStep;
      double previousPoint;
      double currentPoint;
      double candidateLeft;
      double candidateRight;
      FunctionValue leftValue;
      FunctionValue rightValue;
      FunctionValue previousValue;
      FunctionValue currentValue;
      int sampleIndex;

      leftValue = formula.evaluate(left);
      rightValue = formula.evaluate(right);
      middle = left + (right - left) / 2.0;
      if (oppositeSigns(leftValue, rightValue) && formula.evaluate(middle).second >= 0.0) {
        searchDescription = "Начальная точка — середина заданного интервала.";
        return true;
      }
      sampleStep = (right - left) / searchCount;
      previousPoint = left;
      previousValue = leftValue;
      for (sampleIndex = 1; sampleIndex <= searchCount; sampleIndex++) {
        currentPoint = domainLeft + (domainRight - domainLeft) * sampleIndex / searchCount;
        currentValue = formula.evaluate(currentPoint);
        if (oppositeSigns(previousValue, currentValue)) {
          left = previousPoint;
          right = currentPoint;
          searchDescription = "Начальная область найдена по смене знака f′: минус → плюс.";
          return true;
        }
        if (currentValue.first == 0.0 && currentPoint < domainRight) {
          candidateLeft = Math.Max(domainLeft, currentPoint - sampleStep / 4.0);
          candidateRight = Math.Min(domainRight, currentPoint + sampleStep / 4.0);
          if (oppositeSigns(formula.evaluate(candidateLeft), formula.evaluate(candidateRight))) {
            left = candidateLeft;
            right = candidateRight;
            searchDescription = "Стационарная точка изолирована проверкой знаков f′.";
            return true;
          }
        }
        candidateLeft = previousPoint;
        candidateRight = currentPoint;
        if (inspectTurningPoint(ref candidateLeft, ref candidateRight)) {
          left = candidateLeft;
          right = candidateRight;
          searchDescription = "Узкий минимум найден уточнением точки смены знака f″.";
          return true;
        }
        previousPoint = currentPoint;
        previousValue = currentValue;
      }
      return false;
    }

    private bool tightenBracket(double point, ref double left, ref double right) {
      double radius;
      double candidateLeft;
      double candidateRight;
      FunctionValue leftValue;
      FunctionValue rightValue;

      radius = accuracy / 2.0;
      candidateLeft = Math.Max(left, point - radius);
      candidateRight = Math.Min(right, point + radius);
      if (candidateLeft >= point || candidateRight <= point) {
        return false;
      }
      leftValue = formula.evaluate(candidateLeft);
      rightValue = formula.evaluate(candidateRight);
      if (oppositeSigns(leftValue, rightValue)) {
        left = candidateLeft;
        right = candidateRight;
        return true;
      }
      return false;
    }

    private void saveStep(double point, double left, double right, string kind) {
      Iteration step;
      FunctionValue value;

      value = formula.evaluate(point);
      step = new Iteration();
      step.x = point;
      step.value = value.value;
      step.first = value.first;
      step.second = value.second;
      step.correction = double.NaN;
      if (value.second > 0.0) {
        step.correction = Math.Abs(value.first / value.second);
      }
      step.left = left;
      step.right = right;
      step.errorBound = Math.Max(point - left, right - point);
      step.movement = double.NaN;
      if (steps.Count > 0) {
        step.movement = Math.Abs(point - steps[steps.Count - 1].x);
      }
      step.kind = kind;
      steps.Add(step);
      points.Add(point);
      stepNames.Add(kind);
    }

    private bool findBoundaryMinimum() {
      FunctionValue leftValue;
      FunctionValue rightValue;
      double probe;
      bool leftMinimum;
      bool rightMinimum;

      leftValue = formula.evaluate(domainLeft);
      rightValue = formula.evaluate(domainRight);
      probe = (domainRight - domainLeft) / searchCount;
      leftMinimum = leftValue.first > 0.0 || (leftValue.first == 0.0
        && formula.evaluate(domainLeft + probe).first > 0.0 && leftValue.second >= 0.0);
      rightMinimum = rightValue.first < 0.0 || (rightValue.first == 0.0
        && formula.evaluate(domainRight - probe).first < 0.0 && rightValue.second >= 0.0);
      if (!leftMinimum && !rightMinimum) {
        return false;
      }
      minimumX = domainLeft;
      if (!leftMinimum || (rightMinimum && rightValue.value < leftValue.value)) {
        minimumX = domainRight;
      }
      minimumY = formula.calculate(minimumX);
      isBoundary = true;
      errorBound = 0.0;
      searchDescription = "Внутренняя область не обнаружена; проверены односторонние условия на границах.";
      stopReason = "Минимум на границе [a, b]: одностороннее условие. Итерации Ньютона не требуются.";
      saveStep(minimumX, minimumX, minimumX, "Граница отрезка");
      return true;
    }

    public void calculate(double left, double right, double requestedAccuracy) {
      double width;
      double point;
      double nextPoint;
      double samplePoint;
      FunctionValue value;
      string kind;
      int sampleIndex;
      int iterationIndex;
      int validationCount;

      if (double.IsNaN(left) || double.IsInfinity(left) || double.IsNaN(right)
          || double.IsInfinity(right) || left >= right) {
        throw new ArgumentException("Нужны конечные границы a < b.");
      }
      if (double.IsNaN(requestedAccuracy) || double.IsInfinity(requestedAccuracy) || requestedAccuracy <= 0.0) {
        throw new ArgumentException("Точность e должна быть конечным числом больше нуля.");
      }
      width = right - left;
      if (double.IsInfinity(width) || left + width / 2.0 == left || left + width / 2.0 == right) {
        throw new ArgumentException("Границы слишком велики или между ними нет представимой внутренней точки double.");
      }
      steps.Clear();
      points.Clear();
      stepNames.Clear();
      accuracy = requestedAccuracy;
      domainLeft = left;
      domainRight = right;
      isBoundary = false;
      validationCount = 800;
      for (sampleIndex = 0; sampleIndex <= validationCount; sampleIndex++) {
        samplePoint = left + width * ((double)sampleIndex / validationCount);
        formula.evaluate(samplePoint);
      }
      if (!findBracket(ref left, ref right)) {
        if (findBoundaryMinimum()) {
          return;
        }
        throw new ArgumentException("Изолированный минимум не обнаружен. Уточните интервал вокруг нужного минимума. "
          + "Постоянные функции и неразрешимые на текущем масштабе особенности не поддерживаются.");
      }

      point = left + (right - left) / 2.0;
      kind = "Начальная точка";
      for (iterationIndex = 0; iterationIndex < iterationLimit; iterationIndex++) {
        value = formula.evaluate(point);
        tightenBracket(point, ref left, ref right);
        if (value.first < 0.0) {
          left = point;
        } else if (value.first > 0.0) {
          right = point;
        }
        saveStep(point, left, right, kind);
        errorBound = steps[steps.Count - 1].errorBound;
        if (errorBound <= accuracy && value.second >= 0.0) {
          minimumX = point;
          minimumY = value.value;
          stopReason = "R = max(x − L, U − x) ≤ e; f′(L) < 0 < f′(U). "
            + "Оценка положения минимума при непрерывности f′ и изолированной области.";
          return;
        }

        nextPoint = double.NaN;
        if (value.second > 0.0) {
          nextPoint = point - value.first / value.second;
        }
        if (!double.IsNaN(nextPoint) && !double.IsInfinity(nextPoint)
            && nextPoint > left && nextPoint < right && nextPoint != point) {
          kind = "Ньютон";
        } else {
          nextPoint = left + (right - left) / 2.0;
          kind = "Деление отрезка";
        }
        if (nextPoint == point || nextPoint == left || nextPoint == right) {
          throw new ArgumentException("Точность e недостижима на этом масштабе double. "
            + "Последний радиус отрезка: " + errorBound.ToString("G6") + ". Увеличьте e.");
        }
        point = nextPoint;
      }
      throw new ArgumentException("Не удалось подтвердить точность за " + iterationLimit + " итераций. Уточните интервал.");
    }
  }
}
