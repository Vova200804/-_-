using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace NewtonLab {
  public class MainForm : Form {
    private TextBox formulaBox;
    private TextBox leftBox;
    private TextBox rightBox;
    private TextBox accuracyBox;
    private Label resultLabel;
    private Label stepLabel;
    private Chart functionChart;
    private ListBox stepsList;
    private ToolStripMenuItem firstItem;
    private ToolStripMenuItem previousItem;
    private ToolStripMenuItem nextItem;
    private ToolStripMenuItem lastItem;
    private Formula formula;
    private NewtonMethod method;
    private int currentStep;
    private int graphPointCount;
    private bool updatingSteps;
    private double offsetX;
    private double scaleX;
    private double offsetY;
    private double scaleY;

    public MainForm() {
      graphPointCount = 800;
      Text = "Лабораторная работа №2 — Метод Ньютона";
      Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
      ClientSize = new Size(1080, 720);
      MinimumSize = new Size(920, 650);
      StartPosition = FormStartPosition.CenterScreen;
      Font = new Font("Segoe UI", 10.0f);
      AutoScaleMode = AutoScaleMode.Dpi;
      BackColor = Color.White;
      buildInterface();
      clearResult();
    }

    private void buildInterface() {
      MenuStrip menu;
      ToolStripMenuItem calculateItem;
      ToolStripMenuItem clearItem;
      ToolStripMenuItem helpItem;
      TableLayoutPanel layout;
      TableLayoutPanel inputLayout;
      TableLayoutPanel chartLayout;
      Label hintLabel;
      ChartArea chartArea;
      Series functionSeries;
      Series pathSeries;
      Series pointSeries;
      Series minimumSeries;

      menu = new MenuStrip();
      calculateItem = new ToolStripMenuItem("Рассчитать");
      clearItem = new ToolStripMenuItem("Очистить");
      firstItem = new ToolStripMenuItem("К началу");
      previousItem = new ToolStripMenuItem("Предыдущий шаг");
      nextItem = new ToolStripMenuItem("Следующий шаг");
      lastItem = new ToolStripMenuItem("К результату");
      helpItem = new ToolStripMenuItem("Справка");
      calculateItem.ShortcutKeys = Keys.F5;
      previousItem.ShortcutKeys = Keys.Control | Keys.Left;
      nextItem.ShortcutKeys = Keys.Control | Keys.Right;
      calculateItem.Click += calculateClick;
      clearItem.Click += clearClick;
      firstItem.Click += firstClick;
      previousItem.Click += previousClick;
      nextItem.Click += nextClick;
      lastItem.Click += lastClick;
      helpItem.Click += helpClick;
      menu.Items.AddRange(new ToolStripItem[] {
        calculateItem, clearItem, firstItem, previousItem, nextItem, lastItem, helpItem
      });
      MainMenuStrip = menu;

      layout = new TableLayoutPanel();
      layout.Dock = DockStyle.Fill;
      layout.Padding = new Padding(16, 8, 16, 12);
      layout.ColumnCount = 1;
      layout.RowCount = 5;
      layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104.0f));
      layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48.0f));
      layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
      layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34.0f));
      layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38.0f));

      inputLayout = new TableLayoutPanel();
      inputLayout.Dock = DockStyle.Fill;
      inputLayout.ColumnCount = 6;
      inputLayout.RowCount = 2;
      inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58.0f));
      inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34.0f));
      inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44.0f));
      inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.0f));
      inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110.0f));
      inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.0f));
      inputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0f));
      inputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0f));
      formulaBox = makeTextBox("(x - 2)^2 + 1", "Формула функции");
      leftBox = makeTextBox("-5", "Левая граница a");
      rightBox = makeTextBox("5", "Правая граница b");
      accuracyBox = makeTextBox("0,0001", "Точность e");
      inputLayout.Controls.Add(makeLabel("f(x) ="), 0, 0);
      inputLayout.Controls.Add(formulaBox, 1, 0);
      inputLayout.SetColumnSpan(formulaBox, 5);
      inputLayout.Controls.Add(makeLabel("a ="), 0, 1);
      inputLayout.Controls.Add(leftBox, 1, 1);
      inputLayout.Controls.Add(makeLabel("b ="), 2, 1);
      inputLayout.Controls.Add(rightBox, 3, 1);
      inputLayout.Controls.Add(makeLabel("Точность e ="), 4, 1);
      inputLayout.Controls.Add(accuracyBox, 5, 1);

      resultLabel = makeLabel("");
      resultLabel.Font = new Font(Font, FontStyle.Bold);
      resultLabel.ForeColor = Color.FromArgb(25, 80, 135);
      chartLayout = new TableLayoutPanel();
      chartLayout.Dock = DockStyle.Fill;
      chartLayout.ColumnCount = 2;
      chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 74.0f));
      chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26.0f));
      chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));

      functionChart = new Chart();
      functionChart.Dock = DockStyle.Fill;
      functionChart.AccessibleName = "График функции и приближения к минимуму";
      chartArea = new ChartArea("functionArea");
      chartArea.AxisX.Title = "x";
      chartArea.AxisY.Title = "f(x)";
      chartArea.AxisX.LabelStyle.Format = "G5";
      chartArea.AxisY.LabelStyle.Format = "G5";
      chartArea.AxisX.MajorGrid.LineColor = Color.Gainsboro;
      chartArea.AxisY.MajorGrid.LineColor = Color.Gainsboro;
      chartArea.AxisX.IsStartedFromZero = false;
      chartArea.AxisY.IsStartedFromZero = false;
      functionChart.ChartAreas.Add(chartArea);
      functionChart.Legends.Add(new Legend("legend"));
      functionChart.Legends[0].Docking = Docking.Bottom;

      functionSeries = new Series("f(x)");
      functionSeries.ChartType = SeriesChartType.Line;
      functionSeries.Color = Color.RoyalBlue;
      functionSeries.BorderWidth = 2;
      pathSeries = new Series("Приближения");
      pathSeries.ChartType = SeriesChartType.Point;
      pathSeries.Color = Color.Gray;
      pathSeries.MarkerSize = 6;
      pointSeries = new Series("Текущий шаг");
      pointSeries.ChartType = SeriesChartType.Point;
      pointSeries.Color = Color.DarkOrange;
      pointSeries.MarkerSize = 11;
      minimumSeries = new Series("Минимум");
      minimumSeries.ChartType = SeriesChartType.Point;
      minimumSeries.Color = Color.Crimson;
      minimumSeries.MarkerStyle = MarkerStyle.Diamond;
      minimumSeries.MarkerSize = 18;
      minimumSeries.MarkerColor = Color.Transparent;
      minimumSeries.MarkerBorderColor = Color.Crimson;
      minimumSeries.MarkerBorderWidth = 2;
      functionChart.Series.Add(functionSeries);
      functionChart.Series.Add(pathSeries);
      functionChart.Series.Add(pointSeries);
      functionChart.Series.Add(minimumSeries);

      stepsList = new ListBox();
      stepsList.Dock = DockStyle.Fill;
      stepsList.IntegralHeight = false;
      stepsList.HorizontalScrollbar = true;
      stepsList.AccessibleName = "История приближений";
      stepsList.SelectedIndexChanged += stepSelected;
      chartLayout.Controls.Add(functionChart, 0, 0);
      chartLayout.Controls.Add(stepsList, 1, 0);
      stepLabel = makeLabel("");
      hintLabel = makeLabel("Формула: +  -  *  /  ^  ( )  sin  cos  tan  exp  ln  log  sqrt  abs  pi  e. Углы в радианах.\nВведите данные и выберите «Рассчитать» (F5). Действия находятся в верхнем меню.");
      hintLabel.Font = new Font("Segoe UI", 9.0f);
      hintLabel.ForeColor = Color.DimGray;
      layout.Controls.Add(inputLayout, 0, 0);
      layout.Controls.Add(resultLabel, 0, 1);
      layout.Controls.Add(chartLayout, 0, 2);
      layout.Controls.Add(stepLabel, 0, 3);
      layout.Controls.Add(hintLabel, 0, 4);
      Controls.Add(layout);
      Controls.Add(menu);

      formulaBox.TextChanged += inputChanged;
      leftBox.TextChanged += inputChanged;
      rightBox.TextChanged += inputChanged;
      accuracyBox.TextChanged += inputChanged;
    }

    private Label makeLabel(string text) {
      Label label;

      label = new Label();
      label.Text = text;
      label.Dock = DockStyle.Fill;
      label.TextAlign = ContentAlignment.MiddleLeft;
      return label;
    }

    private TextBox makeTextBox(string text, string accessibleName) {
      TextBox textBox;

      textBox = new TextBox();
      textBox.Text = text;
      textBox.AccessibleName = accessibleName;
      textBox.Dock = DockStyle.Fill;
      textBox.Margin = new Padding(3, 10, 12, 3);
      return textBox;
    }

    private double readNumber(TextBox textBox, string name) {
      string text;
      double number;

      text = textBox.Text.Trim().Replace(',', '.');
      if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
          || double.IsNaN(number) || double.IsInfinity(number)) {
        textBox.Focus();
        throw new ArgumentException("Поле «" + name + "» должно содержать конечное вещественное число.");
      }
      return number;
    }

    private void calculateClick(object sender, EventArgs eventArgs) {
      double left;
      double right;
      double accuracy;
      int pointIndex;

      clearResult();
      try {
        left = readNumber(leftBox, "a");
        right = readNumber(rightBox, "b");
        accuracy = readNumber(accuracyBox, "e");
        formula = new Formula(formulaBox.Text);
        method = new NewtonMethod(formula);
        method.calculate(left, right, accuracy);

        drawGraph(left, right);
        for (pointIndex = 0; pointIndex < method.points.Count; pointIndex++) {
          stepsList.Items.Add(pointIndex + ": x = " + method.points[pointIndex].ToString("G10"));
        }
        resultLabel.Text = "Локальный минимум: x = " + method.minimumX.ToString("G17")
          + ";  f(x) = " + method.minimumY.ToString("G10")
          + ";  итераций: " + (method.points.Count - 1) + ".\n"
          + "Точность по отрезку: R = " + method.errorBound.ToString("G6") + " ≤ e = " + method.accuracy.ToString("G6") + ".";
        if (method.isBoundary) {
          resultLabel.Text = "Минимум на границе: x = " + method.minimumX.ToString("G17")
            + ";  f(x) = " + method.minimumY.ToString("G10") + ".\n"
            + "Проверено одностороннее условие минимума на [a, b].";
        }
        currentStep = method.points.Count - 1;
        showStep();
      } catch (ArgumentException error) {
        clearResult();
        MessageBox.Show(this, error.Message, "Проверьте входные данные", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      } catch (ArithmeticException error) {
        clearResult();
        MessageBox.Show(this, error.Message, "Ошибка вычислений", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      } catch (InvalidOperationException) {
        clearResult();
        MessageBox.Show(this, "Не удалось построить график. Выберите меньший интервал или другие значения функции.",
          "Ошибка построения графика", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    private void drawGraph(double left, double right) {
      double point;
      double minimumValue;
      double maximumValue;
      double span;
      double[] values;
      ChartArea area;
      int pointIndex;

      values = new double[graphPointCount + 1];
      minimumValue = double.PositiveInfinity;
      maximumValue = double.NegativeInfinity;
      for (pointIndex = 0; pointIndex <= graphPointCount; pointIndex++) {
        point = left + (right - left) * ((double)pointIndex / graphPointCount);
        values[pointIndex] = formula.calculate(point);
        minimumValue = Math.Min(minimumValue, values[pointIndex]);
        maximumValue = Math.Max(maximumValue, values[pointIndex]);
      }
      span = maximumValue - minimumValue;
      if (double.IsInfinity(span)) {
        throw new ArgumentException("Разброс значений слишком велик для графика. Уточните интервал.");
      }

      offsetX = 0.0;
      scaleX = 1.0;
      offsetY = 0.0;
      scaleY = 1.0;
      if (Math.Max(Math.Abs(left), Math.Abs(right)) > (right - left) * 10000.0) {
        offsetX = left;
      }
      if (right - left > 1e8 || right - left < 1e-6) {
        offsetX = left;
        scaleX = right - left;
      }
      if (span > 0.0 && (span < 1e-6 || Math.Max(Math.Abs(minimumValue), Math.Abs(maximumValue)) > 1e8
          || Math.Max(Math.Abs(minimumValue), Math.Abs(maximumValue)) > span * 100000.0)) {
        offsetY = minimumValue;
        scaleY = span;
      }
      if (span == 0.0 && Math.Abs(minimumValue) > 1e8) {
        offsetY = minimumValue;
        scaleY = Math.Abs(minimumValue);
      }
      area = functionChart.ChartAreas[0];
      area.AxisX.Minimum = (left - offsetX) / scaleX;
      area.AxisX.Maximum = (right - offsetX) / scaleX;
      area.AxisY.Minimum = double.NaN;
      area.AxisY.Maximum = double.NaN;
      area.AxisX.Title = "x";
      area.AxisY.Title = "f(x)";
      if (offsetX != 0.0 || scaleX != 1.0) {
        area.AxisX.Title = "(x − " + offsetX.ToString("G5") + ") / " + scaleX.ToString("G5");
      }
      if (offsetY != 0.0 || scaleY != 1.0) {
        area.AxisY.Title = "(f − " + offsetY.ToString("G5") + ") / " + scaleY.ToString("G5");
      }
      for (pointIndex = 0; pointIndex <= graphPointCount; pointIndex++) {
        point = left + (right - left) * ((double)pointIndex / graphPointCount);
        addPoint(0, point, values[pointIndex]);
      }
    }

    private void addPoint(int seriesIndex, double point, double value) {
      int pointIndex;

      pointIndex = functionChart.Series[seriesIndex].Points.AddXY((point - offsetX) / scaleX, (value - offsetY) / scaleY);
      functionChart.Series[seriesIndex].Points[pointIndex].ToolTip = "x = " + point.ToString("G17")
        + "; f(x) = " + value.ToString("G17");
    }

    private void showStep() {
      int pointIndex;
      double point;
      Iteration step;

      if (method == null) {
        return;
      }
      functionChart.Series[1].Points.Clear();
      functionChart.Series[2].Points.Clear();
      functionChart.Series[3].Points.Clear();
      for (pointIndex = 0; pointIndex <= currentStep; pointIndex++) {
        point = method.points[pointIndex];
        addPoint(1, point, method.steps[pointIndex].value);
      }
      point = method.points[currentStep];
      step = method.steps[currentStep];
      addPoint(2, point, step.value);
      if (currentStep == method.points.Count - 1) {
        addPoint(3, method.minimumX, method.minimumY);
      }
      stepLabel.Text = "Шаг " + currentStep + " из " + (method.points.Count - 1)
        + ": x = " + point.ToString("G17") + ";  f(x) = " + step.value.ToString("G10")
        + ".  " + step.kind + ".\n"
        + "Оценка по отрезку: R = " + step.errorBound.ToString("G6") + ";  e = " + method.accuracy.ToString("G6") + ".";
      updatingSteps = true;
      stepsList.SelectedIndex = currentStep;
      updatingSteps = false;
      firstItem.Enabled = currentStep > 0;
      previousItem.Enabled = currentStep > 0;
      nextItem.Enabled = currentStep < method.points.Count - 1;
      lastItem.Enabled = currentStep < method.points.Count - 1;
    }

    private void clearResult() {
      int seriesIndex;

      method = null;
      formula = null;
      currentStep = 0;
      for (seriesIndex = 0; seriesIndex < functionChart.Series.Count; seriesIndex++) {
        functionChart.Series[seriesIndex].Points.Clear();
      }
      functionChart.ChartAreas[0].AxisX.Minimum = double.NaN;
      functionChart.ChartAreas[0].AxisX.Maximum = double.NaN;
      functionChart.ChartAreas[0].AxisX.Title = "x";
      functionChart.ChartAreas[0].AxisY.Title = "f(x)";
      stepsList.Items.Clear();
      resultLabel.Text = "Результат появится после расчёта.";
      stepLabel.Text = "Шаги ещё не рассчитаны.";
      firstItem.Enabled = false;
      previousItem.Enabled = false;
      nextItem.Enabled = false;
      lastItem.Enabled = false;
    }

    private void inputChanged(object sender, EventArgs eventArgs) {
      clearResult();
    }

    private void clearClick(object sender, EventArgs eventArgs) {
      formulaBox.Clear();
      leftBox.Clear();
      rightBox.Clear();
      accuracyBox.Clear();
      clearResult();
      formulaBox.Focus();
    }

    private void firstClick(object sender, EventArgs eventArgs) {
      if (method == null) {
        return;
      }
      currentStep = 0;
      showStep();
    }

    private void previousClick(object sender, EventArgs eventArgs) {
      if (method != null && currentStep > 0) {
        currentStep--;
        showStep();
      }
    }

    private void nextClick(object sender, EventArgs eventArgs) {
      if (method != null && currentStep < method.points.Count - 1) {
        currentStep++;
        showStep();
      }
    }

    private void lastClick(object sender, EventArgs eventArgs) {
      if (method == null) {
        return;
      }
      currentStep = method.points.Count - 1;
      showStep();
    }

    private void stepSelected(object sender, EventArgs eventArgs) {
      if (!updatingSteps && method != null && stepsList.SelectedIndex >= 0) {
        currentStep = stepsList.SelectedIndex;
        showStep();
      }
    }

    private void helpClick(object sender, EventArgs eventArgs) {
      MessageBox.Show(this,
        "Метод Ньютона: x(next) = x - f'(x) / f''(x).\n"
        + "Производные вычисляются по правилам дифференцирования. Неудачный шаг заменяется делением интервала пополам.\n\n"
        + "Функция должна быть гладкой и определённой на всём [a, b]. "
        + "Выберите интервал вокруг внутреннего локального минимума. "
        + "При нескольких минимумах результат зависит от начальной точки; глобальный минимум не ищется.\n\n"
        + "Точность проверяется по R = max(x-L, U-x) <= e на отрезке со сменой знака производной с минуса на плюс. "
        + "Это численная оценка в double при гладкости функции. Минимум на границе подписывается отдельно.\n\n"
        + "Пример: (x - 2)^2 + 1; a = -5; b = 5; e = 0,0001.\n"
        + "Умножение записывается явно: 2*x. Степень: x^2. "
        + "ln — натуральный логарифм, log — десятичный. Углы в радианах.\n"
        + "Числа принимаются с точкой или запятой, например 0.01 и 0,01.\n\n"
        + "После расчёта выберите «К началу» и «Следующий шаг» либо строку в истории справа.",
        "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
  }
}
