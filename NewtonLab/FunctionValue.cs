using System;

namespace NewtonLab {
  public class FunctionValue {
    public double value;
    public double first;
    public double second;

    public FunctionValue(double functionValue, double firstDerivative, double secondDerivative) {
      value = functionValue;
      first = firstDerivative;
      second = secondDerivative;
    }

    public static FunctionValue add(FunctionValue left, FunctionValue right) {
      return new FunctionValue(left.value + right.value, left.first + right.first, left.second + right.second);
    }

    public static FunctionValue negate(FunctionValue argument) {
      return new FunctionValue(-argument.value, -argument.first, -argument.second);
    }

    public static FunctionValue multiply(FunctionValue left, FunctionValue right) {
      double value;
      double first;
      double second;

      value = left.value * right.value;
      first = left.first * right.value + left.value * right.first;
      second = left.second * right.value + 2.0 * left.first * right.first + left.value * right.second;
      return new FunctionValue(value, first, second);
    }

    public static FunctionValue divide(FunctionValue left, FunctionValue right) {
      FunctionValue reciprocal;
      double inverse;

      if (right.value == 0.0) {
        throw new ArgumentException("Деление на ноль в формуле.");
      }
      inverse = 1.0 / right.value;
      reciprocal = compose(right, inverse, -inverse * inverse, 2.0 * inverse * inverse * inverse);
      return multiply(left, reciprocal);
    }

    // The chain rule propagates both derivatives without a difference step.
    private static FunctionValue compose(FunctionValue argument, double value, double first, double second) {
      if (argument.first == 0.0 && argument.second == 0.0) {
        return new FunctionValue(value, 0.0, 0.0);
      }
      return new FunctionValue(value, first * argument.first,
        second * argument.first * argument.first + first * argument.second);
    }

    public static FunctionValue power(FunctionValue basis, FunctionValue exponent) {
      double value;
      double first;
      double second;

      if (exponent.first == 0.0 && exponent.second == 0.0) {
        value = Math.Pow(basis.value, exponent.value);
        if (exponent.value == 0.0) {
          return new FunctionValue(value, 0.0, 0.0);
        }
        if (exponent.value == 1.0) {
          return new FunctionValue(value, basis.first, basis.second);
        }
        first = exponent.value * Math.Pow(basis.value, exponent.value - 1.0);
        second = exponent.value * (exponent.value - 1.0) * Math.Pow(basis.value, exponent.value - 2.0);
        return compose(basis, value, first, second);
      }
      if (basis.value <= 0.0) {
        throw new ArgumentException("Для переменной степени основание должно быть положительным.");
      }
      return apply("exp", multiply(exponent, apply("ln", basis)));
    }

    public static FunctionValue apply(string name, FunctionValue argument) {
      double value;
      double first;
      double second;
      double cosine;
      double logarithmBase;

      switch (name) {
        case "sin":
          value = Math.Sin(argument.value);
          first = Math.Cos(argument.value);
          second = -value;
          break;
        case "cos":
          value = Math.Cos(argument.value);
          first = -Math.Sin(argument.value);
          second = -value;
          break;
        case "tan":
          cosine = Math.Cos(argument.value);
          if (Math.Abs(cosine) < 1e-15) {
            throw new ArgumentException("Тангенс не определён в этой точке.");
          }
          value = Math.Tan(argument.value);
          first = 1.0 / (cosine * cosine);
          second = 2.0 * value * first;
          break;
        case "exp":
          value = Math.Exp(argument.value);
          first = value;
          second = value;
          break;
        case "ln":
        case "log":
          logarithmBase = 1.0;
          if (name == "log") {
            logarithmBase = Math.Log(10.0);
          }
          value = Math.Log(argument.value) / logarithmBase;
          first = 1.0 / (argument.value * logarithmBase);
          second = -first / argument.value;
          break;
        case "sqrt":
          value = Math.Sqrt(argument.value);
          first = 1.0 / (2.0 * value);
          second = -1.0 / (4.0 * value * value * value);
          break;
        case "abs":
          value = Math.Abs(argument.value);
          if (double.IsNaN(value)) {
            return new FunctionValue(value, double.NaN, double.NaN);
          }
          if (argument.value == 0.0 && argument.first == 0.0) {
            return new FunctionValue(value, 0.0, Math.Abs(argument.second));
          }
          first = Math.Sign(argument.value);
          second = 0.0;
          if (argument.value == 0.0 && argument.first != 0.0) {
            first = double.NaN;
          }
          break;
        default:
          throw new ArgumentException("Неизвестная функция: " + name + ".");
      }
      return compose(argument, value, first, second);
    }
  }
}
