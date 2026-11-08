using System;
using System.Globalization;

namespace NewtonLab {
  public class Formula {
    private string text;
    private int position;
    private int depth;
    private double argument;

    public Formula(string formulaText) {
      if (string.IsNullOrWhiteSpace(formulaText)) {
        throw new ArgumentException("Введите формулу функции.");
      }
      if (formulaText.Length > 300) {
        throw new ArgumentException("Формула слишком длинная. Максимум 300 символов.");
      }
      text = formulaText.ToLowerInvariant().Replace(',', '.');
    }

    public double calculate(double value) {
      return parse(value).value;
    }

    public FunctionValue evaluate(double value) {
      FunctionValue result;

      result = parse(value);
      if (double.IsNaN(result.first) || double.IsInfinity(result.first)
          || double.IsNaN(result.second) || double.IsInfinity(result.second)) {
        throw new ArgumentException("Производные не определены при x = " + value.ToString("G10")
          + ". Для метода Ньютона нужна гладкая функция.");
      }
      return result;
    }

    private FunctionValue parse(double value) {
      FunctionValue result;

      position = 0;
      depth = 0;
      argument = value;
      result = readSum();
      skipSpaces();
      if (position != text.Length) {
        throw new ArgumentException("Лишний символ в формуле, позиция " + (position + 1) + ". Используйте * для умножения.");
      }
      if (double.IsNaN(result.value) || double.IsInfinity(result.value)) {
        throw new ArgumentException("Функция не определена или переполнена при x = " + value.ToString("G8") + ".");
      }
      return result;
    }

    private void skipSpaces() {
      while (position < text.Length && char.IsWhiteSpace(text[position])) {
        position++;
      }
    }

    private bool take(char symbol) {
      skipSpaces();
      if (position < text.Length && text[position] == symbol) {
        position++;
        return true;
      }
      return false;
    }

    private FunctionValue readSum() {
      FunctionValue result;

      result = readProduct();
      while (true) {
        if (take('+')) {
          result = FunctionValue.add(result, readProduct());
        } else if (take('-')) {
          result = FunctionValue.add(result, FunctionValue.negate(readProduct()));
        } else {
          return result;
        }
      }
    }

    private FunctionValue readProduct() {
      FunctionValue result;
      FunctionValue divisor;

      result = readUnary();
      while (true) {
        if (take('*')) {
          result = FunctionValue.multiply(result, readUnary());
        } else if (take('/')) {
          divisor = readUnary();
          if (divisor.value == 0.0) {
            throw new ArgumentException("Деление на ноль при x = " + argument.ToString("G8") + ".");
          }
          result = FunctionValue.divide(result, divisor);
        } else {
          return result;
        }
      }
    }

    private FunctionValue readUnary() {
      FunctionValue result;

      depth++;
      if (depth > 60) {
        throw new ArgumentException("Слишком много вложенных операций в формуле.");
      }
      if (take('+')) {
        result = readUnary();
      } else if (take('-')) {
        result = FunctionValue.negate(readUnary());
      } else {
        result = readPower();
      }
      depth--;
      return result;
    }

    private FunctionValue readPower() {
      FunctionValue result;
      FunctionValue exponent;

      result = readValue();
      if (take('^')) {
        exponent = readUnary();
        result = FunctionValue.power(result, exponent);
      }
      return result;
    }

    private FunctionValue readValue() {
      int start;
      string name;
      string numberText;
      double number;
      FunctionValue value;

      skipSpaces();
      if (take('(')) {
        value = readSum();
        if (!take(')')) {
          throw new ArgumentException("Не хватает закрывающей скобки.");
        }
        return value;
      }
      if (position >= text.Length) {
        throw new ArgumentException("Формула обрывается: ожидается число, x или функция.");
      }

      start = position;
      if (char.IsLetter(text[position])) {
        while (position < text.Length && char.IsLetter(text[position])) {
          position++;
        }
        name = text.Substring(start, position - start);
        if (name == "x") {
          return new FunctionValue(argument, 1.0, 0.0);
        }
        if (name == "pi") {
          return new FunctionValue(Math.PI, 0.0, 0.0);
        }
        if (name == "e") {
          return new FunctionValue(Math.E, 0.0, 0.0);
        }
        if (!take('(')) {
          throw new ArgumentException("После имени функции нужны скобки: например, sin(x).");
        }
        value = readSum();
        if (!take(')')) {
          throw new ArgumentException("Не хватает закрывающей скобки функции.");
        }
        return FunctionValue.apply(name, value);
      }

      while (position < text.Length && (char.IsDigit(text[position]) || text[position] == '.')) {
        position++;
      }
      if (position < text.Length && text[position] == 'e' && position > start) {
        position++;
        if (position < text.Length && (text[position] == '+' || text[position] == '-')) {
          position++;
        }
        while (position < text.Length && char.IsDigit(text[position])) {
          position++;
        }
      }
      numberText = text.Substring(start, position - start);
      if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
          || double.IsNaN(number) || double.IsInfinity(number)) {
        throw new ArgumentException("Некорректное число или символ в позиции " + (start + 1) + ".");
      }
      return new FunctionValue(number, 0.0, 0.0);
    }
  }
}
